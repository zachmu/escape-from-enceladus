using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ConvertUnits = Arena.Farseer.ConvertUnits;

namespace Arena.Entity {
    public class Player : IGameEntity {

        private static Player _instance;

        public static Player Instance {
            get { return _instance; }
        }

        /// <summary>
        /// How much bigger the image of the character should be than his hit box
        /// </summary>
        private const float ImageBuffer = .1f;

        private const float CharacterHeight = 2f - ImageBuffer;
        private const float CharacterWidth = 1f - ImageBuffer;

        public const int ImageWidth = 64;
        public const int ImageHeight = 128;

        private static readonly int DisplayHeight = (int) ConvertUnits.ToDisplayUnits(CharacterHeight + ImageBuffer);
        private static readonly int DisplayWidth = (int) ConvertUnits.ToDisplayUnits(CharacterWidth + ImageBuffer);

        private readonly World _world;

        private static readonly Constants Constants = Constants.Instance;
        private const string PlayerInitSpeedMs = "Player initial run speed (m/s)";
        private const string PlayerMaxSpeedMs = "Player max run speed (m/s)";
        private const string PlayerAccelerationMss = "Player acceleration (m/s/s)";
        private const string PlayerAirAccelerationMss = "Player horizontal air acceleration (m/s/s)";
        private const string PlayerJumpSpeed = "Player jump speed (m/s)";
        private const string PlayerAirBoostTime = "Player max air boost time (s)";
        private const string PlayerKnockbackTime = "Player knock back time (s)";
        private const string PlayerKnockbackAmt = "Player knock back amount (scalar)";
        

        static Player() {
            Constants.Register(new Constant(PlayerInitSpeedMs, 5.0f, Keys.I));
            Constants.Register(new Constant(PlayerAccelerationMss, 5.0f, Keys.A));
            Constants.Register(new Constant(PlayerMaxSpeedMs, 20, Keys.S));
            Constants.Register(new Constant(PlayerAirAccelerationMss, 5.0f, Keys.D));
            Constants.Register(new Constant(PlayerJumpSpeed, 10f, Keys.J));
            Constants.Register(new Constant(PlayerAirBoostTime, .5f, Keys.Y));
            Constants.Register(new Constant(PlayerKnockbackTime, .3f, Keys.K));
            Constants.Register(new Constant(PlayerKnockbackAmt, 5f, Keys.L));
        }

        private readonly List<Shot> _shots = new List<Shot>();

        private Direction _direction = Direction.Right;

        /// <summary>
        /// How long, in milliseconds, the player has been holding down the jump button.
        /// </summary>
        private long _airBoostTime = -1;

        /// <summary>
        /// How long, in ms, the player must wait before regaining control after being knocked back
        /// </summary>
        private long _timeUntilRegainControl;

        private readonly Body _body;
        private readonly Fixture _floorSensor;
        private int _floorSensorContactCount = 0;
        private readonly Fixture _ceilingSensor;
        private int _ceilingSensorContantCount = 0;

        public bool Disposed {
            get { return false; }
        }

        public Vector2 Position {
            get { return _body.Position; }
        }

        public PolygonShape Shape {
            get { return (PolygonShape) _body.FixtureList.First().Shape; }
        }

        public Transform Transform {
            get {
                Transform t = new Transform();
                _body.GetTransform(out t);
                return t;
            }
        }

        public Player(Vector2 position, World world) {
            _instance = this;

//            _body = BodyFactory.CreateRectangle(world, CharacterWidth, CharacterHeight, 10f);
            _body = BodyFactory.CreateRoundedRectangle(world, CharacterWidth, CharacterHeight, .02f, .02f, 0, 10);
            _body.IsStatic = false;
            _body.Restitution = 0.0f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.SleepingAllowed = false;
            _body.CollidesWith = Arena.TerrainCategory | Arena.EnemyCategory;
            _body.CollisionCategories = Arena.PlayerCategory;
            _body.UserData = UserData.NewPlayer();
            _body.FixtureList.First().UserData = "body";

            var rectangle = PolygonTools.CreateRectangle(CharacterWidth / 2f - .1f, .05f);
            Vertices translated = new Vertices(rectangle.Count);
            translated.AddRange(rectangle.Select(v => v + new Vector2(0, CharacterHeight / 2f)));

            Shape footSensorShape = new PolygonShape(translated, 0f);
            _floorSensor = _body.CreateFixture(footSensorShape);
            _floorSensor.IsSensor = true;
            _floorSensor.UserData = "floor";
            _floorSensor.OnCollision += (a, b, contact) => {
                _floorSensorContactCount++;
                Console.WriteLine("Hit floor");
                return true;
            };
            _floorSensor.OnSeparation += (a, b) => {
                // For some reason we sometimes get "duplicate" leaving events due to body / fixture destruction
                if ( _floorSensorContactCount > 0 ) {
                    _floorSensorContactCount--;
                    Console.WriteLine("Left floor");
                }
            };

            translated = new Vertices(rectangle.Count);
            translated.AddRange(rectangle.Select(v => v - new Vector2(0, CharacterHeight / 2f)));
            Shape ceilingSensorShape = new PolygonShape(translated, 0);
            _ceilingSensor = _body.CreateFixture(ceilingSensorShape);
            _ceilingSensor.IsSensor = true;
            _ceilingSensor.UserData = "ceiling";
            _ceilingSensor.OnCollision += (a, b, contact) => {
                _ceilingSensorContantCount++;
                Console.WriteLine("Hit ceiling");
                return true;
            };
            _ceilingSensor.OnSeparation += (a, b) => {
                // For some reason we sometimes get "duplicate" leaving events due to body / fixture destruction
                if ( _ceilingSensorContantCount > 0 ) {
                    _ceilingSensorContantCount--;
                    Console.WriteLine("Left ceiling");
                }
            };

            _world = world;
        }

        private Texture2D Image { get; set; }

        private readonly Texture2D[] _walkAnimation = new Texture2D[8];
        private readonly Texture2D[] _runAnimation = new Texture2D[8];
        private readonly Texture2D[] _duckAnimation = new Texture2D[2];
        private Texture2D _standImage;

        private int _animationFrame = 0;
        private long _timeSinceLastAnimationUpdate;
        private bool _isDucking;
        private bool _isRunning;

        public void LoadContent(ContentManager content) {
            for ( int i = 1; i <= 8; i++ ) {
                _walkAnimation[i - 1] = content.Load<Texture2D>("Character/walk" + i);
                _runAnimation[i - 1] = content.Load<Texture2D>("Character/run000" + i);
            }
            _duckAnimation[0] = content.Load<Texture2D>("Character/duck1");
            _duckAnimation[1] = content.Load<Texture2D>("Character/duck2");
            _standImage = content.Load<Texture2D>("Character/stand");

            Image = _standImage;
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 position = _body.Position;
            position.X -= CharacterWidth / 2;
            position.Y -= CharacterHeight / 2;
            if ( _isRunning || _isDucking ) {
                position -= new Vector2(.5f, 0);
            }
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, Color.White, 0f, new Vector2(),
                             _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            foreach ( Shot shot in _shots ) {
                shot.Draw(spriteBatch, camera);
            }
        }

        /// <summary>
        /// Updates the player for elapsed game time.
        /// </summary>
        public void Update(GameTime gameTime) {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            Vector2 leftStick = gamePadState.ThumbSticks.Left;

            if ( _timeUntilRegainControl > 0 ) {
                _timeUntilRegainControl -= gameTime.ElapsedGameTime.Milliseconds;
            }

            HandleJump(gameTime);

            HandleShot(gameTime);

            HandleMovement(leftStick, gameTime);

            UpdateImage(gameTime);

            foreach ( Shot shot in _shots ) {
                shot.Update(gameTime);
            }
            _shots.RemoveAll(shot => shot.Disposed);
        }

        /// <summary>
        /// Updates the current image for the next frame
        /// </summary>
        private void UpdateImage(GameTime gameTime) {
            if ( !IsStanding() || _body.LinearVelocity.X == 0 ) {
                if (_isDucking) {
                    Image = _duckAnimation[1];
                } else {
                    Image = _standImage;   
                }
                _animationFrame = 0;
                _timeSinceLastAnimationUpdate = 0;
                _isRunning = false;
            } else if ( Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerInitSpeedMs] ) {
                _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;
                if ( _timeSinceLastAnimationUpdate > 1000f / 8 ) {
                    _isRunning = false;
                    Image = _walkAnimation[_animationFrame];
                    _animationFrame = (_animationFrame + 1) % _walkAnimation.Length;
                    _timeSinceLastAnimationUpdate = 0;
                }
            } else {
                _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;
                if ( _timeSinceLastAnimationUpdate > 1000f / 8 ) {
                    _isRunning = true;
                    Image = _runAnimation[_animationFrame];
                    _animationFrame = (_animationFrame + 1) % _runAnimation.Length;
                    _timeSinceLastAnimationUpdate = 0;
                }
            }
        }
        
        /// <summary>
        /// Handles firing any weapons
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.X) ) {
                Vector2 position = _body.Position;
                switch ( _direction ) {
                    case Direction.Right:
                        position += new Vector2(CharacterWidth / 2f, -CharacterHeight / 4.5f);
                        break;
                    case Direction.Left:
                        position += new Vector2(-(CharacterWidth / 2f), -CharacterHeight / 4.5f);
                        break;
                }
                if ( _isDucking ) {
                    position += new Vector2(0, CharacterHeight / 2);
                }
                _shots.Add(new Shot(position, _world, _direction));
            }
        }

        /// <summary>
        /// Handles movement input, both on the ground and in the air.
        /// </summary>
        private void HandleMovement(Vector2 movementInput, GameTime gameTime) {
            if ( _timeUntilRegainControl <= 0 ) {
                if ( IsStanding() ) {
                    if ( movementInput.X < 0 ) {
                        _direction = Direction.Left;
                        if ( _body.LinearVelocity.X > -Constants[PlayerInitSpeedMs] ) {
                            _body.LinearVelocity = new Vector2(-Constants[PlayerInitSpeedMs], _body.LinearVelocity.Y);
                        } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                            if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.B) ) {
                                _body.LinearVelocity -= new Vector2(
                                    GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                            }
                        } else {
                            _body.LinearVelocity = new Vector2(-Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                        }
                    } else if ( movementInput.X > 0 ) {
                        _direction = Direction.Right;
                        if ( _body.LinearVelocity.X < Constants[PlayerInitSpeedMs] ) {
                            _body.LinearVelocity = new Vector2(Constants[PlayerInitSpeedMs], _body.LinearVelocity.Y);
                        } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                            if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.B) ) {
                                _body.LinearVelocity += new Vector2(
                                    GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                            }
                        } else {
                            _body.LinearVelocity = new Vector2(Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                        }
                    } else {
                        _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                    }
                } else {
                    // in the air
                    if ( movementInput.X < 0 ) {
                        if ( _body.LinearVelocity.X > -Constants[PlayerMaxSpeedMs] ) {
                            _body.LinearVelocity -= new Vector2(
                                GetVelocityDelta(Constants[PlayerAirAccelerationMss], gameTime), 0);
                        } else {
                            _body.LinearVelocity = new Vector2(-Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                        }
                        if ( _body.LinearVelocity.X <= 0 ) {
                            _direction = Direction.Left;
                        }
                    } else if ( movementInput.X > 0 ) {
                        if ( _body.LinearVelocity.X < Constants[PlayerMaxSpeedMs] ) {
                            _body.LinearVelocity += new Vector2(
                                GetVelocityDelta(Constants[PlayerAirAccelerationMss], gameTime), 0);
                        } else {
                            _body.LinearVelocity = new Vector2(Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                        }
                        if ( _body.LinearVelocity.X >= 0 ) {
                            _direction = Direction.Right;
                        }
                    }
                }

                // handle ducking
                if ( movementInput.Y < -.9 ) {
                    _isDucking = true;
                    if ( IsStanding() ) {
                        _body.LinearVelocity = new Vector2(0, 0);
                    }
                } else {
                    _isDucking = false;
                }
            }
        }

        private float GetVelocityDelta(float acceleration, GameTime gameTime) {
            return gameTime.ElapsedGameTime.Milliseconds / 1000f * acceleration;
        }

        /// <summary>
        /// Handles jump input 
        /// </summary>
        private void HandleJump(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.A) ) {
                if ( IsStanding() ) {
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                    _airBoostTime = 0;
                }
            } else if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.A) ) {
                if ( IsTouchingCeiling() ) {
                    _airBoostTime = -1;
                } else if ( _airBoostTime >= 0 && _airBoostTime < Constants[PlayerAirBoostTime] * 1000 ) {
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                    _airBoostTime += gameTime.ElapsedGameTime.Milliseconds;
                }
            } else {
                _airBoostTime = -1;
            }
        }

        private bool IsStanding() {
            return _floorSensorContactCount > 0;
        }

        private bool IsTouchingCeiling() {
            return _ceilingSensorContantCount > 0;
        }

        public void HitBy(Enemy enemy) {
            Vector2 diff = Position - enemy.Position;
            _body.LinearVelocity = new Vector2(0);
            _body.ApplyLinearImpulse(diff * Constants[PlayerKnockbackAmt] * _body.Mass);
            _timeUntilRegainControl = (long) (Constants[PlayerKnockbackTime] * 1000);
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }

    public enum Direction {
        Left,
        Right,
        Up,
        Down,
    };
}

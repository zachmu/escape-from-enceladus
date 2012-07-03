using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ConvertUnits = Arena.Farseer.ConvertUnits;

namespace Arena.Entity {
    public class Player : Entity, IGameEntity {

        private static Player _instance;
        public static Player Instance {
            get { return _instance; }
        }

        #region Constants

        private const float CharacterHeight = 1.9f;
        private const float CharacterWidth = .6f;

        public const int ImageWidth = 64;
        public const int ImageHeight = 128;

        private static readonly Constants Constants = Constants.Instance;
        private const string PlayerInitSpeedMs = "Player initial run speed (m/s)";
        private const string PlayerMaxSpeedMs = "Player max run speed (m/s)";
        private const string PlayerAccelerationMss = "Player acceleration (m/s/s)";
        private const string PlayerAirAccelerationMss = "Player horizontal air acceleration (m/s/s)";
        private const string PlayerJumpSpeed = "Player jump speed (m/s)";
        private const string PlayerAirBoostTime = "Player max air boost time (s)";
        private const string PlayerKnockbackTime = "Player knock back time (s)";
        private const string PlayerKnockbackAmt = "Player knock back amount (scalar)";
        private const string PlayerJogSpeedMultiplier = "Player jog speed multiplier";
        private const string PlayerWalkSpeedMultiplier = "Player walk speed multiplier";

        static Player() {
            Constants.Register(new Constant(PlayerInitSpeedMs, 2.5f, Keys.I));
            Constants.Register(new Constant(PlayerAccelerationMss, 5.0f, Keys.A));
            Constants.Register(new Constant(PlayerMaxSpeedMs, 20, Keys.S));
            Constants.Register(new Constant(PlayerAirAccelerationMss, 5.0f, Keys.D));
            Constants.Register(new Constant(PlayerJumpSpeed, 10f, Keys.J));
            Constants.Register(new Constant(PlayerAirBoostTime, .5f, Keys.Y));
            Constants.Register(new Constant(PlayerKnockbackTime, .3f, Keys.K));
            Constants.Register(new Constant(PlayerKnockbackAmt, 5f, Keys.L));
            Constants.Register(new Constant(PlayerJogSpeedMultiplier, .37f, Keys.B, .01f));
            Constants.Register(new Constant(PlayerWalkSpeedMultiplier, .5f, Keys.N));
        }

        #endregion

        private Direction _direction = Direction.Right;
        public Direction Direction {
            get { return _direction; }
        }

        /// <summary>
        /// How long, in milliseconds, the player has been holding down the jump button.
        /// </summary>
        private long _airBoostTime = -1;

        /// <summary>
        /// How long, in ms, the player must wait before regaining control after being knocked back
        /// </summary>
        private long _timeUntilRegainControl;

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

            _body.OnCollision += (a, b, contact) => {
                UpdateStanding();
                return true;
            };
            _body.OnSeparation += (a, b) => UpdateStanding();

            var rectangle = PolygonTools.CreateRectangle(CharacterWidth / 2f - .1f, .01f);
            Vertices translated = new Vertices(rectangle.Count);
            translated = new Vertices(rectangle.Count);
            translated.AddRange(rectangle.Select(v => v - new Vector2(0, CharacterHeight / 2f)));
            Shape ceilingSensorShape = new PolygonShape(translated, 0);
            _ceilingSensor = _body.CreateFixture(ceilingSensorShape);
            _ceilingSensor.IsSensor = true;
            _ceilingSensor.UserData = "ceiling";
            _ceilingSensor.OnCollision += (a, b, contact) => {
                if ( !b.IsSensor ) {
                    _ceilingSensorContantCount++;
                }
                return true;
            };
            _ceilingSensor.OnSeparation += (a, b) => {
                if ( _ceilingSensorContantCount > 0 ) {
                    _ceilingSensorContantCount--;
                }
            };

            _world = world;
        }

        private Texture2D Image { get; set; }
        private SoundEffect LandSound { get; set; }

        private int _waveTimeMs = -1;
        private Effect _waveEffect;
        private Vector2 _waveEffectCenter;

        protected override bool IsStanding {
            get { return _isStanding; }
            set {
                if ( value && !_isStanding ) {
                    LandSound.Play();
                }
                _isStanding = value;
            }
        }

        private bool IsTouchingCeiling() {
            return _ceilingSensorContantCount > 0;
        }

        public void LoadContent(ContentManager content) {
            LoadAnimations(content);
            LandSound = content.Load<SoundEffect>("land");
            Sonar.LoadContent(content);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is character's feet
            Vector2 position = _body.Position;
            position.Y += CharacterHeight / 2;
                        
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, Color.White, 0f, new Vector2(Image.Width / 2, Image.Height - 1),
                             _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            foreach ( IGameEntity shot in _shots ) {
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

            HandleWave(gameTime);

            foreach ( IGameEntity shot in _shots ) {
                shot.Update(gameTime);
            }
            _shots.RemoveAll(shot => shot.Disposed);
        }

        private void HandleWave(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.Y) ) {
                Direction shotDirection;
                var position = GetShotPosition(out shotDirection);
                _shots.Add(new Sonar(position, shotDirection));
            }
        }

        #region Animation

        private const int NumStandFrames = 2;
        private const int NumWalkFrames = 30;
        private const int NumJogFrames = 13;
        private const int NumRunFrames = 22;
        private const int NumJumpFrames = 12;

        private enum Animation {
            Stand,
            Walk,
            Jog,
            Run,
            Duck,
            Jump,
        };

        private Animation _currentAnimation = Animation.Stand;
        private Animation _prevAnimation = Animation.Stand;

        private readonly Texture2D[] _standAnimation = new Texture2D[NumStandFrames];
        private readonly Texture2D[] _walkAnimation = new Texture2D[NumWalkFrames];
        private readonly Texture2D[] _jogAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _runAnimation = new Texture2D[NumRunFrames];
        private readonly Texture2D[] _jumpAnimation = new Texture2D[NumJumpFrames];
        private readonly Texture2D[] _duckAnimation = new Texture2D[2];

        private int _animationFrame = 0;
        private const int JumpDelayMs = 50;
        private long _timeSinceLastAnimationUpdate;
        private long _timeSinceJump = -1;
        private bool _isDucking;
        private bool _jumpInitiated;

        private void LoadAnimations(ContentManager content) {
            for ( int i = 0; i < NumStandFrames; i++ ) {
                _standAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunStance/GunStance{0:0000}", i));
            }
            for ( int i = 0; i < NumWalkFrames; i++ ) {
                _walkAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalk{0:0000}", i));
            }
            for ( int i = 0; i < NumJogFrames; i++ ) {
                _jogAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJog{0:0000}", i));
            }
            for ( int i = 0; i < NumRunFrames; i++ ) {
                _runAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRun{0:0000}", i));
            }
            for ( int i = 0; i < NumJumpFrames; i++ ) {
                _jumpAnimation[i] = content.Load<Texture2D>(String.Format("Character/Jump/jump{0:0000}", i));
            }

            _duckAnimation[0] = content.Load<Texture2D>("Character/duck1");
            _duckAnimation[1] = content.Load<Texture2D>("Character/duck2");

            Image = _standAnimation[0];
        }

        /// <summary>
        /// Updates the current image for the next frame
        /// </summary>
        private void UpdateImage(GameTime gameTime) {

            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;

            if ( IsStanding ) {
                if ( _jumpInitiated ) {
                    _currentAnimation = Animation.Jump;
                    if ( _timeSinceJump == 0 ) {
                        _animationFrame = 0;
                    }
                    Image = _jumpAnimation[_animationFrame];
                    _animationFrame++;
                } else if ( _body.LinearVelocity.X == 0 ) {
                    if ( _isDucking ) {
                        _currentAnimation = Animation.Duck;
                        Image = _duckAnimation[1];
                    } else {
                        if ( _prevAnimation != Animation.Stand ) {
                            _currentAnimation = Animation.Stand;
                            _animationFrame = 0;
                            Image = _standAnimation[_animationFrame];
                        }

                        if ( _timeSinceLastAnimationUpdate > 1000f / NumStandFrames ) {
                            _animationFrame %= NumStandFrames;
                            _currentAnimation = Animation.Stand;
                            Image = _standAnimation[_animationFrame++];
                            _timeSinceLastAnimationUpdate = 0;
                        }
                    }
                } else if ( Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerInitSpeedMs] ) {
                    _currentAnimation = Animation.Walk;
                    float walkSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerWalkSpeedMultiplier]);
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkFrames / walkSpeed ) {
                        _animationFrame %= _walkAnimation.Length;
                        Image = _walkAnimation[_animationFrame++];
                        _timeSinceLastAnimationUpdate = 0;
                    }
                } else if ( Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerMaxSpeedMs] * .75f ) {
                    _currentAnimation = Animation.Jog;
                    float jogSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed ) {
                        _animationFrame %= _jogAnimation.Length;
                        Image = _jogAnimation[_animationFrame++];
                        _timeSinceLastAnimationUpdate = 0;
                    }
                } else {
                    _currentAnimation = Animation.Run;
                    float runSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunFrames / runSpeed ) {
                        _animationFrame %= _runAnimation.Length;
                        Image = _runAnimation[_animationFrame++];
                        _timeSinceLastAnimationUpdate = 0;
                    }
                }
            } else {
                _currentAnimation = Animation.Jump;
                if ( _prevAnimation != Animation.Jump ) {
                    _animationFrame = 5;
                }
                _animationFrame %= _jumpAnimation.Length;
                Image = _jumpAnimation[_animationFrame];
                if ( _animationFrame < 5 ) {
                    _animationFrame++;
                }
            }

            _prevAnimation = _currentAnimation;
        }

        #endregion

        private readonly List<IGameEntity> _shots = new List<IGameEntity>();

        /// <summary>
        /// Handles firing any weapons
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.X) ) {
                Direction shotDirection;
                var position = GetShotPosition(out shotDirection);
                _shots.Add(new Shot(position, _world, shotDirection));
            }
        }

        /// <summary>
        /// Returns the original location and direction to place a new shot in the game world.
        /// </summary>
        private Vector2 GetShotPosition(out Direction shotDirection) {
            Vector2 position = _body.Position;
            if ( InputHelper.Instance.GamePadState.ThumbSticks.Left.Y > .8 ) {
                shotDirection = Direction.Up;
            } else if ( !IsStanding && InputHelper.Instance.GamePadState.ThumbSticks.Left.Y < -.8 ) {
                shotDirection = Direction.Down;
            } else {
                shotDirection = _direction;
            }

            switch ( shotDirection ) {
                case Direction.Right:
                    position += new Vector2(CharacterWidth / 2f, -CharacterHeight / 4.5f);
                    break;
                case Direction.Left:
                    position += new Vector2(-(CharacterWidth / 2f), -CharacterHeight / 4.5f);
                    break;
                case Direction.Down:
                    position += new Vector2(0, CharacterHeight / 2 - .1f);
                    break;
                case Direction.Up:
                    position += new Vector2(0, -CharacterHeight / 2 + .1f);
                    break;
            }
            if ( _isDucking ) {
                position += new Vector2(0, CharacterHeight / 2);
            }
            return position;
        }

        /// <summary>
        /// Handles movement input, both on the ground and in the air.
        /// </summary>
        private void HandleMovement(Vector2 movementInput, GameTime gameTime) {
            if ( _timeUntilRegainControl <= 0 ) {
                if ( IsStanding ) {
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
                    if ( IsStanding ) {
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

            if ( _timeSinceJump >= 0 ) {
                _timeSinceJump += gameTime.ElapsedGameTime.Milliseconds;
            }

            if ( InputHelper.Instance.IsNewButtonPress(Buttons.A) ) {
                if ( IsStanding ) {
                    _timeSinceJump = 0;
                    _jumpInitiated = true;
                }
            } else if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.A) ) {
                if ( IsTouchingCeiling() ) {
                    _airBoostTime = -1;
                } else if ( _timeSinceJump >= JumpDelayMs 
                    && _airBoostTime >= 0 
                    && _airBoostTime < Constants[PlayerAirBoostTime] * 1000 + JumpDelayMs) {
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                    _airBoostTime += gameTime.ElapsedGameTime.Milliseconds;
                }
            } else {
                _airBoostTime = -1;
                _timeSinceJump = -1;
                _jumpInitiated = false;
            }

            if ( _jumpInitiated && _timeSinceJump >= JumpDelayMs ) {
                _jumpInitiated = false;
                _airBoostTime = 0;
                _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
            }
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

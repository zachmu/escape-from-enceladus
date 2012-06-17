using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {

    internal class Player {
        /// <summary>
        /// How much bigger the image of the character should be than his hit box
        /// </summary>
        private const float ImageBuffer = .1f;

        private const float CharacterHeight = 2f - ImageBuffer;
        private const float CharacterWidth = 1f - ImageBuffer;

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

        static Player() {
            Constants.Register(new Constant(PlayerInitSpeedMs, 5.0f, Keys.I));
            Constants.Register(new Constant(PlayerAccelerationMss, 5.0f, Keys.A));
            Constants.Register(new Constant(PlayerMaxSpeedMs, 20, Keys.S));
            Constants.Register(new Constant(PlayerAirAccelerationMss, 5.0f, Keys.D));
            Constants.Register(new Constant(PlayerJumpSpeed, 10f, Keys.J));
            Constants.Register(new Constant(PlayerAirBoostTime, .5f, Keys.Y));
        }

        public enum Direction {
            Left,
            Right
        };

        private readonly List<Shot> _shots = new List<Shot>();

        private Direction _direction = Direction.Right;

        /// <summary>
        /// How long, in milliseconds, the player has been holding down the jump button.
        /// </summary>
        private long _airBoostTime = -1;

        public Texture2D Image { get; set; }

        private readonly Body _body;
        private readonly Fixture _floorSensor;
        private Fixture _standingOnFixture;
        private int _floorSensorContactCount = 0;
        private readonly Fixture _ceilingSensor;
        private int _ceilingSensorContantCount = 0;
        private readonly Fixture _rightSideSensor;
        private readonly Fixture _leftSideSensor;

        public Vector2 Position {
            get { return _body.Position; }
        }

        public Player(Vector2 position, World world) {
            _body = BodyFactory.CreateRectangle(world, CharacterWidth, CharacterHeight, 10f);
            _body.IsStatic = false;
            _body.Restitution = 0.0f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.SleepingAllowed = false;
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

        public void Draw(SpriteBatch spriteBatch, Camera2D c) {
            Vector2 position = _body.Position;
            position.X -= CharacterWidth / 2;
            position.Y -= CharacterHeight / 2;
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, DisplayWidth, DisplayHeight),
                             null, Color.White, 0f, new Vector2(),
                             _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            foreach ( Shot shot in _shots ) {
                shot.Draw(spriteBatch, c);
            }
        }

        public void Update(GameTime gameTime) {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            Vector2 leftStick = gamePadState.ThumbSticks.Left;

            Vector2 adjustedDelta = new Vector2(leftStick.X, 0);

            HandleJump(gameTime);

            HandleShot(gameTime);

            HandleMovement(adjustedDelta, gameTime);
        }

        private void HandleShot(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.X) ) {
                Vector2 position = _body.Position;
                switch ( _direction ) {
                    case Direction.Right:
                        position += new Vector2(CharacterWidth / 2f + .5f, 0);
                        break;
                    case Direction.Left:
                        position += new Vector2(-(CharacterWidth / 2f) - .5f, 0);
                        break;
                }
                _shots.Add(new Shot(position, _world, _direction));
            }

            foreach ( Shot shot in _shots ) {
                shot.Update();
            }
            _shots.RemoveAll(shot => shot.Disposed);
        }

        /// <summary>
        /// Handles movement input, both on the ground and in the air.  Velocity is manipulated directly in the X axis, 
        /// but only if the player isn't touching a wall on that side.  Even with zero friction on the body, pressing 
        /// into the surface of a static body repeatedly will cause a friction effect.
        /// </summary>
        private void HandleMovement(Vector2 movementInput, GameTime gameTime) {
            if ( IsStanding() ) {
                if ( movementInput.X < 0) {
                    _direction = Direction.Left;
                    if ( _body.LinearVelocity.X > -Constants[PlayerInitSpeedMs] ) {
                        _body.LinearVelocity -= new Vector2(Constants[PlayerInitSpeedMs], 0);
                    } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                        _body.LinearVelocity -= new Vector2(
                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                    } else {
                        _body.LinearVelocity = new Vector2(-Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                    }
                } else if ( movementInput.X > 0) {
                    _direction = Direction.Right;
                    if ( _body.LinearVelocity.X < Constants[PlayerInitSpeedMs] ) {
                        _body.LinearVelocity += new Vector2(Constants[PlayerInitSpeedMs], 0);
                    } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                        _body.LinearVelocity += new Vector2(
                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                    } else {
                        _body.LinearVelocity = new Vector2(Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                    }
                } else {
                    _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                }
            } else { // in the air
                if ( movementInput.X < 0) {
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
        }

        private float GetVelocityDelta(float acceleration, GameTime gameTime) {
            return gameTime.ElapsedGameTime.Milliseconds / 1000f * acceleration;
        }

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
            return IsSensorTouchingWall(_floorSensor);
        }

        private bool IsTouchingCeiling() {
            return _ceilingSensorContantCount > 0;
            return IsSensorTouchingWall(_ceilingSensor);
        }

        private bool IsSensorTouchingWall(Fixture sensor) {
            var contactEdge = sensor.Body.ContactList;
            while ( contactEdge != null && contactEdge.Contact != null ) {
                if ( contactEdge.Contact.IsTouching() && contactEdge.Contact.FixtureA == sensor || contactEdge.Contact.FixtureB == sensor ) {
                    return true;
                }
                contactEdge = contactEdge.Next;
            }
            return false;
        }
    }
}

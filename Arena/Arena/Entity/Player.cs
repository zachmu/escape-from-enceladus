using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Collision;
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

        private const float CharacterStandingHeight = 1.9f;
        private const float CharacterStandingWidth = .6f;
        private const float CharacterJumpingHeight = 1.6f;
        private const float CharacterJumpingWidth = .6f;
        private const float CharacterDuckingHeight = 1.3f;
        private const float CharacterDuckingWidth = .6f;
        private const float CharacterScootingHeight = .5f;
        private const float CharacterScootingWidth = 1.7f;
        private const float ScooterNudge = CharacterScootingWidth / 2 - CharacterDuckingWidth / 2;
        // How far ahead of the standing / ducking position the world must be clear to scoot freely.
        private const float ScooterForwardClearance = ScooterNudge + CharacterScootingWidth / 2;

        private float Height { get; set; }
        private float Width { get; set; }
        
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
        private const string PlayerWheelSpinSpeedMultiplier = "Player wheel spin speed multipler";
        private const string PlayerScooterOffset = "Player scooter offset";

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
            Constants.Register(new Constant(PlayerWheelSpinSpeedMultiplier, 1.0f, Keys.M));
            Constants.Register(new Constant(PlayerScooterOffset, 0f, Keys.P));            
        }

        #endregion

        private Direction _facingDirection = Direction.Right;
        public Direction Direction {
            get { return _facingDirection; }
        }

        /// <summary>
        /// How long, in milliseconds, the player has been holding down the jump button.
        /// </summary>
        private long _airBoostTime = -1;

        /// <summary>
        /// How long, in ms, the player must wait before regaining control after being knocked back
        /// </summary>
        private long _timeUntilRegainControl;

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

        //private Fixture _scooterSensor;

        public Player(Vector2 position, World world) {
            _instance = this;

            _body = BodyFactory.CreateRectangle(world, CharacterStandingWidth, CharacterStandingHeight, 10f);
            _body.FixtureList.First().UserData = "body";
            Height = CharacterStandingHeight;
            Width = CharacterStandingWidth;
            ConfigureBody(position);
//            _scooterSensor = FixtureFactory.AttachRectangle(CharacterScootingWidth, CharacterScootingHeight, 0f,
//                                                            new Vector2(0,
//                                                                //CharacterScootingWidth / 2 - CharacterStandingWidth / 2,
//                                                                CharacterDuckingHeight / 2 -
//                                                                CharacterScootingHeight / 2 - .02f), _body);
//            _scooterSensor.IsSensor = true;
//            _scooterSensor.CollidesWith = Arena.TerrainCategory;
//            _scooterSensor.CollisionCategories = Category.All;
//            _scooterSensor.UserData = "scooter";
            
            HealthCapacity = 650;
            Health = HealthCapacity;

            _world = world;
        }

        private void ConfigureBody(Vector2 position) {
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
        }

        public int Health { get; private set; }
        public int HealthCapacity { get; private set; }

        private Texture2D _image;
        private Texture2D Image {
            get { return _image; }
            set {
                if ( _image != value ) {
                    _timeSinceLastAnimationUpdate = 0;
                }
                _image = value;
            }
        }

        private SoundEffect LandSound { get; set; }

        public void LoadContent(ContentManager content) {
            LoadAnimations(content);
            LandSound = content.Load<SoundEffect>("land");
            Sonar.LoadContent(content);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is character's feet
            Vector2 position = _body.Position;
            position.Y += Height / 2;
                        
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, Color.White, 0f, new Vector2(Image.Width / 2, Image.Height - 1),
                             _facingDirection == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            foreach ( IGameEntity shot in _shots ) {
                shot.Draw(spriteBatch, camera);
            }
        }

        /// <summary>
        /// Whether or not the character is standing on solid ground.
        /// </summary>
        protected override bool IsStanding {
            get { return _isStanding; }
            set {
                if ( value && !_isStanding ) {
                    LandSound.Play();
                    ResizeBody(CharacterStandingWidth, CharacterStandingHeight);
                } else if ( !value && _isStanding ) {
                    ResizeBody(CharacterJumpingWidth, CharacterJumpingHeight);
                    _isDucking = false;
                    _isScooting = false;
                }
                _isStanding = value;
            }
        }

        /// <summary>
        /// Whether or not the character is ducking, on the ground
        /// </summary>
        private bool _isDucking;
        public bool     IsDucking {
            get { return _isDucking; }
            private set {
                if ( IsScooting ) {
                    _isDucking = true;
                    return;
                }

                if ( value && !_isDucking && !IsScooting ) {
                    ResizeBody(CharacterDuckingWidth, CharacterDuckingHeight);
                } else if ( !value && _isDucking ) {
                    if ( IsScooting ) {
                        ResizeBody(CharacterScootingWidth, CharacterScootingHeight);
                    } else if ( IsStanding ) {
                        ResizeBody(CharacterStandingWidth, CharacterStandingHeight);
                    } else {
                        ResizeBody(CharacterJumpingWidth, CharacterJumpingHeight);
                    }
                }
                _isDucking = value;
            }
        }

        /// <summary>
        /// Whether or not the character is scooting
        /// </summary>
        private bool _isScooting;

        public bool IsScooting {
            get { return _isScooting && !_resizeRequested; }
            set {
                if ( value && !_isScooting ) {

                    // Make sure we're not too close to a vertical wall
                    float nudgeAmount = ScooterNudge;
                    float positionCorrectionAmount = 0;
                    float forwardClearance = ScooterForwardClearance;
                    if ( _facingDirection == Direction.Left ) {
                        nudgeAmount = -nudgeAmount;
                        forwardClearance = -forwardClearance;
                        //positionCorrectionAmount = -positionCorrectionAmount;
                    }

                    Vector2 startRay = new Vector2(_body.Position.X,
                                                   _body.Position.Y + CharacterDuckingHeight / 2 -
                                                   CharacterScootingHeight / 2 - .02f);
                    Vector2 endRay = startRay + new Vector2(forwardClearance, 0);
                    bool roomAhead = true;
                    _world.RayCast((fixture, point, normal, fraction) => {
                        if ( fixture.GetUserData().IsDoor || fixture.GetUserData().IsTerrain ) {
                            roomAhead = false;
                            positionCorrectionAmount = ScooterForwardClearance - Math.Abs(point.X - _body.Position.X) + .01f;
                            return 0;
                        }
                        return -1;
                    },
                                   startRay, endRay);

                    if ( !roomAhead ) {
                        endRay = startRay - new Vector2(nudgeAmount + .02f, 0);
                        bool roomBehind = true;
                        _world.RayCast((fixture, point, normal, fraction) => {
                            if ( fixture.GetUserData().IsDoor || fixture.GetUserData().IsTerrain ) {
                                roomBehind = false;
                                return 0;
                            }
                            return -1;
                        },
                                       startRay, endRay);
                    }

                    if ( !roomAhead ) {
                        if ( _facingDirection == Direction.Right ) {
                            _body.Position += new Vector2(-positionCorrectionAmount, 0);
                        } else {
                            _body.Position += new Vector2(positionCorrectionAmount, 0);                            
                        }
                    }

                    ResizeBody(CharacterScootingWidth, CharacterScootingHeight, new Vector2(nudgeAmount, 0));
                } else if ( !value && _isScooting ) {
                    if ( IsDucking ) {
                        ResizeBody(CharacterDuckingWidth, CharacterDuckingHeight);
                    } else if ( IsStanding ) {
                        ResizeBody(CharacterStandingWidth, CharacterStandingHeight);
                    } else {
                        ResizeBody(CharacterJumpingWidth, CharacterJumpingHeight);
                    }
                }
                _isScooting = value;
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

            if ( _terrainChanged ) {
                UpdateStanding();
                _terrainChanged = false;
            }

            ProcessResize();

            HandleJump(gameTime);

            HandleShot(gameTime);

            HandleMovement(leftStick, gameTime);

            HandleScooter(gameTime);

            UpdateImage(gameTime);

            HandleSonar(gameTime);

            foreach ( IGameEntity shot in _shots ) {
                shot.Update(gameTime);
            }
            _shots.RemoveAll(shot => shot.Disposed);
        }

        /// <summary>
        /// Handles firing any weapons
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.X)
                || InputHelper.Instance.IsNewButtonPress(Buttons.RightTrigger) ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                _shots.Add(new Shot(position, _world, shotDirection));
            } else if ( InputHelper.Instance.IsNewButtonPress(Buttons.LeftShoulder) ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                _shots.Add(new Missile(position, _world, shotDirection));
            }
        }

        private Direction GetAimDirection() {
            Direction? aimDirection =
                InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Right);
            if ( aimDirection == null ) {
                aimDirection = _facingDirection;
            }

            // Left stick always overrides right stick, unless just running or ducking
            if ( !IsDucking && (!IsStanding || IsStandingStill()) ) {
                Direction? leftStickDirection =
                    InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Left);
                if ( leftStickDirection != null && leftStickDirection != Direction.Left &&
                     leftStickDirection != Direction.Right ) {
                    aimDirection = leftStickDirection.Value;
                }
            }

            // cull out aiming directions that aren't possible
            switch ( _facingDirection ) {
                case Direction.Left:
                    switch ( aimDirection.Value ) {
                        case Direction.Right:
                            aimDirection = _facingDirection;
                            break;
                        case Direction.UpRight:
                            aimDirection = Direction.Up;
                            break;
                        case Direction.DownRight:
                            aimDirection = Direction.Down;
                            break;
                    }
                    break;
                case Direction.Right:
                    switch ( aimDirection.Value ) {
                        case Direction.Left:
                            aimDirection = _facingDirection;
                            break;
                        case Direction.UpLeft:
                            aimDirection = Direction.Up;
                            break;
                        case Direction.DownLeft:
                            aimDirection = Direction.Down;
                            break;
                    }
                    break;
            }
            return aimDirection.Value;
        }

        /// <summary>
        /// Returns the original location and direction to place a new shot in the game world.
        /// </summary>
        private Vector2 GetShotParameters(out Direction shotDirection) {
            shotDirection = GetAimDirection();

            Vector2 position = _body.Position;
            switch ( shotDirection ) {
                case Direction.Right:
                    position += new Vector2(CharacterStandingWidth / 2f, -CharacterStandingHeight / 4.5f);
                    break;
                case Direction.Left:
                    position += new Vector2(-(CharacterStandingWidth / 2f), -CharacterStandingHeight / 4.5f);
                    break;
                case Direction.Down:
                    position += new Vector2(0, CharacterStandingHeight / 2 - .1f);
                    break;
                case Direction.Up:
                    position += new Vector2(0, -CharacterStandingHeight / 2 + .1f);
                    break;
                case Direction.UpLeft:
                    position += new Vector2(-CharacterStandingWidth / 2 + .1f, -CharacterStandingHeight / 2 + .1f);
                    break;
                case Direction.UpRight:
                    position += new Vector2(CharacterStandingWidth / 2 - .1f, -CharacterStandingHeight / 2 + .1f);
                    break;
                case Direction.DownLeft:
                    position += new Vector2(-CharacterStandingWidth / 2 + .1f, -CharacterStandingHeight / 4 + -.1f);
                    break;
                case Direction.DownRight:
                    position += new Vector2(CharacterStandingWidth / 2 - .1f, -CharacterStandingHeight / 4 + -.1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("shotDirection");
            }

            if ( IsDucking && shotDirection != Direction.Down ) {
                position += new Vector2(0, CharacterStandingHeight / 3f);
            }
            return position;
        }

        /// <summary>
        /// Handles movement input, both on the ground and in the air.
        /// </summary>
        private void HandleMovement(Vector2 movementInput, GameTime gameTime) {
            bool isDucking = false;

            if ( _timeUntilRegainControl <= 0 ) {
                if ( IsStanding ) {
                    Direction? leftStickDirection = InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Left);
                    if ( leftStickDirection != null ) {
                        switch ( leftStickDirection.Value ) {
                            case Direction.Left:
                            case Direction.DownLeft:
                            case Direction.UpLeft:
                                _facingDirection = Direction.Left;
                                if ( _body.LinearVelocity.X > -Constants[PlayerInitSpeedMs] ) {
                                    _body.LinearVelocity = new Vector2(-Constants[PlayerInitSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                                    if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.B) ) {
                                        _body.LinearVelocity -= new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else {
                                    _body.LinearVelocity = new Vector2(-Constants[PlayerMaxSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Right:
                            case Direction.UpRight:
                            case Direction.DownRight:
                                _facingDirection = Direction.Right;
                                if ( _body.LinearVelocity.X < Constants[PlayerInitSpeedMs] ) {
                                    _body.LinearVelocity = new Vector2(Constants[PlayerInitSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxSpeedMs] ) {
                                    if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.B) ) {
                                        _body.LinearVelocity += new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else {
                                    _body.LinearVelocity = new Vector2(Constants[PlayerMaxSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Down:
                                isDucking = true;
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                break;
                            case Direction.Up:
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                break;
                            default:
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                break;
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
                            _facingDirection = Direction.Left;
                        }
                    } else if ( movementInput.X > 0 ) {
                        if ( _body.LinearVelocity.X < Constants[PlayerMaxSpeedMs] ) {
                            _body.LinearVelocity += new Vector2(
                                GetVelocityDelta(Constants[PlayerAirAccelerationMss], gameTime), 0);
                        } else {
                            _body.LinearVelocity = new Vector2(Constants[PlayerMaxSpeedMs], _body.LinearVelocity.Y);
                        }
                        if ( _body.LinearVelocity.X >= 0 ) {
                            _facingDirection = Direction.Right;
                        }
                    }
                }
            }

            IsDucking = isDucking;
        }

        private float GetVelocityDelta(float acceleration, GameTime gameTime) {
            return gameTime.ElapsedGameTime.Milliseconds / 1000f * acceleration;
        }

        /// <summary>
        /// Handles jump input 
        /// </summary>
        private void HandleJump(GameTime gameTime) {

            if ( InputHelper.Instance.IsNewButtonPress(Buttons.A) ) {
                if ( IsStanding ) {
                    _jumpInitiated = true;
                    _airBoostTime = 0;
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                }
            } else if ( InputHelper.Instance.GamePadState.IsButtonDown(Buttons.A) ) {
                if ( IsTouchingCeiling ) {
                    _airBoostTime = -1;
                } else if ( _airBoostTime >= 0
                    && _airBoostTime < Constants[PlayerAirBoostTime] * 1000 ) {
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                    _airBoostTime += gameTime.ElapsedGameTime.Milliseconds;
                }
            } else {
                _airBoostTime = -1;
                _jumpInitiated = false;
            }
        }

        /// <summary>
        /// Handles the scooter device controls
        /// </summary>
        private void HandleScooter(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.LeftTrigger) && IsDucking ) {
                _scooterInitiated = true;
                IsScooting = true;
            } else if ( !InputHelper.Instance.GamePadState.IsButtonDown(Buttons.LeftTrigger) ) {
                EndScooter();
            }
        }

        /// <summary>
        /// Makes sure that the scooter isn't boxed in to the left and right, and forces a stand if so.
        /// </summary>
        private void EnsureRoomForScooter() {
            var contactEdge = _body.ContactList;
            bool boxedLeft = false;
            bool boxedRight = false;
            FixedArray2<Vector2> points;
            while ( contactEdge != null ) {
                if ( contactEdge.Contact.IsTouching() &&
                     (contactEdge.Other.GetUserData().IsTerrain || contactEdge.Other.GetUserData().IsDoor) ) {
                    Vector2 normal;
                    contactEdge.Contact.GetWorldManifold(out normal, out points);
                    if ( normal.X < -.8 ) {
                        boxedRight = true;
                    } else if ( normal.X > .8 ) {
                        boxedLeft = true;
                    }
                }
                contactEdge = contactEdge.Next;
            }

            if ( boxedLeft && boxedRight ) {
                EndScooter();
            }
        }

        /// <summary>
        /// Ends the scooter session
        /// </summary>
        private void EndScooter() {
            if ( IsScooting ) {
                _endScooterInitiated = true;
            }
            IsScooting = false;
            _scooterInitiated = false;
        }


        private bool _resizeRequested;
        private float _nextHeight;
        private float _nextWidth;
        private Vector2 _positionCorrection;

        /// <summary>
        /// Resizes the body while keeping the lower edge in the same position and the X position constant.
        /// </summary>
        private void ResizeBody(float width, float height, Vector2 positionalCorrection = new Vector2()) {
            _nextHeight = height;
            _nextWidth = width;
            _positionCorrection = positionalCorrection;
            _resizeRequested = true;
        }

        /// <summary>
        /// When we change the size of the body, we sometimes need to nudge it a bit first to prevent 
        /// Box2D allowing it to pass through terrain edges, then allow a world step to take place 
        /// before processing the resize.  This function gets called by Update() to change the shape of
        /// the body one step *after* any corrections were performed.
        /// </summary>
        private void ProcessResize() {
            if ( _resizeRequested ) {

                float halfHeight = _nextHeight / 2;
                var newPosition = GetNewBodyPosition(halfHeight);
                _body.Position = newPosition;

                PolygonShape shape = (PolygonShape) _body.FixtureList.First().Shape;
                shape.SetAsBox(_nextWidth / 2, halfHeight);
                Height = _nextHeight;
                Width = _nextWidth;
                _resizeRequested = false;
            }
        }

        /// <summary>
        /// Returns the position of the body if the half-height is as indicated, 
        /// holding the Y position of the bottom edge constant.
        /// </summary>
        private Vector2 GetNewBodyPosition(float halfHeight) {
            Vector2 position = _body.Position;
            float oldYPos = position.Y + Height / 2;
            float newYPos = position.Y + halfHeight;
            Vector2 newPosition = new Vector2(position.X, position.Y + (oldYPos - newYPos)) + _positionCorrection;
            return newPosition;
        }

        private void HandleSonar(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.Y) 
                || InputHelper.Instance.IsNewButtonPress(Buttons.RightShoulder) ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                _shots.Add(new Sonar(_world, position, shotDirection));
            }
        }

        #region Animation

        private enum Animation {
            AimStraight,
            AimUp,
            AimDiagonalUp,
            AimDiagonalDown,
            AimDown,
            WalkAimStraight,
            WalkAimUp,
            WalkAimDiagonalUp,
            WalkAimDiagonalDown,
            WalkAimDown,
            JogAimStraight,
            JogAimUp,
            JogAimDiagonalUp,
            JogAimDiagonalDown,
            JogAimDown,
            RunAimStraight,
            RunAimUp,
            RunAimDiagonalUp,
            RunAimDiagonalDown,
            RunAimDown,
            CrouchAimStraight,
            CrouchAimUp,
            CrouchAimDiagonalUp,
            CrouchAimDiagonalDown,
            CrouchAimDown,
            JumpInit,
            JumpAimStraight,
            JumpAimUp,
            JumpAimDiagonalUp,
            JumpAimDiagonalDown,
            JumpAimDown,
            LieDown,
            StandUp,
            Scoot,
        };

        private int _animationFrame;
        private long _timeSinceLastAnimationUpdate;
        private bool _jumpInitiated;
        private bool _scooterInitiated;
        private bool _endScooterInitiated;

        private Animation _currentAnimation = Animation.AimStraight;
        private Animation _prevAnimation = Animation.AimStraight;

        private const int NumWalkAimStraightFrames = 30;
        private const int NumWalkAimFrames = 16;
        private const int NumJogFrames = 13;

        private const int NumRunAimStraightFrames = 22;
        private const int NumRunAimFrames = 11;
        
        private const int NumCrouchFrames = 11;
        private const int CrouchAimUpFrame = 1;
        private const int CrouchAimUpRightFrame = 3;
        private const int CrouchAimStraightFrame = 5;
        private const int CrouchAimDownRightFrame = 7;
        private const int CrouchAimDownFrame = 9;

        private const int NumStandAimFrames = 14;
        private const int AimUpFrame = 0;
        private const int AimUpRightFrame = 3;
        private const int AimRightFrame = 6;
        private const int AimDownRightFrame = 9;
        private const int AimDownFrame = 12;

        private const int NumJumpFrames = 9;
        private const int JumpAimStraightFrame = 6;
        private const int JumpAimUpFrame = 4;
        private const int JumpAimUpRightFrame = 5;
        private const int JumpAimDownRightFrame = 7;
        private const int JumpAimDownFrame = 8;

        private const int NumScooterFrames = 17;
        private const int ScootFrame = 9;
        private const int NumScootFrames = NumScooterFrames - ScootFrame;

        private readonly Texture2D[] _standAimAnimation = new Texture2D[NumStandAimFrames];

        private readonly Texture2D[] _walkAimUpAnimation = new Texture2D[NumWalkAimFrames];
        private readonly Texture2D[] _walkAimDiagonalUpAnimation = new Texture2D[NumWalkAimFrames];
        private readonly Texture2D[] _walkAimStraightAnimation = new Texture2D[NumWalkAimStraightFrames];
        private readonly Texture2D[] _walkAimDiagonalDownAnimation = new Texture2D[NumWalkAimFrames];
        private readonly Texture2D[] _walkAimDownAnimation = new Texture2D[NumWalkAimFrames];

        private readonly Texture2D[] _jogAimUpAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDiagonalUpAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimStraightAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDiagonalDownAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDownAnimation = new Texture2D[NumJogFrames];

        private readonly Texture2D[] _runAimUpAnimation = new Texture2D[NumRunAimFrames];
        private readonly Texture2D[] _runAimDiagonalUpAnimation = new Texture2D[NumRunAimFrames];
        private readonly Texture2D[] _runAimStraightAnimation = new Texture2D[NumRunAimStraightFrames];
        private readonly Texture2D[] _runAimDiagonalDownAnimation = new Texture2D[NumRunAimFrames];
        private readonly Texture2D[] _runAimDownAnimation = new Texture2D[NumRunAimFrames];

        private readonly Texture2D[] _jumpAnimation = new Texture2D[NumJumpFrames];
        private readonly Texture2D[] _crouchAnimation = new Texture2D[NumCrouchFrames];

        private readonly Texture2D[] _scooterAnimation = new Texture2D[NumScooterFrames];

        private void LoadAnimations(ContentManager content) {
            for ( int i = 0; i < NumStandAimFrames; i++ ) {
                _standAimAnimation[i] = content.Load<Texture2D>(String.Format("Character/StandAim/StandAim{0:0000}", i));
            }

            for ( int i = 0; i < NumWalkAimStraightFrames; i++ ) {
                _walkAimStraightAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkStraight/GunWalkStraight{0:0000}", i));
            }
            for ( int i = 0; i < NumWalkAimFrames; i++ ) {
                _walkAimUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkUp/GunWalkUp{0:0000}", i));
                _walkAimDiagonalUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkDiagonalUp/GunWalkDiagonalUp{0:0000}", i));
                _walkAimDiagonalDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkDiagonalDown/GunWalkDiagonalDown{0:0000}", i));
                _walkAimDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkDown/GunWalkDown{0:0000}", i));
            }

            for ( int i = 0; i < NumJogFrames; i++ ) {
                _jogAimStraightAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJogStraight/GunJogStraight{0:0000}", i));
                _jogAimUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJogUp/GunJogUp{0:0000}", i));
                _jogAimDiagonalUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJogDiagonalUp/GunJogDiagonalUp{0:0000}", i));
                _jogAimDiagonalDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJogDiagonalDown/GunJogDiagonalDown{0:0000}", i));
                _jogAimDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJog/GunJogDown/GunJogDown{0:0000}", i));
            }

            
            for ( int i = 0; i < NumRunAimStraightFrames; i++ ) {
                _runAimStraightAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRunStraight/GunRunStraight{0:0000}", i));
            }
            for ( int i = 0; i < NumRunAimFrames; i++ ) {
                _runAimUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRunUp/GunRunUp{0:0000}", i));
                _runAimDiagonalUpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRunDiagonalUp/GunRunDiagonalUp{0:0000}", i));
                _runAimDiagonalDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRunDiagonalDown/GunRunDiagonalDown{0:0000}", i));
                _runAimDownAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRunDown/GunRunDown{0:0000}", i));
            }

            for ( int i = 0; i < NumJumpFrames; i++ ) {
                _jumpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJump/GunJump{0:0000}", i));
            }
            for ( int i = 0; i < NumCrouchFrames; i++ ) {
                _crouchAnimation[i] = content.Load<Texture2D>(String.Format("Character/Crouch/Crouch{0:0000}", i));
            }

            for ( int i = 0; i < NumScooterFrames; i++ ) {
                _scooterAnimation[i] = content.Load<Texture2D>(String.Format("Character/Scooter/Scooter{0:0000}", i));
            }

            Image = _standAimAnimation[AimRightFrame];
        }

        /// <summary>
        /// Updates the current image for the next frame
        /// </summary>
        private void UpdateImage(GameTime gameTime) {

            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;

            var aimDirection = GetAimDirection();

            if ( IsStanding && !_jumpInitiated ) {
                if ( IsScootingAnimation() ) {
                    AnimateScooter();
                } else if ( IsStandingStill() ) {
                    AnimateStandingStill(aimDirection);
                } else if ( IsWalkingSpeed() ) {
                    AnimateWalking(aimDirection);
                } else if ( IsJoggingSpeed() ) {
                    AnimateJogging(aimDirection);
                } else {
                    AnimateRunning(aimDirection);
                }
            } else {
                AnimateJumping(aimDirection);
            }

            _prevAnimation = _currentAnimation;
        }

        private bool IsScootingAnimation() {
            return IsScooting || _endScooterInitiated;
        }

        /// <summary>
        /// Animating the scooter is a bit unusual since we nudge the 
        /// player body in one direction or the other to make up for differences 
        /// in x offsets of the animations.
        /// </summary>
        private void AnimateScooter() {
            if ( _scooterInitiated ) {
                _endScooterInitiated = false;
                _currentAnimation = Animation.LieDown;
                if ( _currentAnimation != _prevAnimation ) {
                    _animationFrame = 0;
                }

                if ( _timeSinceLastAnimationUpdate > 0 || _currentAnimation != _prevAnimation ) {
                    Image = _scooterAnimation[_animationFrame++];
                    if ( _animationFrame >= ScootFrame ) {
                        _scooterInitiated = false;
                        EnsureRoomForScooter();
                    }
                }
            } else if ( _endScooterInitiated ) {
                _currentAnimation = Animation.StandUp;
                if ( _currentAnimation != _prevAnimation && _animationFrame > ScootFrame ) {
                    _animationFrame = ScootFrame;
                }
                if ( _timeSinceLastAnimationUpdate > 0 || _currentAnimation != _prevAnimation ) {
                    Image = _scooterAnimation[_animationFrame--];
                    if ( _animationFrame < 0 ) {
                        _animationFrame = 0;
                        _endScooterInitiated = false;

                        if ( _facingDirection == Direction.Right ) {
                            ResizeBody(Width, Height, new Vector2(-ScooterNudge, 0));
                        } else if ( _facingDirection == Direction.Left ) {
                            ResizeBody(Width, Height, new Vector2(ScooterNudge, 0));
                        }
                    }
                }
            } else {
                _currentAnimation = Animation.Scoot;
                float speed = Math.Abs(_body.LinearVelocity.X) * Constants[PlayerWheelSpinSpeedMultiplier];
                if ( _timeSinceLastAnimationUpdate > 1000 / NumScootFrames / speed
                    || _prevAnimation != _currentAnimation ) {
                    _animationFrame %= NumScooterFrames;
                    if ( _animationFrame == 0 ) {
                        _animationFrame = ScootFrame;
                    }
                    Image = _scooterAnimation[_animationFrame++];
                }
            }
        }

        private void AnimateJumping(Direction aimDirection) {

            if ( _jumpInitiated ) {
                _currentAnimation = Animation.JumpInit;
                if ( _currentAnimation != _prevAnimation ) {
                    _animationFrame = 0;
                }
                Image = _jumpAnimation[_animationFrame++];
                if ( _animationFrame >= JumpAimUpFrame ) {
                    _jumpInitiated = false;
                }
            } else {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.Right:
                        _currentAnimation = Animation.JumpAimStraight;
                        if ( _currentAnimation != _prevAnimation ) {
                            Image = _jumpAnimation[JumpAimStraightFrame];
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.JumpAimUp;
                        if ( _currentAnimation != _prevAnimation ) {
                            Image = _jumpAnimation[JumpAimUpFrame];
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.JumpAimDown;
                        if ( _currentAnimation != _prevAnimation ) {
                            Image = _jumpAnimation[JumpAimDownFrame];
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.JumpAimDiagonalUp;
                        if ( _currentAnimation != _prevAnimation ) {
                            Image = _jumpAnimation[JumpAimUpRightFrame];
                        }

                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.JumpAimDiagonalDown;
                        if ( _currentAnimation != _prevAnimation ) {
                            Image = _jumpAnimation[JumpAimDownRightFrame];
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void AnimateRunning(Direction aimDirection) {
            float runSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);

            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.RunAimStraight;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunAimStraightFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumRunAimStraightFrames;
                        Image = _runAimStraightAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.RunAimUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunAimStraightFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumRunAimFrames;
                        Image = _runAimUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.RunAimDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunAimStraightFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumRunAimFrames;
                        Image = _runAimDownAnimation[_animationFrame++];
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.RunAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunAimStraightFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumRunAimFrames;
                        Image = _runAimDiagonalUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.RunAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunAimStraightFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumRunAimFrames;
                        Image = _runAimDiagonalDownAnimation[_animationFrame++];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateJogging(Direction aimDirection) {
            float jogSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);

            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.JogAimStraight;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumJogFrames;
                        Image = _jogAimStraightAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.JogAimUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumJogFrames;
                        Image = _jogAimUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.JogAimDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumJogFrames;
                        Image = _jogAimDownAnimation[_animationFrame++];
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.JogAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumJogFrames;
                        Image = _jogAimDiagonalUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.JogAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumJogFrames;
                        Image = _jogAimDiagonalDownAnimation[_animationFrame++];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateWalking(Direction aimDirection) {
            float walkSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerWalkSpeedMultiplier]);

            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.WalkAimStraight;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkAimStraightFrames / walkSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumWalkAimStraightFrames;
                        Image = _walkAimStraightAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.WalkAimUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkAimStraightFrames / walkSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumWalkAimFrames;
                        Image = _walkAimUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.WalkAimDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkAimStraightFrames / walkSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumWalkAimFrames;
                        Image = _walkAimDownAnimation[_animationFrame++];
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.WalkAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkAimStraightFrames / walkSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumWalkAimFrames;
                        Image = _walkAimDiagonalUpAnimation[_animationFrame++];
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.WalkAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumWalkAimStraightFrames / walkSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= NumWalkAimFrames;
                        Image = _walkAimDiagonalDownAnimation[_animationFrame++];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateStandingStill(Direction aimDirection) {
            if ( !IsDucking ) {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.Right:
                        _currentAnimation = Animation.AimStraight;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < AimRightFrame
                                 || _animationFrame > AimRightFrame + 1 ) {
                                _animationFrame = AimRightFrame;
                            }
                            Image = _standAimAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.AimUp;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < AimUpFrame
                                 || _animationFrame > AimUpFrame + 1 ) {
                                _animationFrame = AimUpFrame;
                            }
                            Image = _standAimAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.AimDown;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < AimDownFrame
                                 || _animationFrame > AimDownFrame + 1 ) {
                                _animationFrame = AimDownFrame;
                            }
                            Image = _standAimAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.AimDiagonalUp;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < AimUpRightFrame
                                 || _animationFrame > AimUpRightFrame + 1 ) {
                                _animationFrame = AimUpRightFrame;
                            }
                            Image = _standAimAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.AimDiagonalDown;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < AimDownRightFrame
                                 || _animationFrame > AimDownRightFrame + 1 ) {
                                _animationFrame = AimDownRightFrame;
                            }
                            Image = _standAimAnimation[_animationFrame++];
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.Right:
                        _currentAnimation = Animation.CrouchAimStraight;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < CrouchAimStraightFrame
                                 || _animationFrame > CrouchAimStraightFrame + 1 ) {
                                _animationFrame = CrouchAimStraightFrame;
                            }
                            Image = _crouchAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.CrouchAimUp;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < CrouchAimUpFrame
                                 || _animationFrame > CrouchAimUpFrame + 1 ) {
                                _animationFrame = CrouchAimUpFrame;
                            }
                            Image = _crouchAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.CrouchAimDown;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < CrouchAimDownFrame
                                 || _animationFrame > CrouchAimDownFrame + 1 ) {
                                _animationFrame = CrouchAimDownFrame;
                            }
                            Image = _crouchAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.CrouchAimDiagonalUp;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < CrouchAimUpRightFrame
                                 || _animationFrame > CrouchAimUpRightFrame + 1 ) {
                                _animationFrame = CrouchAimUpRightFrame;
                            }
                            Image = _crouchAnimation[_animationFrame++];
                        }
                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.CrouchAimDiagonalDown;
                        if ( _timeSinceLastAnimationUpdate > 500f
                             || _prevAnimation != _currentAnimation ) {
                            if ( _animationFrame < CrouchAimDownRightFrame
                                 || _animationFrame > CrouchAimDownRightFrame + 1 ) {
                                _animationFrame = CrouchAimDownRightFrame;
                            }
                            Image = _crouchAnimation[_animationFrame++];
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private bool IsStandingStill() {
            return _body.LinearVelocity.X == 0;
        }

        private bool IsJoggingSpeed() {
            return Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerMaxSpeedMs] * .75f;
        }

        private bool IsWalkingSpeed() {
            return Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerInitSpeedMs];
        }

        #endregion

        private readonly List<IGameEntity> _shots = new List<IGameEntity>();

        public void HitBy(Enemy enemy) {
            Vector2 diff = Position - enemy.Position;
            _body.LinearVelocity = new Vector2(0);
            _body.ApplyLinearImpulse(diff * Constants[PlayerKnockbackAmt] * _body.Mass);
            _timeUntilRegainControl = (long) (Constants[PlayerKnockbackTime] * 1000);
            Health -= 10;
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public void Pickup(HealthPickup healthPickup) {
            Health += 10;
        }

        private bool _terrainChanged;

        /// <summary>
        /// Unfortunately, we can't rely on Box2d to propertly notify us when we hit or 
        /// leave the ground or ceiling when bodies are being created or destroyed.  
        /// This method allows knowledgable callers to suggest updating the standing info 
        /// on the next update cycle.
        /// </summary>
        public void NotifyTerrainChange() {
            _terrainChanged = true;
        }
    }

    public enum Direction {
        Left,
        Right,
        Up,
        Down,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
    };
}

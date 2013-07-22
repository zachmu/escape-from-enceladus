using System;
using System.Collections.Generic;
using System.Linq;
using Enceladus.Event;
using Enceladus.Map;
using Enceladus.Xbox;
using Enceladus.Control;
using Enceladus.Entity.Enemy;
using Enceladus.Farseer;
using Enceladus.Weapon;
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
using ConvertUnits = Enceladus.Farseer.ConvertUnits;

namespace Enceladus.Entity {
    public class Player : IGameEntity, ISaveable {

        private static Player _instance;
        public static Player Instance {
            get { return _instance; }
        }

        #region Constants

        private const float CharacterStandingHeight = 1.9f;
        private const float CharacterStandingWidth = .6f;
        private const float CharacterJumpingHeight = 1.7f;
        private const float CharacterJumpingWidth = .6f;
        private const float CharacterDuckingHeight = 1.3f;
        private const float CharacterDuckingWidth = .6f;
        private const float CharacterScootingHeight = .5f;
        private const float CharacterScootingWidth = 1.7f;
        private const float ScooterNudge = CharacterScootingWidth / 2 - CharacterDuckingWidth / 2 + .03f;
        // How far ahead of the standing / ducking position the world must be clear to scoot freely.
        private const float ScooterForwardClearance = ScooterNudge + CharacterScootingWidth / 2;

        public float Height { get; private set; }
        public float Width { get; private set; }
        
        private static readonly Constants Constants = Constants.Instance;
        private const string PlayerInitSpeedMs = "Player initial walk speed (m/s)";
        private const string PlayerInitRunSpeedMs = "Player initial run speed (m/s)";
        private const string PlayerMaxGroundSpeedMs = "Player max run speed (m/s)";
        private const string PlayerAccelerationMss = "Player acceleration (m/s/s)";
        private const string PlayerFastJumpSpeed = "Player fast jump speed (m/s)";
        private const string PlayerAirBrakeMss = "Player horizontal air brake (m/s/s)";
        private const string PlayerJumpSpeed = "Player jump speed (m/s)";
        private const string PlayerAirBoostTime = "Player max air boost time (s)";
        private const string PlayerKnockbackTime = "Player knock back time (s)";
        private const string PlayerKnockbackAmt = "Player knock back amount (scalar)";
        private const string PlayerJogSpeedMultiplier = "Player jog speed multiplier";
        private const string PlayerWalkSpeedMultiplier = "Player walk speed multiplier";
        private const string PlayerWheelSpinSpeedMultiplier = "Player wheel spin speed multipler";
        private const string PlayerScooterOffset = "Player scooter offset";
        private const string ProjectileOffsetX = "Projectile offset X";
        private const string ProjectileOffsetY = "Projectile offset Y";

        static Player() {
            Constants.Register(new Constant(PlayerInitSpeedMs, 3.5f, Keys.I));
            Constants.Register(new Constant(PlayerInitRunSpeedMs, 7.0f, Keys.O));
            Constants.Register(new Constant(PlayerAccelerationMss, 5.0f, Keys.A));
            Constants.Register(new Constant(PlayerFastJumpSpeed, 5f, Keys.M));
            Constants.Register(new Constant(PlayerMaxGroundSpeedMs, 20, Keys.S));
            Constants.Register(new Constant(PlayerAirBrakeMss, 7.0f, Keys.D));
            Constants.Register(new Constant(PlayerJumpSpeed, 10f, Keys.J));
            Constants.Register(new Constant(PlayerAirBoostTime, .4f, Keys.D4));
            Constants.Register(new Constant(PlayerKnockbackTime, .3f, Keys.K));
            Constants.Register(new Constant(PlayerKnockbackAmt, 5f, Keys.L));
            Constants.Register(new Constant(PlayerJogSpeedMultiplier, .5f, Keys.B, .01f));
            Constants.Register(new Constant(PlayerWalkSpeedMultiplier, .5f, Keys.N));
            Constants.Register(new Constant(PlayerWheelSpinSpeedMultiplier, 1.0f, null));
            Constants.Register(new Constant(PlayerScooterOffset, 0f, Keys.P));
            Constants.Register(new Constant(ProjectileOffsetX, 0f, Keys.X, .01f));
            Constants.Register(new Constant(ProjectileOffsetY, 0f, Keys.Y, .01f));
        }

        #endregion

        private Direction _facingDirection = Direction.Right;

        /// <summary>
        /// How long, in milliseconds, the player has been holding down the jump button.
        /// </summary>
        private long _airBoostTime = -1;

        /// <summary>
        /// How long, in ms, the player must wait before regaining control after being knocked back
        /// </summary>
        private long _timeUntilRegainControl;

        private Color _color = Color.SteelBlue;
        public Color Color { get { return _color; } }

        public bool Disposed {
            get { return _body == null || _body.IsDisposed; }
        }

        public Vector2 Position {
            get { return _body.Position; }
            set { _body.Position = value; }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        public Vector2 LinearVelocity {
            get { return _body.LinearVelocity; }
        }

        public Player(Vector2 position, World world) {
            _instance = this;
            
            HealthCapacity = 650;
            Health = 100;

            Equipment = new Equipment();

            _world = world;
        }

        // Creates the simulated body at the specified position
        public void CreateBody(Vector2 position) {
            if ( !Disposed ) {
                Dispose();
            }

            _body = BodyFactory.CreateRectangle(_world, CharacterStandingWidth, CharacterStandingHeight, 10f);
            _body.FixtureList.First().UserData = "body";
            Height = CharacterStandingHeight;
            Width = CharacterStandingWidth;
            ConfigureBody(position);            
        }

        private void ConfigureBody(Vector2 position) {
            _body.IsStatic = false;
            _body.Restitution = 0.0f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.SleepingAllowed = false;
            _body.CollidesWith = EnceladusGame.TerrainCategory | EnceladusGame.EnemyCategory | EnceladusGame.PlayerSensorCategory;
            _body.CollisionCategories = EnceladusGame.PlayerCategory;
            _body.UserData = UserData.NewPlayer();
            _body.FixtureList.First().UserData = "body";

            _body.OnCollision += (a, b, contact) => {
                if ( contact.IsTouching() ) {
                    UpdateStanding();
                }
                return true;
            };
            _body.OnSeparation += (a, b) => UpdateStanding();
        }

        public int Health { get; private set; }
        public int HealthCapacity { get; private set; }

        public Equipment Equipment { get; private set; }

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
            LandSound = content.Load<SoundEffect>("Sounds/land");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // Draw origin is character's feet
                Vector2 position = _body.Position;
                position.Y += Height/2;
                position += _imageDrawOffset;

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : _color;
                spriteBatch.Draw(Image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width,
                                               Image.Height),
                                 null, color, 0f, new Vector2(Image.Width/2, Image.Height - 1),
                                 _facingDirection == Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);
            }
        }

        /// <summary>
        /// Whether or not the character is standing on solid ground.
        /// </summary>
        private bool _isStanding;
        protected bool IsStanding {
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

        protected Vector2 GetStandingLocation() {
            return _body.Position + new Vector2(0, Height / 2);
        }

        /// <summary>
        /// Whether or not the character is ducking, on the ground
        /// </summary>
        private bool _isDucking;
        public bool IsDucking {
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

        /// <summary>
        /// Whether to abort the scooting attempt due to space.
        /// </summary>
        private bool _abortScooting;

        /// <summary>
        /// Whether or not the character is scooting
        /// </summary>
        public bool IsScooting {
            get { return _isScooting; }
            set {
                if ( value && !_isScooting ) {

                    _abortScooting = false;

                    // Make sure we're not too close to a vertical wall
                    float nudgeAmount = ScooterNudge;
                    float positionCorrectionAmount = 0;
                    float forwardClearance = ScooterForwardClearance;
                    if ( _facingDirection == Direction.Left ) {
                        nudgeAmount = -nudgeAmount;
                        forwardClearance = -forwardClearance;
                    }

                    Vector2 startRay = new Vector2(_body.Position.X,
                                                   _body.Position.Y + CharacterDuckingHeight / 2 -
                                                   CharacterScootingHeight / 2 - .02f);
                    Vector2 endRay = startRay + new Vector2(forwardClearance, 0);
                    bool roomAhead = true;
                    bool roomBehind = true;

                    _world.RayCast((fixture, point, normal, fraction) => {
                        if ( fixture.GetUserData().IsDoor || fixture.GetUserData().IsTerrain ) {
                            roomAhead = false;
                            positionCorrectionAmount = ScooterForwardClearance - Math.Abs(point.X - _body.Position.X) +
                                                       .01f;
                            return 0;
                        }
                        return -1;
                    },
                                   startRay, endRay);

                    if ( !roomAhead ) {
                        if ( _facingDirection == Direction.Right ) {
                            endRay = startRay - new Vector2(Width / 2f + positionCorrectionAmount, 0);
                        } else {
                            endRay = startRay + new Vector2(Width / 2f + positionCorrectionAmount, 0);
                        }

                        _world.RayCast((fixture, point, normal, fraction) => {
                            if ( fixture.GetUserData().IsDoor || fixture.GetUserData().IsTerrain ) {
                                roomBehind = false;
                                return 0;
                            }
                            return -1;
                        },
                                       startRay, endRay);
                    }

                    Vector2 nudge = new Vector2(nudgeAmount, 0);
                    if ( !roomAhead && roomBehind ) {
                        if ( _facingDirection == Direction.Right ) {
                            nudge += new Vector2(-positionCorrectionAmount, 0);
                        } else {
                            nudge += new Vector2(positionCorrectionAmount, 0);
                        }
                    }

                    if ( roomBehind ) {
                        ResizeBody(CharacterScootingWidth, CharacterScootingHeight, nudge);
                    } else {
                        _abortScooting = true;
                    }

                } else if ( !value && _isScooting && !_abortScooting ) {
                    // Make sure that we have room overhead to stand back up.  Check both overhead corners.
                    List<Vector2> startRays = new List<Vector2>();
                    if ( _facingDirection == Direction.Right ) {
                        startRays.Add(_body.Position +
                                      new Vector2(-ScooterNudge - CharacterDuckingWidth / 2f, CharacterScootingHeight / 2));
                        startRays.Add(_body.Position +
                                      new Vector2(-ScooterNudge + CharacterDuckingWidth / 2f, CharacterScootingHeight / 2));
                    } else {
                        startRays.Add(_body.Position +
                                      new Vector2(ScooterNudge + CharacterDuckingWidth / 2f, CharacterScootingHeight / 2));
                        startRays.Add(_body.Position +
                                      new Vector2(ScooterNudge - CharacterDuckingWidth / 2f, CharacterScootingHeight / 2));
                    }

                    bool roomToStand = true;
                    foreach ( Vector2 startRay in startRays ) {
                        Vector2 endRay = startRay + new Vector2(0, -CharacterStandingHeight);
                        _world.RayCast((fixture, point, normal, fraction) => {
                            if ( fixture.GetUserData().IsDoor || fixture.GetUserData().IsTerrain ) {
                                roomToStand = false;
                                return 0;
                            }
                            return -1;
                        },
                                       startRay, endRay);
                    }

                    // If there's no room to stand up, simply disallow it and keep scooting.
                    if ( !roomToStand )
                        return;

                    Vector2 nudge = _facingDirection == Direction.Right
                                        ? new Vector2(-ScooterNudge, 0)
                                        : new Vector2(ScooterNudge, 0);
                    if ( IsDucking ) {
                        ResizeBody(CharacterDuckingWidth, CharacterDuckingHeight, nudge);
                    } else if ( IsStanding ) {
                        ResizeBody(CharacterStandingWidth, CharacterStandingHeight, nudge);
                    } else {
                        ResizeBody(CharacterJumpingWidth, CharacterJumpingHeight, nudge);
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

            UpdateBookkeepingCounters(gameTime);

            HandleJump(gameTime);

            HandleShot(gameTime);

            HandleMovement(gameTime);

            HandleScooter(gameTime);

            UpdateImage(gameTime);

            HandleSonar(gameTime);

            UpdateFlash(gameTime);
        }

        /// <summary>
        /// Updates the several book-keeping figures used in character control
        /// </summary>
        private void UpdateBookkeepingCounters(GameTime gameTime) {
            if ( _timeUntilRegainControl > 0 ) {
                _timeUntilRegainControl -= gameTime.ElapsedGameTime.Milliseconds;
            }

            if ( _terrainChanged && _standingMonitor.IgnoreStandingUpdatesNextNumFrames <= 0 ) {
                UpdateStanding();
                _terrainChanged = false;
            }

            _standingMonitor.UpdateCounters();
        }

        /// <summary>
        /// Handles firing any weapons
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( PlayerControl.Control.IsNewShot() ) {
                if ( IsScooting ) {
                    Vector2 pos = Vector2.Zero;
                    if (_facingDirection == Direction.Right) {
                        pos = _body.Position + new Vector2(CharacterScootingWidth / 2, CharacterScootingHeight / 2 - Bomb.Height / 2);
                    } else {
                        pos = _body.Position + new Vector2(-CharacterScootingWidth / 2, CharacterScootingHeight / 2 - Bomb.Height / 2);                                                
                    }
                    EnceladusGame.Instance.Register(new Bomb(pos, _world, _facingDirection));
                } else {
                    Direction shotDirection;
                    var position = GetShotParameters(out shotDirection);
                    EnceladusGame.Instance.Register(new Shot(position, _world, shotDirection));
                }
            } else if ( InputHelper.Instance.IsNewButtonPress(Buttons.LeftShoulder) ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                EnceladusGame.Instance.Register(new Missile(position, _world, shotDirection));
            }
        }

        private Direction GetAimDirection() {
            Direction? aimDirection =
                PlayerControl.Control.GetAimDirection();
            if ( aimDirection == null || (aimDirection == Direction.Down && PlayerControl.Control.IsKeyboardControl()) ) {
                aimDirection = _facingDirection;
            }

            // Left stick always overrides right stick, unless just running or ducking
            if ( !IsDucking && (!IsStanding || IsStandingStill()) ) {
                Direction? movementDirection =
                    PlayerControl.Control.GetMovementDirection();
                if ( movementDirection != null && movementDirection != Direction.Left &&
                     movementDirection != Direction.Right ) {
                    aimDirection = movementDirection.Value;
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

            // tuning params
            position += new Vector2(Constants[ProjectileOffsetX], Constants[ProjectileOffsetY]);

            if ( IsDucking && shotDirection != Direction.Down ) {
                position += new Vector2(0, CharacterStandingHeight / 3f);
            }

            // Fine tuning the shot placement.  Numbers determined experimentally
            Vector2 tuning;
            switch ( shotDirection ) {
                case Direction.Left:
                case Direction.Right:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingRight;
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingRight;
                        if ( !IsStandingStill() ) {
                            tuning += new Vector2(0, -.06f);
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingRight;
                    }
                    break;
                case Direction.Up:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingUp;
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingUp;
                        if ( !IsStandingStill() ) {
                            tuning += new Vector2(-.03f, 0);
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingUp;
                    }
                    break;
                case Direction.Down:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingDown;
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingDown;
                    } else {
                        tuning = ShotAdjustmentJumpingDown;
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingUpRight;
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingUpRight;
                        if ( !IsStandingStill() ) {
                            if ( IsWalkingSpeed() ) {
                                tuning += new Vector2(0, .06f);
                            } else {
                                tuning += new Vector2(0, -.07f);
                            }
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingUpRight;
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingDownRight;
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingDownRight;
                        if ( !IsStandingStill() ) {
                            if ( IsWalkingSpeed() ) {
                                tuning += new Vector2(0, -.11f);
                            } else {
                                tuning += new Vector2(0, -.15f);
                            }
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingDownRight;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("shotDirection");
            }
            if (_facingDirection == Direction.Left) {
                tuning = new Vector2(-tuning.X, tuning.Y);
            }

            position += tuning;

            return position;
        }

        #region ShotAdjustments

        private static readonly Vector2 ShotAdjustmentStandingRight = new Vector2(-.08f, .08f);
        private static readonly Vector2 ShotAdjustmentStandingUp = new Vector2(.1f, 0f);
        private static readonly Vector2 ShotAdjustmentStandingUpRight = new Vector2(-.02f, .22f);
        private static readonly Vector2 ShotAdjustmentStandingDownRight = new Vector2(-.02f, .24f);
        private static readonly Vector2 ShotAdjustmentStandingDown = new Vector2(.18f, -.36f);
        private static readonly Vector2 ShotAdjustmentDuckingRight = new Vector2(-.02f, -.25f);
        private static readonly Vector2 ShotAdjustmentDuckingUp = new Vector2(-.05f, -.33f);
        private static readonly Vector2 ShotAdjustmentDuckingDownRight = new Vector2(0f, -.09f);
        private static readonly Vector2 ShotAdjustmentDuckingUpRight = new Vector2(0f, -.36f);
        private static readonly Vector2 ShotAdjustmentDuckingDown = new Vector2(.06f, -.26f);
        private static readonly Vector2 ShotAdjustmentJumpingRight = new Vector2(-.02f, .10f);
        private static readonly Vector2 ShotAdjustmentJumpingUp = new Vector2(.06f, .10f);
        private static readonly Vector2 ShotAdjustmentJumpingUpRight = new Vector2(.06f, .19f);
        private static readonly Vector2 ShotAdjustmentJumpingDownRight = new Vector2(.06f, .40f);
        private static readonly Vector2 ShotAdjustmentJumpingDown = new Vector2(.16f, -.20f);

        #endregion

        /// <summary>
        /// Handles movement input, both on the ground and in the air.
        /// </summary>
        private void HandleMovement(GameTime gameTime) {
            bool isDucking = false;

            Direction? movementDirection = PlayerControl.Control.GetMovementDirection();
            if ( _timeUntilRegainControl <= 0 ) {
                if ( IsStanding ) {
                    if ( movementDirection != null ) {
                        float minLateralSpeed = PlayerControl.Control.IsRunButtonDown() ? Constants[PlayerInitRunSpeedMs] : Constants[PlayerInitSpeedMs];
                        switch ( movementDirection.Value ) {
                            case Direction.Left:
                            case Direction.DownLeft:
                            case Direction.UpLeft:
                                _facingDirection = Direction.Left;
                                if ( _body.LinearVelocity.X > -minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(-minLateralSpeed,
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxGroundSpeedMs] ) {
                                    if ( PlayerControl.Control.IsRunButtonDown() ) {
                                        _body.LinearVelocity -= new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else {
                                    _body.LinearVelocity = new Vector2(-Constants[PlayerMaxGroundSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Right:
                            case Direction.UpRight:
                            case Direction.DownRight:
                                _facingDirection = Direction.Right;
                                if ( _body.LinearVelocity.X < minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(minLateralSpeed,
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxGroundSpeedMs] ) {
                                    if ( PlayerControl.Control.IsRunButtonDown() ) {
                                        _body.LinearVelocity += new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else {
                                    _body.LinearVelocity = new Vector2(Constants[PlayerMaxGroundSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Down:
                                isDucking = true;
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                AdjustFacingDirectionForAim();
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
                        AdjustFacingDirectionForAim();
                    }
                } else {
                    // in the air
                    if ( movementDirection != null ) {
                        float minLateralSpeed = Constants[PlayerInitSpeedMs];
                        float fastJumpSpeed = Constants[PlayerFastJumpSpeed];
                        switch ( movementDirection.Value ) {                                
                            case Direction.Left:
                            case Direction.UpLeft:
                            case Direction.DownLeft:
                                // move left instantaneously unless you are moving too fast to the right
                                if ( _body.LinearVelocity.X < fastJumpSpeed && _body.LinearVelocity.X > -minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(-minLateralSpeed, _body.LinearVelocity.Y);
                                // you can brake a fast jump, but you can't turn it around until it's slow enough
                                } else if ( _body.LinearVelocity.X >= fastJumpSpeed ) {
                                    _body.LinearVelocity -= new Vector2(
                                        GetVelocityDelta(Constants[PlayerAirBrakeMss], gameTime), 0);
                                }

                                if ( _body.LinearVelocity.X <= 0 ) {
                                    _facingDirection = Direction.Left;
                                }

                                break;
                            case Direction.Right:
                            case Direction.UpRight:
                            case Direction.DownRight:
                                // move right instantaneously unless you are moving too fast to the left
                                if ( _body.LinearVelocity.X > -fastJumpSpeed && _body.LinearVelocity.X < minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(minLateralSpeed, _body.LinearVelocity.Y);
                                    // you can brake a fast jump, but you can't turn it around until it's slow enough
                                } else if ( _body.LinearVelocity.X <= -fastJumpSpeed ) {
                                    _body.LinearVelocity += new Vector2(
                                        GetVelocityDelta(Constants[PlayerAirBrakeMss], gameTime), 0);
                                }

                                if ( _body.LinearVelocity.X >= 0 ) {
                                    _facingDirection = Direction.Right;
                                }
                                break;
                        }
                    }
                }
            }

            IsDucking = isDucking;
        }

        private void AdjustFacingDirectionForAim() {
             // If we're standing still, the right stick can change the facing direction
            Direction? aimDirection = InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Right);
            if ( aimDirection != null && aimDirection != _facingDirection ) {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.UpLeft:
                    case Direction.DownLeft:
                        _facingDirection = Direction.Left;
                        break;
                    case Direction.Right:
                    case Direction.UpRight:
                    case Direction.DownRight:
                        _facingDirection = Direction.Right;
                        break;
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

            if ( PlayerControl.Control.IsNewJump() ) {
                if ( IsStanding ) {
                    _jumpInitiated = true;
                    _airBoostTime = 0;
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                }
            } else if ( PlayerControl.Control.IsJumpButtonDown() ) {
                if ( _standingMonitor.IsTouchingCeiling ) {
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
            if ( PlayerControl.Control.IsNewScooter() && IsDucking && !IsScooting ) {
                _scooterInitiated = true;
                IsScooting = true;
            } else if ( !PlayerControl.Control.IsScooterButtonDown() ) {
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
        
        /// <summary>
        /// Resizes the body while keeping the lower edge in the same position and the X position constant.
        /// </summary>
        private void ResizeBody(float width, float height) {
            ResizeBody(width, height, Vector2.Zero);
        }

        /// <summary>
        /// Resizes the body while keeping the lower edge in the same position and the X position constant.
        /// </summary>
        private void ResizeBody(float width, float height, Vector2 positionalCorrection) {
            _standingMonitor.IgnoreStandingUpdatesNextNumFrames = 2;

            float halfHeight = height / 2;
            var newPosition = GetNewBodyPosition(halfHeight, positionalCorrection);
            
            _body.Position = newPosition;

            PolygonShape shape = (PolygonShape) _body.FixtureList.First().Shape;
            shape.SetAsBox(width / 2, halfHeight);
            Height = height;
            Width = width;
        }

        /// <summary>
        /// Returns the position of the body if the half-height is as indicated, 
        /// holding the Y position of the bottom edge constant.
        /// </summary>
        private Vector2 GetNewBodyPosition(float halfHeight, Vector2 positionCorrection) {
            Vector2 position = _body.Position;
            float oldYPos = position.Y + Height / 2;
            float newYPos = position.Y + halfHeight;
            Vector2 newPosition = new Vector2(position.X, position.Y + (oldYPos - newYPos)) + positionCorrection;
            return newPosition;
        }

        private void HandleSonar(GameTime gameTime) {
            if ( PlayerControl.Control.IsNewSonar() ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                EnceladusGame.Instance.Register(new Sonar(_world, position, shotDirection));
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
        public const int CrouchAimStraightFrame = 5;
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
        public const int ScootFrame = 9;
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

        public Texture2D[] RunAimStraightAnimation {
            get { return _runAimStraightAnimation; }
        }

        private readonly Texture2D[] _runAimDiagonalDownAnimation = new Texture2D[NumRunAimFrames];
        private readonly Texture2D[] _runAimDownAnimation = new Texture2D[NumRunAimFrames];

        private readonly Texture2D[] _jumpAnimation = new Texture2D[NumJumpFrames];
        private readonly Texture2D[] _crouchAnimation = new Texture2D[NumCrouchFrames];

        public Texture2D[] CrouchAnimation {
            get { return _crouchAnimation; }
        }

        private readonly Texture2D[] _scooterAnimation = new Texture2D[NumScooterFrames];

        public Texture2D[] ScooterAnimation {
            get { return _scooterAnimation; }
        }

        /*
         * Unarmed animations
         * 
         * TODO: change to private access
         */
        public const int NumUnarmedJumpFrames = 12;
        public const int NumUnarmedWalkFrames = 27;
        public const int NumUnarmedJogFrames = 17;
        public readonly Texture2D[] _unarmedJumpAnimation = new Texture2D[NumUnarmedJumpFrames];
        public readonly Texture2D[] _unarmedWalkAnimation = new Texture2D[NumUnarmedWalkFrames];
        public readonly Texture2D[] _unarmedJogAnimation = new Texture2D[NumUnarmedJogFrames];
        public Texture2D _unarmedStandFrame;

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

            for ( int i = 0; i < NumUnarmedJogFrames; i++ ) {
                _unarmedJogAnimation[i] = content.Load<Texture2D>(String.Format("Character/JogRight/jog_right{0:0000}", i));
            }
            for ( int i = 0; i < NumUnarmedJumpFrames; i++ ) {
                _unarmedJumpAnimation[i] = content.Load<Texture2D>(String.Format("Character/Jump/Jump{0:0000}", i));
            }
            for ( int i = 0; i < NumUnarmedWalkFrames; i++ ) {
                _unarmedWalkAnimation[i] = content.Load<Texture2D>(String.Format("Character/Walk/Walk{0:0000}", i));
            }

            _unarmedStandFrame = _unarmedJumpAnimation[NumUnarmedJumpFrames - 1];
        }

        private Vector2 _imageDrawOffset = Vector2.Zero;

        /// <summary>
        /// Updates the current image for the next frame
        /// </summary>
        private void UpdateImage(GameTime gameTime) {

            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;
            _imageDrawOffset = Vector2.Zero;

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
                        if ( _abortScooting ) {
                            EndScooter();
                        }
                    }
                }
            } else if ( _endScooterInitiated && !IsScooting ) {
                _currentAnimation = Animation.StandUp;
                if ( _currentAnimation != _prevAnimation && _animationFrame > ScootFrame ) {
                    _animationFrame = ScootFrame;
                }
                if ( _timeSinceLastAnimationUpdate > 0 || _currentAnimation != _prevAnimation ) {
                    Image = _scooterAnimation[_animationFrame--];
                    if ( !_abortScooting ) {
                        if ( _facingDirection == Direction.Right ) {
                            _imageDrawOffset = new Vector2(ScooterNudge, 0);
                        } else if ( _facingDirection == Direction.Left ) {
                            _imageDrawOffset = new Vector2(-ScooterNudge, 0);
                        }
                    }
                    if ( _animationFrame < 0 ) {
                        _animationFrame = 0;
                        _endScooterInitiated = false;
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
            return Math.Abs(_body.LinearVelocity.X) <= Constants[PlayerMaxGroundSpeedMs] * .6f;
        }

        private bool IsWalkingSpeed() {
            return Math.Abs(_body.LinearVelocity.X) <= (Constants[PlayerInitSpeedMs] + Constants[PlayerInitRunSpeedMs]) / 2f;
        }

        #endregion

        public void HitBy(AbstractWalkingEnemy enemy) {
            Health -= 10;
            if ( Health <= 0 ) {
                Health = 0;
                EnceladusGame.Instance.Die();
                return;
            }

            Vector2 diff = Position - enemy.Position;
            _body.LinearVelocity = new Vector2(0);
            _body.ApplyLinearImpulse(diff * Constants[PlayerKnockbackAmt] * _body.Mass);
            _timeUntilRegainControl = (long)(Constants[PlayerKnockbackTime] * 1000);

            _flashAnimation.SetFlashTime(150);

            // Make sure we're in the air or on the ground as necessary
            _standingMonitor.IgnoreStandingUpdatesNextNumFrames = 0;
            UpdateStanding();
        }

        public void Dispose() {
            if ( _body != null ) {
                _body.Dispose();
            }
        }

        public void Pickup(HealthPickup healthPickup) {
            Health += 10;
        }

        private bool _terrainChanged;
        protected World _world;
        protected Body _body;
        protected readonly FlashAnimation _flashAnimation = new FlashAnimation();
        protected readonly StandingMonitor _standingMonitor = new StandingMonitor();

        /// <summary>
        /// Unfortunately, we can't rely on Box2d to properly notify us when we hit or 
        /// leave the ground or ceiling when bodies are being created or destroyed.  
        /// This method allows knowledgable callers to suggest updating the standing info 
        /// on the next update cycle.
        /// </summary>
        public void NotifyTerrainChange() {
            _terrainChanged = true;
        }

        public void Save(SaveState save) {
            Equipment.Save(save);
        }

        public void LoadFromSave(SaveState save) {
            Equipment.LoadFromSave(save);
        }

        /// <summary>
        /// Destroys the player and disposes of his simulation body
        /// </summary>
        public void Destroy() {
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, Image, Color, null, _body.Position, 8, 30f));
            Dispose();
        }

        /// <summary>
        /// Updates the standing and ceiling status using the body's current contacts.
        /// </summary>
        protected void UpdateStanding() {
            _standingMonitor.UpdateStanding(_body, _world, GetStandingLocation());
            IsStanding = _standingMonitor.IsStanding;
        }

        protected void UpdateFlash(GameTime gameTime) {
            _flashAnimation.UpdateFlash(gameTime);
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

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
                             _facingDirection == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
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
            Run,
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
        };

        private Animation _currentAnimation = Animation.AimStraight;
        private Animation _prevAnimation = Animation.AimStraight;
        private bool _isDucking = false;

        private const int NumAimWalkStraightFrames = 30;
        private const int NumAimWalkFrames = 16;
        private const int NumJogFrames = 13;
        private const int NumRunFrames = 22;
        
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


        private readonly Texture2D[] _standAimAnimation = new Texture2D[NumStandAimFrames];

        private readonly Texture2D[] _walkAimUpAnimation = new Texture2D[NumAimWalkFrames];
        private readonly Texture2D[] _walkAimDiagonalUpAnimation = new Texture2D[NumAimWalkFrames];
        private readonly Texture2D[] _walkAimStraightAnimation = new Texture2D[NumAimWalkStraightFrames];
        private readonly Texture2D[] _walkAimDiagonalDownAnimation = new Texture2D[NumAimWalkFrames];
        private readonly Texture2D[] _walkAimDownAnimation = new Texture2D[NumAimWalkFrames];

        private readonly Texture2D[] _jogAimUpAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDiagonalUpAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimStraightAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDiagonalDownAnimation = new Texture2D[NumJogFrames];
        private readonly Texture2D[] _jogAimDownAnimation = new Texture2D[NumJogFrames];

        private readonly Texture2D[] _runAnimation = new Texture2D[NumRunFrames];
        private readonly Texture2D[] _jumpAnimation = new Texture2D[NumJumpFrames];
        private readonly Texture2D[] _crouchAnimation = new Texture2D[NumCrouchFrames];

        private int _animationFrame;
        private const int JumpDelayMs = 50;
        private long _timeSinceLastAnimationUpdate;
        private long _timeSinceJump = -1;
        private bool _jumpInitiated;

        private void LoadAnimations(ContentManager content) {
            for ( int i = 0; i < NumStandAimFrames; i++ ) {
                _standAimAnimation[i] = content.Load<Texture2D>(String.Format("Character/StandAim/StandAim{0:0000}", i));
            }

            for ( int i = 0; i < NumAimWalkStraightFrames; i++ ) {
                _walkAimStraightAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunWalk/GunWalkStraight/GunWalkStraight{0:0000}", i));
            }
            for ( int i = 0; i < NumAimWalkFrames; i++ ) {
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

            
            for ( int i = 0; i < NumRunFrames; i++ ) {
                _runAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunRun/GunRun{0:0000}", i));
            }

            for ( int i = 0; i < NumJumpFrames; i++ ) {
                _jumpAnimation[i] = content.Load<Texture2D>(String.Format("Character/GunJump/GunJump{0:0000}", i));
            }
            for ( int i = 0; i < NumCrouchFrames; i++ ) {
                _crouchAnimation[i] = content.Load<Texture2D>(String.Format("Character/Crouch/Crouch{0:0000}", i));
            }

            Image = _standAimAnimation[AimRightFrame];
        }

        /// <summary>
        /// Updates the current image for the next frame
        /// </summary>
        private void UpdateImage(GameTime gameTime) {

            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;

            var aimDirection = GetAimDirection();

            if ( IsStanding ) {
                if ( _jumpInitiated ) {
                    _currentAnimation = Animation.JumpInit;
                    if ( _currentAnimation != _prevAnimation ) {
                        _animationFrame = 0;
                    }
                    Image = _jumpAnimation[_animationFrame++];
                } else if ( IsStandingStill() ) {

                    if ( !_isDucking ) {
                        switch ( aimDirection ) {
                            case Direction.Left:
                            case Direction.Right:
                                _currentAnimation = Animation.AimStraight;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < AimRightFrame
                                         || _animationFrame > AimRightFrame + 1 )
                                        _animationFrame = AimRightFrame;
                                    Image = _standAimAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.Up:
                                _currentAnimation = Animation.AimUp;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < AimUpFrame
                                         || _animationFrame > AimUpFrame + 1 )
                                        _animationFrame = AimUpFrame;
                                    Image = _standAimAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.Down:
                                _currentAnimation = Animation.AimDown;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < AimDownFrame
                                         || _animationFrame > AimDownFrame + 1 )
                                        _animationFrame = AimDownFrame;
                                    Image = _standAimAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.UpLeft:
                            case Direction.UpRight:
                                _currentAnimation = Animation.AimDiagonalUp;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < AimUpRightFrame
                                         || _animationFrame > AimUpRightFrame + 1 )
                                        _animationFrame = AimUpRightFrame;
                                    Image = _standAimAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.DownLeft:
                            case Direction.DownRight:
                                _currentAnimation = Animation.AimDiagonalDown;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < AimDownRightFrame
                                         || _animationFrame > AimDownRightFrame + 1 )
                                        _animationFrame = AimDownRightFrame;
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
                                         || _animationFrame > CrouchAimStraightFrame + 1 )
                                        _animationFrame = CrouchAimStraightFrame;
                                    Image = _crouchAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.Up:
                                _currentAnimation = Animation.CrouchAimUp;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < CrouchAimUpFrame
                                         || _animationFrame > CrouchAimUpFrame + 1 )
                                        _animationFrame = CrouchAimUpFrame;
                                    Image = _crouchAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.Down:
                                _currentAnimation = Animation.CrouchAimDown;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < CrouchAimDownFrame
                                         || _animationFrame > CrouchAimDownFrame + 1 )
                                        _animationFrame = CrouchAimDownFrame;
                                    Image = _crouchAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.UpLeft:
                            case Direction.UpRight:
                                _currentAnimation = Animation.CrouchAimDiagonalUp;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < CrouchAimUpRightFrame
                                         || _animationFrame > CrouchAimUpRightFrame + 1 )
                                        _animationFrame = CrouchAimUpRightFrame;
                                    Image = _crouchAnimation[_animationFrame++];
                                }
                                break;
                            case Direction.DownLeft:
                            case Direction.DownRight:
                                _currentAnimation = Animation.CrouchAimDiagonalDown;
                                if ( _timeSinceLastAnimationUpdate > 500f
                                     || _prevAnimation != _currentAnimation ) {
                                    if ( _animationFrame < CrouchAimDownRightFrame
                                         || _animationFrame > CrouchAimDownRightFrame + 1 )
                                        _animationFrame = CrouchAimDownRightFrame;
                                    Image = _crouchAnimation[_animationFrame++];
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                } else if ( IsWalkingSpeed() ) {
                    float walkSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerWalkSpeedMultiplier]);

                    switch ( aimDirection ) {
                        case Direction.Left:
                        case Direction.Right:
                            _currentAnimation = Animation.WalkAimStraight;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumAimWalkStraightFrames / walkSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumAimWalkStraightFrames;
                                Image = _walkAimStraightAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.Up:
                            _currentAnimation = Animation.WalkAimUp;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumAimWalkStraightFrames / walkSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumAimWalkFrames;
                                Image = _walkAimUpAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.Down:
                            _currentAnimation = Animation.WalkAimDown;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumAimWalkStraightFrames / walkSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumAimWalkFrames;
                                Image = _walkAimDownAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.UpLeft:
                        case Direction.UpRight:
                            _currentAnimation = Animation.WalkAimDiagonalUp;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumAimWalkStraightFrames / walkSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumAimWalkFrames;
                                Image = _walkAimDiagonalUpAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.DownLeft:
                        case Direction.DownRight:
                            _currentAnimation = Animation.WalkAimDiagonalDown;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumAimWalkStraightFrames / walkSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumAimWalkFrames;
                                Image = _walkAimDiagonalDownAnimation[_animationFrame++];
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } else if ( IsJoggingSpeed() ) {

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
                            _currentAnimation = Animation.WalkAimUp;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumJogFrames;
                                Image = _jogAimUpAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.Down:
                            _currentAnimation = Animation.WalkAimDown;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumJogFrames;
                                Image = _jogAimDownAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.UpLeft:
                        case Direction.UpRight:
                            _currentAnimation = Animation.WalkAimDiagonalUp;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumJogFrames;
                                Image = _jogAimDiagonalUpAnimation[_animationFrame++];
                            }
                            break;
                        case Direction.DownLeft:
                        case Direction.DownRight:
                            _currentAnimation = Animation.WalkAimDiagonalDown;
                            if ( _timeSinceLastAnimationUpdate > 1000f / NumJogFrames / jogSpeed
                                 || _prevAnimation != _currentAnimation ) {
                                _animationFrame %= NumJogFrames;
                                Image = _jogAimDiagonalDownAnimation[_animationFrame++];
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } else {
                    _currentAnimation = Animation.Run;
                    float runSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);
                    if ( _timeSinceLastAnimationUpdate > 1000f / NumRunFrames / runSpeed
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame %= _runAnimation.Length;
                        Image = _runAnimation[_animationFrame++];
                    }
                }
            } else {
                if ( _jumpInitiated && _animationFrame < JumpAimUpFrame ) {
                    Image = _jumpAnimation[_animationFrame];
                    _animationFrame++;
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

            _prevAnimation = _currentAnimation;
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

        private Direction GetAimDirection() {
            Direction? aimDirection =
                InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Right);
            if ( aimDirection == null ) {
                aimDirection = _facingDirection;
            }

            // Left stick always overrides right stick, unless just running or ducking
            if ( !_isDucking && (!IsStanding || _body.LinearVelocity.X == 0) ) {
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
                        case Direction.UpRight:
                        case Direction.DownRight:
                            aimDirection = _facingDirection;
                            break;
                    }
                    break;
                case Direction.Right:
                    switch ( aimDirection.Value ) {
                        case Direction.Left:
                        case Direction.UpLeft:
                        case Direction.DownLeft:
                            aimDirection = _facingDirection;
                            break;
                    }
                    break;
            }
            return aimDirection.Value;
        }

        #endregion

        private readonly List<IGameEntity> _shots = new List<IGameEntity>();

        /// <summary>
        /// Handles firing any weapons
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( InputHelper.Instance.IsNewButtonPress(Buttons.X) 
                || InputHelper.Instance.IsNewButtonPress(Buttons.RightTrigger) ) {
                Direction shotDirection;
                var position = GetShotParameters(out shotDirection);
                _shots.Add(new Shot(position, _world, shotDirection));
            }
        }

        /// <summary>
        /// Returns the original location and direction to place a new shot in the game world.
        /// </summary>
        private Vector2 GetShotParameters(out Direction shotDirection) {
            shotDirection = GetAimDirection();

            Vector2 position = _body.Position;
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
                case Direction.UpLeft:
                    position += new Vector2(-CharacterWidth / 2 + .1f, -CharacterHeight / 2 + .1f);
                    break;
                case Direction.UpRight:
                    position += new Vector2(CharacterWidth / 2 - .1f, -CharacterHeight / 2 + .1f);
                    break;
                case Direction.DownLeft:
                    position += new Vector2(-CharacterWidth / 2 + .1f, -CharacterHeight / 4 + -.1f);
                    break;
                case Direction.DownRight:
                    position += new Vector2(CharacterWidth / 2 - .1f, -CharacterHeight / 4 + -.1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("shotDirection");
            }

            if ( _isDucking && shotDirection != Direction.Down ) {
                position += new Vector2(0, CharacterHeight / 3f);
            }
            return position;
        }

        /// <summary>
        /// Handles movement input, both on the ground and in the air.
        /// </summary>
        private void HandleMovement(Vector2 movementInput, GameTime gameTime) {
            _isDucking = false;
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
                                _isDucking = true;
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
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
    };
}

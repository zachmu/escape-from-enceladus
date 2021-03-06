﻿using System;
using System.Collections.Generic;
using System.Linq;
using Enceladus.Entity.InteractiveObject;
using Enceladus.Event;
using Enceladus.Map;
using Enceladus.Overlay;
using Enceladus.Util;
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
            Constants.Register(new Constant(PlayerJogSpeedMultiplier, .46f, Keys.B, .01f));
            Constants.Register(new Constant(PlayerWalkSpeedMultiplier, .4f, Keys.N));
            Constants.Register(new Constant(PlayerWheelSpinSpeedMultiplier, .57f, null, .01f));
            Constants.Register(new Constant(PlayerScooterOffset, 0f, null));
            Constants.Register(new Constant(Projectile.ProjectileOffsetX, 0f, Keys.X, 1f));
            Constants.Register(new Constant(Projectile.ProjectileOffsetY, 0f, Keys.Y, 1f));
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

        private CollectibleItem _activeItem;

        /// <summary>
        /// The player's beam weapon when active, or null if none is active.
        /// </summary>
        private Beam _beam;

        /// <summary>
        /// The player's holocube tool when active, or null if none is active.
        /// </summary>
        private Holocube _cube;

        /// <summary>
        /// The player's springboard tool when active, or null if none is active.
        /// </summary>
        private Springboard _springboard;

        /// <summary>
        /// The player's hoverboots when firing, or null otherwise
        /// </summary>
        private HoverBoots _hover;
        private bool IsHoverActive { get { return _hover != null && !_hover.Disposed; } }
        private bool _canHover = false;

        private Color _color = Color.SteelBlue;

        public Color Color {
            get { return _color; }
        }

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

        public int RapidFireSetting {
            get { return _rapidFireSetting; }
            set {
                _rapidFireSetting = value;
                StopChargeSoundEffect();
            }
        }

        private double _shotChargeTime = 0;
        private double _cumulativeShotChargeTime = 0;
        private const double MaxChargeTimeMs = 4000;
        private const double ChargeWaitTimeMs = 250;

        /// <summary>
        /// How long a shot must charge at each rapid fire setting before discharging
        /// </summary>
        private static readonly double[] ShotTimeThresholdsMs = new double[] {
            double.MaxValue, 1000, 500, 150, 50
        };
        private static readonly float[] ChargeShotDamage = new float[] {
            0, 7, 3, .9f, .25f,
        };
        private static readonly float[] ChargeShotScale = new float[] {
            0, 2, 1.5f, .9f, .5f,
        };

        public Player(Vector2 position, World world) {
            _instance = this;

            HealthCapacity = 100;
            Health = 100;

            Equipment = new Equipment();
            // TODO: Fix
            _activeItem = CollectibleItem.Beam;

            _world = world;

            _world.ContactManager.OnBroadphaseCollision += OnBroadphaseDashCollision;
        }

        private void OnBroadphaseDashCollision(ref FixtureProxy a, ref FixtureProxy b) {
            if ( _isDashing
                 && (a.Fixture.GetUserData().IsPlayer || b.Fixture.GetUserData().IsPlayer)
                 && (a.Fixture.GetUserData().IsDestructibleRegion || b.Fixture.GetUserData().IsDestructibleRegion) ) {
                Fixture f = a.Fixture.GetUserData().IsDestructibleRegion ? a.Fixture : b.Fixture;
                if ( f.GetUserData().Destruction.DestroyedBy(DestructionFlags.DashDestructionFlag) ) {
                    Tile tile = TileLevel.CurrentLevel.GetTile(f.Body.Position);
                    if ( tile != null ) {
                        TileLevel.CurrentLevel.DestroyTile(tile);
                    }
                }
            }
        }

        // Creates the simulated body at the specified position
        public void CreateBody(Vector2 position) {
            if ( !Disposed ) {
                Dispose();
            }

            _body = BodyFactory.CreateRectangle(_world, CharacterStandingWidth, CharacterStandingHeight, 10f);
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
            _body.IgnoreGravity = IsHoverActive;
            _body.CollidesWith = EnceladusGame.TerrainCategory | EnceladusGame.EnemyCategory |
                                 EnceladusGame.PlayerSensorCategory;
            _body.CollisionCategories = EnceladusGame.PlayerCategory;
            _body.UserData = UserData.NewPlayer();

            _body.OnCollision += DashCollisionHandler;
        }

        /// <summary>
        /// Monitor block breaking with dashes in two separate ways. 
        /// In the broad phase, just look for an upcoming collision with 
        /// the player and a destructible region. At full speed, this can't 
        /// break blocks fast enough, so we also need to monitor dashing collisions
        /// with the function below.
        /// </summary>
        private bool DashCollisionHandler(Fixture a, Fixture b, Contact contact) {
            if ( _isDashing ) {
                if ( b.GetUserData().IsTerrain && contact.GetPlayerNormal(_body).Y == 0 ) {
                    FixedArray2<Vector2> points;
                    Vector2 normal;
                    contact.GetWorldManifold(out normal, out points);
                    var tile = TileLevel.CurrentLevel.GetCollidedTile(points[0], normal);
                    if ( tile != null && TileLevel.CurrentLevel.IsTileDestroyedBy(tile, DestructionFlags.DashDestructionFlag) ) {
                        TileLevel.CurrentLevel.DestroyTile(tile);
                        return false;
                    }
                }
            }
            return true;
        }

        public float Health { get; private set; }
        public float HealthCapacity { get; private set; }

        public Equipment Equipment { get; private set; }

        private Texture2D _image;

        private Texture2D Image {
            get { return _image; }
        }

        private void SetImage(Texture2D image, double timeSinceLastUpdate) {
            if ( _image != image ) {
                _timeSinceLastAnimationUpdate = timeSinceLastUpdate;
            }
            _image = image;
        }

        public void LoadContent(ContentManager content) {
            LoadAnimations(content);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // Draw origin is character's feet
                Vector2 position = _body.Position;
                position.Y += Height / 2;
                position += _imageDrawOffset;

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : _color;
                float alpha = _invulnerabilityTimer == null ? 1.0f : .65f;
                spriteBatch.Draw(Image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width,
                                               Image.Height),
                                 null, color * alpha, 0f, new Vector2(Image.Width / 2, Image.Height - 1),
                                 _facingDirection == Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);

                // drawing the frame number on top of the character
                if ( Constants[Sonar.WeaponDrawDebug] >= 1 ) {
                    spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, "" + _animationFrame,
                                           displayPosition - new Vector2(0, 100), Color.White);
                }   
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
                    SoundEffectManager.Instance.PlaySoundEffect("land");
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
                                      new Vector2(-ScooterNudge - CharacterDuckingWidth / 2f,
                                                  CharacterScootingHeight / 2));
                        startRays.Add(_body.Position +
                                      new Vector2(-ScooterNudge + CharacterDuckingWidth / 2f,
                                                  CharacterScootingHeight / 2));
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
            UpdateBookkeepingCounters(gameTime);

            HandleMovement(gameTime);
            HandleDash(gameTime);
            HandleJump(gameTime);
            HandleScooter(gameTime);
            UpdateImage(gameTime);

            // Some moves rely on proper animation info, 
            // so we do them after the image update.
            HandleShot(gameTime);
            HandleBomb(gameTime);
            HandleCube(gameTime);
            HandleSpringboard(gameTime);

            if ( !IsScooting ) {
                HandleBeam(gameTime);
                HandleSonar(gameTime);
            }

            UpdateFlash(gameTime);
            UpdateInvulnerable(gameTime);
            UpdateStanding();

            if ( _isDashing ) {
                EnceladusGame.Instance.Register(new PlayerEcho(_image, GetStandingLocation(),
                                                               _facingDirection == Direction.Left));
            }
        }

        private void HandleCube(GameTime gameTime) {
            if ( _activeItem != CollectibleItem.Holocube || IsScooting || _endScooterInitiated ) {
                if ( _cube != null ) {
                    _cube.Dispose();
                    _cube = null;
                }
                return;
            }

            Direction direction;
            Vector2 position = GetShotPlacement(out direction);
            if ( _cube == null ) {
                EnceladusGame.Instance.Register(_cube = new Holocube(_world, position, direction));
            } else {
                _cube.UpdateProjection(position, direction);
            }
            if ( PlayerControl.Control.IsNewSecondaryFire() ) {
                _cube.Fire();
            }
        }

        private void HandleSpringboard(GameTime gameTime) {
            if ( _activeItem != CollectibleItem.Springboard || IsScooting || _endScooterInitiated ) {
                if ( _springboard != null ) {
                    _springboard.Dispose();
                    _springboard = null;
                }
                return;
            }

            Direction direction;
            Vector2 position = GetShotPlacement(out direction);
            if ( _springboard == null ) {
                EnceladusGame.Instance.Register(_springboard = new Springboard(_world, position, direction));
            } else {
                _springboard.UpdateProjection(position, direction);
            }
            if ( PlayerControl.Control.IsNewSecondaryFire() ) {
                _springboard.Fire();
            }
        }


        private void HandleBeam(GameTime gameTime) {
            if ( _activeItem != CollectibleItem.Beam ) {
                if ( _beam != null ) {
                    DisposeBeam();
                }
                return;
            }

            if ( _beam == null ) {
                if ( PlayerControl.Control.IsNewSecondaryFire() ) {
                    Direction direction;
                    Vector2 position = GetShotPlacement(out direction);
                    EnceladusGame.Instance.Register(_beam = new Beam(_world, position, direction));
                    SoundEffectManager.Instance.PlayOngoingEffect("buzz");
                    SoundEffectManager.Instance.PlayOngoingEffect("buzzLong");
                }
            } else if ( PlayerControl.Control.IsSecondaryFireButtonDown() ) {
                Direction direction;
                Vector2 position = GetShotPlacement(out direction);
                _beam.Update(_world, position, direction);
            } else {
                DisposeBeam();
            }
        }

        private void DisposeBeam() {
            _beam.Dispose();
            _beam = null;
            SoundEffectManager.Instance.StopOngoingEffect("buzz");
            SoundEffectManager.Instance.StopOngoingEffect("buzzLong");
        }

        private void UpdateInvulnerable(GameTime gameTime) {
            if ( _invulnerabilityTimer != null ) {
                _invulnerabilityTimer.Update(gameTime);
                if ( _invulnerabilityTimer.IsTimeUp() ) {
                    RemoveInvulnerability();
                }
            }
        }

        /// <summary>
        /// Updates the several book-keeping figures used in character control
        /// </summary>
        private void UpdateBookkeepingCounters(GameTime gameTime) {
            if ( _timeUntilRegainControl > 0 ) {
                _timeUntilRegainControl -= gameTime.ElapsedGameTime.Milliseconds;
            }

            _standingMonitor.UpdateCounters();
        }

        /// <summary>
        /// Handles firing the primary weapon
        /// </summary>
        private void HandleShot(GameTime gameTime) {
            if ( IsScooting )
                return;

            if ( PlayerControl.Control.IsNewShot() ) {
                Direction shotDirection;
                var position = GetShotPlacement(out shotDirection);
                EnceladusGame.Instance.Register(new Shot(position, _world, shotDirection));
            } else if ( PlayerControl.Control.IsShotButtonDown() ) {
                _shotChargeTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                _cumulativeShotChargeTime += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( RapidFireSetting < 3 && _cumulativeShotChargeTime >= ChargeWaitTimeMs ) {
                    switch ( RapidFireSetting ) {
                        case 0:
                            SoundEffectManager.Instance.PlayOngoingEffect("chargeShot");
                            break;
                        case 1:
                            SoundEffectManager.Instance.PlayOngoingEffect("chargeShotHigher");
                            break;
                        case 2:
                            SoundEffectManager.Instance.PlayOngoingEffect("chargeShotHighest");
                            break;
                    }
                }
                if ( _shotChargeTime > ShotTimeThresholdsMs[RapidFireSetting] ) {
                    _shotChargeTime %= ShotTimeThresholdsMs[RapidFireSetting];
                    Direction shotDirection;
                    var position = GetShotPlacement(out shotDirection);
                    EnceladusGame.Instance.Register(new Shot(position, _world, shotDirection,
                                                             ChargeShotScale[RapidFireSetting],
                                                             ChargeShotDamage[RapidFireSetting]));
                    StopChargeSoundEffect();
                }
            } else {
                if ( _shotChargeTime >= ChargeWaitTimeMs && RapidFireSetting == 0 ) {
                    Direction shotDirection;
                    var position = GetShotPlacement(out shotDirection);
                    if ( _shotChargeTime > MaxChargeTimeMs ) {
                        _shotChargeTime = MaxChargeTimeMs;
                    }
                    float scale = (float) (_shotChargeTime / MaxChargeTimeMs);
                    EnceladusGame.Instance.Register(new Shot(position, _world, shotDirection, 1 + 3 * scale,
                                                             1 + 9 * scale));
                }
                _shotChargeTime = 0;
                _cumulativeShotChargeTime = 0;
                StopChargeSoundEffect();
            }
        }

        private static void StopChargeSoundEffect() {
            SoundEffectManager.Instance.StopOngoingEffect("chargeShot");
            SoundEffectManager.Instance.StopOngoingEffect("chargeShotHigher");
            SoundEffectManager.Instance.StopOngoingEffect("chargeShotHighest");
        }

        /// <summary>
        /// Handles laying a bomb while scooting
        /// </summary>
        private void HandleBomb(GameTime gameTime) {
            if ( !IsScooting )
                return;

            if ( PlayerControl.Control.IsNewShot() ) {
                Vector2 pos = Vector2.Zero;
                if ( _facingDirection == Direction.Right ) {
                    pos = _body.Position +
                          new Vector2(CharacterScootingWidth / 2, CharacterScootingHeight / 2 - Bomb.Height / 2);
                } else {
                    pos = _body.Position +
                          new Vector2(-CharacterScootingWidth / 2, CharacterScootingHeight / 2 - Bomb.Height / 2);
                }
                EnceladusGame.Instance.Register(new Bomb(pos, _world, _facingDirection));
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
        private Vector2 GetShotPlacement(out Direction shotDirection) {
            shotDirection = GetAimDirection();

            Vector2 position = _body.Position;
            switch ( shotDirection ) {
                case Direction.Right:
                    position += new Vector2(CharacterStandingWidth/2f - .1f, -CharacterStandingHeight/4.5f);
                    break;
                case Direction.Left:
                    position += new Vector2(-(CharacterStandingWidth/2f - .1f), -CharacterStandingHeight/4.5f);
                    break;
                case Direction.Down:
                    position += new Vector2(0, CharacterStandingHeight/2 - .1f);
                    break;
                case Direction.Up:
                    position += new Vector2(0, -CharacterStandingHeight/2 + .1f);
                    break;
                case Direction.UpLeft:
                    position += new Vector2(-CharacterStandingWidth/2 + .1f, -CharacterStandingHeight/2 + .1f);
                    break;
                case Direction.UpRight:
                    position += new Vector2(CharacterStandingWidth/2 - .1f, -CharacterStandingHeight/2 + .1f);
                    break;
                case Direction.DownLeft:
                    position += new Vector2(-CharacterStandingWidth/2 + .1f, -CharacterStandingHeight/4 + -.1f);
                    break;
                case Direction.DownRight:
                    position += new Vector2(CharacterStandingWidth/2 - .1f, -CharacterStandingHeight/4 + -.1f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("shotDirection");
            }

            // tuning params
            //            position += new Vector2(ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetX]), 
            //                ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetY]));

            if ( IsDucking && shotDirection != Direction.Down ) {
                position += new Vector2(0, CharacterStandingHeight/3f);
            }

            // Fine tuning the shot placement.
            // Numbers determined experimentally and with pixel counting.
            // It is a mess.
            Vector2 tuning = Vector2.Zero;
            switch ( shotDirection ) {
                case Direction.Left:
                case Direction.Right:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingRight + new Vector2(0, DuckingAdjustments[_animationFrame]);
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingRight;
                        if ( IsStandingStill() ) {
                            tuning += new Vector2(0, StandingAdjustments[_animationFrame]);
                        } else if ( IsWalkingSpeed() ) {
                            tuning += new Vector2(0, WalkingStraightAdjustments[_animationFrame]);
                        } else if ( IsJoggingSpeed() ) {
                            tuning += new Vector2(0, JoggingStraightAdjustments[_animationFrame]);
                        } else {
                            tuning += new Vector2(0, RunningStraightAdjustments[_animationFrame]);
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingRight + new Vector2(0, JumpingAdjustments[_animationFrame]);
                    }
                    break;
                case Direction.Up:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingUp;
                        if ( _facingDirection == Direction.Left ) {
                            tuning += new Vector2(ConvertUnits.ToSimUnits(1), 0);
                        }
                    } else if ( IsStanding ) {
                        if ( IsStandingStill() ) {
                            if ( _facingDirection == Direction.Right ) {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(6), StandingAdjustments[_animationFrame]);
                            } else {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(7), StandingAdjustments[_animationFrame]);
                            }
                        } else if ( IsWalkingSpeed() ) {
                            if ( _facingDirection == Direction.Right ) {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(8), 0);
                            } else {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(9), 0);                                
                            }
                        }
                        else if ( IsJoggingSpeed() ) {
                            if ( _facingDirection == Direction.Right ) {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(4), StandingAdjustments[_animationFrame]);
                            } else {
                                tuning += new Vector2(ConvertUnits.ToSimUnits(5), StandingAdjustments[_animationFrame]);
                            }
                        } else {
                            //tuning += new Vector2(0, RunningUpAdjustments[_animationFrame]);
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingUp + new Vector2(0, JumpingAdjustments[_animationFrame]);
                    }
                    break;
                case Direction.Down:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingDown;//  + new Vector2(0, DuckingAdjustments[_animationFrame]);
                        if ( _facingDirection == Direction.Left ) {
                            tuning += new Vector2(ConvertUnits.ToSimUnits(1), 0);
                        }
                    }
                    else if (IsStanding) {
                        tuning = ShotAdjustmentStandingDown;
                    } else {
                        tuning = ShotAdjustmentJumpingDown + new Vector2(0, JumpingAdjustments[_animationFrame]);
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingUpRight + new Vector2(0, DuckingAdjustments[_animationFrame]);
                        if ( _facingDirection == Direction.Left ) {
                            tuning += new Vector2(ConvertUnits.ToSimUnits(1), 0);
                        }
                    } else if ( IsStanding ) {
                        tuning = ShotAdjustmentStandingUpRight;
                        if ( IsStandingStill() ) {
                            tuning += new Vector2(0, StandingAdjustments[_animationFrame]);
                        } else if ( IsWalkingSpeed() ) {
                            tuning += new Vector2(0, WalkingAimUpRightAdjustments[_animationFrame]);
                        } else if ( IsJoggingSpeed() ) {
                            tuning += new Vector2(0, JoggingUpRightAdjustments[_animationFrame]);
                        } else {
                            tuning += new Vector2(0, RunningUpRightAdjustments[_animationFrame]);
                        }
                    } else {
                        tuning = ShotAdjustmentJumpingUpRight + new Vector2(0, JumpingAdjustments[_animationFrame]);
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    if ( IsDucking ) {
                        tuning = ShotAdjustmentDuckingDownRight + new Vector2(0, DuckingAdjustments[_animationFrame]);
                        if ( _facingDirection == Direction.Left ) {
                            tuning += new Vector2(ConvertUnits.ToSimUnits(1), 0);
                        }
                    }
                    else if ( IsStanding ) {
                        if ( IsStandingStill() ) {
                            tuning = ShotAdjustmentStandingDownRight + new Vector2(0, StandingAdjustments[_animationFrame]);
                        } else if ( IsWalkingSpeed() ) {
                            tuning += new Vector2(0, WalkingAimDownRightAdjustments[_animationFrame]);
                        } else if ( IsJoggingSpeed() ) {
                            tuning += new Vector2(0, JoggingDownRightAdjustments[_animationFrame]);
                        } else {
                            tuning += new Vector2(0, RunningDownRightAdjustments[_animationFrame]);
                        }

                    } else {
                        tuning = ShotAdjustmentJumpingDownRight + new Vector2(0, JumpingAdjustments[_animationFrame]);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("shotDirection");
            }
            if ( _facingDirection == Direction.Left ) {
                tuning = new Vector2(-tuning.X, tuning.Y);
            }

            position += tuning;

            return position;
        }

        #region ShotAdjustments

        private static readonly Vector2 ShotAdjustmentStandingRight = new Vector2(-.08f, .09f);
        private static readonly Vector2 ShotAdjustmentStandingUpRight = new Vector2(-.02f, .26f);
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

        /*
         * A tedious set of animation-specific directions.
         */

        private static readonly float[] DuckingAdjustments = new[] {
            ConvertUnits.ToSimUnits(0), 
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(1),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-2),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
        };

        private static readonly float[] JumpingAdjustments = new[] {
            ConvertUnits.ToSimUnits(0), 
            ConvertUnits.ToSimUnits(4),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(4),
            ConvertUnits.ToSimUnits(4),
            ConvertUnits.ToSimUnits(4),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
        };


        private static readonly float[] RunningStraightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-10),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(-10),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-9),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-6)
        };

        private static readonly float[] JoggingStraightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-2),
            ConvertUnits.ToSimUnits(-2),
            ConvertUnits.ToSimUnits(-3),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
        };

        private static readonly float[] WalkingStraightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-8),
            ConvertUnits.ToSimUnits(-7),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-6),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-5),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-5),            
        };

        private static readonly float[] WalkingAimUpRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-2),
            ConvertUnits.ToSimUnits(-3),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-3),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-4),
            ConvertUnits.ToSimUnits(-3),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-2),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(-2),
        };

        private static readonly float[] WalkingAimDownRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(9),
            ConvertUnits.ToSimUnits(9),
            ConvertUnits.ToSimUnits(10),
            ConvertUnits.ToSimUnits(10),
            ConvertUnits.ToSimUnits(9),
            ConvertUnits.ToSimUnits(9),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
        };

        private static readonly float[] StandingAdjustments = new[] {
            ConvertUnits.ToSimUnits(0), 
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(-1),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
        };

        private static readonly float[] JoggingUpRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-10),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(-12),
            ConvertUnits.ToSimUnits(-13),
            ConvertUnits.ToSimUnits(-13),
            ConvertUnits.ToSimUnits(-14),
            ConvertUnits.ToSimUnits(-14),
            ConvertUnits.ToSimUnits(-14),
            ConvertUnits.ToSimUnits(-14),
            ConvertUnits.ToSimUnits(-12),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(-11),
            ConvertUnits.ToSimUnits(-10),
        };

        private static readonly float[] JoggingDownRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(14),
            ConvertUnits.ToSimUnits(14),
            ConvertUnits.ToSimUnits(14),
            ConvertUnits.ToSimUnits(13),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(13),
            ConvertUnits.ToSimUnits(14),
            ConvertUnits.ToSimUnits(15),
            ConvertUnits.ToSimUnits(15),
            ConvertUnits.ToSimUnits(14),
        };

        private static readonly float[] JoggingUpAdjustments = new[] {
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
            ConvertUnits.ToSimUnits(0),
        };

        private static readonly float[] RunningUpRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(-15),
            ConvertUnits.ToSimUnits(-15),
            ConvertUnits.ToSimUnits(-15),
            ConvertUnits.ToSimUnits(-18),
            ConvertUnits.ToSimUnits(-20),
            ConvertUnits.ToSimUnits(-20),
            ConvertUnits.ToSimUnits(-20),
            ConvertUnits.ToSimUnits(-20),
            ConvertUnits.ToSimUnits(-17),
            ConvertUnits.ToSimUnits(-14),
            ConvertUnits.ToSimUnits(-15),
        };

        private static readonly float[] RunningDownRightAdjustments = new[] {
            ConvertUnits.ToSimUnits(15),
            ConvertUnits.ToSimUnits(15),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(12),
            ConvertUnits.ToSimUnits(10),
            ConvertUnits.ToSimUnits(10),
            ConvertUnits.ToSimUnits(10),
            ConvertUnits.ToSimUnits(11),
            ConvertUnits.ToSimUnits(13),
            ConvertUnits.ToSimUnits(15),
            ConvertUnits.ToSimUnits(15),
        };

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
                                if ( _isDashing && _facingDirection != Direction.Left ) {
                                    _isDashing = false;
                                }
                                _facingDirection = Direction.Left;
                                
                                if ( _body.LinearVelocity.X > -minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(-minLateralSpeed,
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxGroundSpeedMs] ) {
                                    if ( PlayerControl.Control.IsRunButtonDown() && !_isDashing ) {
                                        _body.LinearVelocity -= new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else if ( !_isDashing ) {
                                    _body.LinearVelocity = new Vector2(-Constants[PlayerMaxGroundSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Right:
                            case Direction.UpRight:
                            case Direction.DownRight:
                                if ( _isDashing && _facingDirection != Direction.Right) {
                                    _isDashing = false;
                                }
                                _facingDirection = Direction.Right;

                                if ( _body.LinearVelocity.X < minLateralSpeed ) {
                                    _body.LinearVelocity = new Vector2(minLateralSpeed,
                                                                       _body.LinearVelocity.Y);
                                } else if ( Math.Abs(_body.LinearVelocity.X) < Constants[PlayerMaxGroundSpeedMs] ) {
                                    if (PlayerControl.Control.IsRunButtonDown() && !_isDashing ) {
                                        _body.LinearVelocity += new Vector2(
                                            GetVelocityDelta(Constants[PlayerAccelerationMss], gameTime), 0);
                                    }
                                } else if ( !_isDashing ) {
                                    _body.LinearVelocity = new Vector2(Constants[PlayerMaxGroundSpeedMs],
                                                                       _body.LinearVelocity.Y);
                                }
                                break;
                            case Direction.Down:
                                isDucking = true;
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                _isDashing = false;
                                AdjustFacingDirectionForAim();
                                break;
                            case Direction.Up:
                                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
                                _isDashing = false;
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
            return GetVelocityDelta(acceleration, (float) gameTime.ElapsedGameTime.TotalMilliseconds);
        }

        private float GetVelocityDelta(float acceleration, float gameTime) {
            return gameTime / 1000f * acceleration;
        }

        private void HandleDash(GameTime gameTime) {
            if ( _isDashing ) {               
                _dashTimer.Update(gameTime);
                if ( _dashTimer.IsTimeUp() || Math.Abs(_body.LinearVelocity.X) < Constants[PlayerInitRunSpeedMs] + 1) {
                    StopDash();
                    return;
                }
            }

            if ( !IsStanding ) {
                return;
            }

            if ( PlayerControl.Control.IsNewRunButton() && Math.Abs(_body.LinearVelocity.X) > 1f) {
                if ( _timeSinceRunButton > 0 && _timeSinceRunButton < 200 ) {
                    Dash();
                } else {
                    _timeSinceRunButton = 0;
                }
            } else {
                _timeSinceRunButton += gameTime.ElapsedGameTime.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Stops dashing
        /// </summary>
        private void StopDash() {
            if ( IsStanding ) {
                switch ( _facingDirection ) {
                    case Direction.Right:
                        _body.LinearVelocity =
                            new Vector2(Math.Min(Math.Abs(_body.LinearVelocity.X), _dashReturnVelocity),
                                        _body.LinearVelocity.Y);
                        break;
                    case Direction.Left:
                        _body.LinearVelocity = new Vector2(Math.Max(_body.LinearVelocity.X, -_dashReturnVelocity),
                                                           _body.LinearVelocity.Y);
                        break;
                }
            }
            _isDashing = false;
            _body.IsBullet = false;
        }

        /// <summary>
        /// Begins a dash move
        /// </summary>
        private void Dash() {
            float velocity = Math.Abs(_body.LinearVelocity.X);
            float newVelocity;
            if ( velocity < 5 ) {
                newVelocity = 15;
            } else if ( velocity < 10 ) {
                newVelocity = 25;
            } else if ( velocity < 15 ) {
                newVelocity = 35;
            } else if ( velocity < 20 ) {
                newVelocity = 40;
            } else {
                newVelocity = 50;
            }

            switch ( _facingDirection ) {
                case Direction.Right:
                    _body.LinearVelocity = new Vector2(newVelocity, _body.LinearVelocity.Y);
                    break;
                case Direction.Left:
                    _body.LinearVelocity = new Vector2(-newVelocity, _body.LinearVelocity.Y);
                    break;
            }

            _isDashing = true;
            _body.IsBullet = true;
            _dashTimer = new Timer(500);
            _dashReturnVelocity = velocity + GetVelocityDelta(Constants[PlayerAccelerationMss], 750f);

            // When we first begin dashing, break any dash-vulnerable blocks we're already in contact with.
            // Only new contacts will trigger the collision handlers.
            ContactEdge edge = _body.ContactList;
            while ( edge != null ) {
                if ( edge.Contact.FixtureA.GetUserData().IsTerrain || edge.Contact.FixtureB.GetUserData().IsTerrain ) {
                    if ( edge.Contact.FixtureA.GetUserData().IsTerrain ) {
                        DashCollisionHandler(edge.Contact.FixtureB, edge.Contact.FixtureA, edge.Contact);
                    } else {
                        DashCollisionHandler(edge.Contact.FixtureA, edge.Contact.FixtureB, edge.Contact);                        
                    }
                }
                edge = edge.Next;
            }
        }

        /// <summary>
        /// Handles jump input 
        /// </summary>
        private void HandleJump(GameTime gameTime) {

            if ( PlayerControl.Control.IsNewJump() ) {
                if ( IsStanding && _hover == null ) {
                    _jumpInitiated = true;
                    _airBoostTime = 0;
                    _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -Constants[PlayerJumpSpeed]);
                } else {
                    if ( _hover == null && _canHover ) {
                        ActivateHover();
                    } else {
                        DeactivateHover();
                        _canHover = false;
                    }
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

            if ( _hover != null && _hover.Disposed ) {
                DeactivateHover();
            }
        }

        /// <summary>
        /// Activates the hover boots
        /// </summary>
        private void ActivateHover() {
            if ( !IsHoverActive ) {
                EnceladusGame.Instance.Register(_hover = new HoverBoots(500));
                _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, 0f);
                _body.IgnoreGravity = true;
                UpdateStanding();
                _canHover = false;
            }
        }

        /// <summary>
        /// Deactivates the hover boots
        /// </summary>
        private void DeactivateHover() {
            if ( _hover != null ) {
                _hover.Dispose();
                _hover = null;
            }
            _body.IgnoreGravity = false;
            UpdateStanding();
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

            /*
             * Resizing the body sometimes results in our slight 
             * overlap with the floor, resulting in Box2D detecting 
             * a collision and bouncing the body upwards. To get 
             * around this, we just disable any existing contact 
             * edges with terrain for the next timestep.
             */
            ContactEdge edge = _body.ContactList;
            while ( edge != null ) {
                if ( edge.Contact.FixtureA.GetUserData().IsTerrain 
                    || edge.Contact.FixtureB.GetUserData().IsTerrain ) {
                    edge.Contact.Enabled = false;
                }
                edge = edge.Next;
            }

            var newPosition = GetNewBodyPosition(halfHeight, positionalCorrection);
            PolygonShape shape = (PolygonShape) _body.FixtureList.First().Shape;
            shape.SetAsBox(width / 2, halfHeight);
            _body.SetTransformIgnoreContacts(ref newPosition, 0);

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
                var position = GetShotPlacement(out shotDirection);
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
        private double _timeSinceLastAnimationUpdate;
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

            SetImage(_standAimAnimation[AimRightFrame], 0);

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
                    SetImage(_scooterAnimation[_animationFrame++], 0);
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
                    SetImage(_scooterAnimation[_animationFrame--], 0);
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
                float timeTillUpdate = 1000 / NumScootFrames / speed;
                if ( _timeSinceLastAnimationUpdate > timeTillUpdate
                    || _prevAnimation != _currentAnimation ) {
                    _animationFrame %= NumScooterFrames;
                    if ( _animationFrame == 0 ) {
                        _animationFrame = ScootFrame;
                    }
                    SetImage(_scooterAnimation[_animationFrame++], _timeSinceLastAnimationUpdate % timeTillUpdate);
                }
            }
        }

        private void AnimateJumping(Direction aimDirection) {

            if ( _jumpInitiated ) {
                _currentAnimation = Animation.JumpInit;
                if ( _currentAnimation != _prevAnimation ) {
                    _animationFrame = 0;
                }
                SetImage(_jumpAnimation[_animationFrame++], 0);
                if ( _animationFrame >= JumpAimUpFrame ) {
                    _jumpInitiated = false;
                }
            } else {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.Right:
                        _currentAnimation = Animation.JumpAimStraight;
                        if ( _currentAnimation != _prevAnimation ) {
                            _animationFrame = JumpAimStraightFrame;
                            SetImage(_jumpAnimation[JumpAimStraightFrame], 0);
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.JumpAimUp;
                        if ( _currentAnimation != _prevAnimation ) {
                            _animationFrame = JumpAimUpFrame;
                            SetImage(_jumpAnimation[JumpAimUpFrame], 0);
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.JumpAimDown;
                        if ( _currentAnimation != _prevAnimation ) {
                            _animationFrame = JumpAimDownFrame;
                            SetImage(_jumpAnimation[JumpAimDownFrame], 0);
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.JumpAimDiagonalUp;
                        if ( _currentAnimation != _prevAnimation ) {
                            _animationFrame = JumpAimUpRightFrame;
                            SetImage(_jumpAnimation[JumpAimUpRightFrame], 0);
                        }

                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.JumpAimDiagonalDown;
                        if ( _currentAnimation != _prevAnimation ) {
                            _animationFrame = JumpAimDownRightFrame;
                            SetImage(_jumpAnimation[JumpAimDownRightFrame], 0);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void AnimateRunning(Direction aimDirection) {
            float runSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);

            float timeTilAnimationUpdate = 1000f / NumRunAimStraightFrames / runSpeed;
            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.RunAimStraight;
                    if ( _timeSinceLastAnimationUpdate > timeTilAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumRunAimStraightFrames;
                        SetImage(_runAimStraightAnimation[_animationFrame],
                                 _timeSinceLastAnimationUpdate % timeTilAnimationUpdate);
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.RunAimUp;
                    if ( _timeSinceLastAnimationUpdate > timeTilAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumRunAimFrames;
                        SetImage(_runAimUpAnimation[_animationFrame],
                                 _timeSinceLastAnimationUpdate % timeTilAnimationUpdate);
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.RunAimDown;
                    if ( _timeSinceLastAnimationUpdate > timeTilAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumRunAimFrames;
                        SetImage(_runAimDownAnimation[_animationFrame],
                                 _timeSinceLastAnimationUpdate % timeTilAnimationUpdate);
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.RunAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > timeTilAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumRunAimFrames;
                        SetImage(_runAimDiagonalUpAnimation[_animationFrame],
                                 _timeSinceLastAnimationUpdate % timeTilAnimationUpdate);
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.RunAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > timeTilAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumRunAimFrames;
                        SetImage(_runAimDiagonalDownAnimation[_animationFrame],
                                 _timeSinceLastAnimationUpdate % timeTilAnimationUpdate);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateJogging(Direction aimDirection) {
            float jogSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerJogSpeedMultiplier]);

            float timeTillAnimationUpdate = 1000f / NumJogFrames / jogSpeed;
            double residualTime = _timeSinceLastAnimationUpdate % timeTillAnimationUpdate;
            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.JogAimStraight;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumJogFrames;
                        SetImage(_jogAimStraightAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.JogAimUp;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumJogFrames;
                        SetImage(_jogAimUpAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.JogAimDown;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumJogFrames;
                        SetImage(_jogAimDownAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.JogAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumJogFrames;
                        SetImage(_jogAimDiagonalUpAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.JogAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumJogFrames;
                        SetImage(_jogAimDiagonalDownAnimation[_animationFrame], residualTime);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateWalking(Direction aimDirection) {
            float walkSpeed = Math.Abs(_body.LinearVelocity.X * Constants[PlayerWalkSpeedMultiplier]);

            float timeTillAnimationUpdate = 1000f / NumWalkAimStraightFrames / walkSpeed;
            double residualTime = _timeSinceLastAnimationUpdate % timeTillAnimationUpdate;
            switch ( aimDirection ) {
                case Direction.Left:
                case Direction.Right:
                    _currentAnimation = Animation.WalkAimStraight;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumWalkAimStraightFrames;
                        SetImage(_walkAimStraightAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.Up:
                    _currentAnimation = Animation.WalkAimUp;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumWalkAimFrames;
                        SetImage(_walkAimUpAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.Down:
                    _currentAnimation = Animation.WalkAimDown;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumWalkAimFrames;
                        SetImage(_walkAimDownAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.UpLeft:
                case Direction.UpRight:
                    _currentAnimation = Animation.WalkAimDiagonalUp;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumWalkAimFrames;
                        SetImage(_walkAimDiagonalUpAnimation[_animationFrame], residualTime);
                    }
                    break;
                case Direction.DownLeft:
                case Direction.DownRight:
                    _currentAnimation = Animation.WalkAimDiagonalDown;
                    if ( _timeSinceLastAnimationUpdate > timeTillAnimationUpdate
                         || _prevAnimation != _currentAnimation ) {
                        _animationFrame = (_animationFrame + 1) % NumWalkAimFrames;
                        SetImage(_walkAimDiagonalDownAnimation[_animationFrame], residualTime);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AnimateStandingStill(Direction aimDirection) {
            float timeTillAnimationChange = 500f;
            double residualTime = _timeSinceLastAnimationUpdate % timeTillAnimationChange;
            if ( !IsDucking ) {
                switch ( aimDirection ) {
                    case Direction.Left:
                    case Direction.Right:
                        _currentAnimation = Animation.AimStraight;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < AimRightFrame
                                 || _animationFrame > AimRightFrame + 1 ) {
                                _animationFrame = AimRightFrame;
                            }
                            SetImage(_standAimAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.AimUp;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < AimUpFrame
                                 || _animationFrame > AimUpFrame + 1 ) {
                                _animationFrame = AimUpFrame;
                            }
                            SetImage(_standAimAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.AimDown;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < AimDownFrame
                                 || _animationFrame > AimDownFrame + 1 ) {
                                _animationFrame = AimDownFrame;
                            }
                            SetImage(_standAimAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.AimDiagonalUp;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < AimUpRightFrame
                                 || _animationFrame > AimUpRightFrame + 1 ) {
                                _animationFrame = AimUpRightFrame;
                            }
                            SetImage(_standAimAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.AimDiagonalDown;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < AimDownRightFrame
                                 || _animationFrame > AimDownRightFrame + 1 ) {
                                _animationFrame = AimDownRightFrame;
                            }
                            SetImage(_standAimAnimation[_animationFrame], residualTime);
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
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < CrouchAimStraightFrame
                                 || _animationFrame > CrouchAimStraightFrame + 1 ) {
                                _animationFrame = CrouchAimStraightFrame;
                            }
                            SetImage(_crouchAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.Up:
                        _currentAnimation = Animation.CrouchAimUp;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < CrouchAimUpFrame
                                 || _animationFrame > CrouchAimUpFrame + 1 ) {
                                _animationFrame = CrouchAimUpFrame;
                            }
                            SetImage(_crouchAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.Down:
                        _currentAnimation = Animation.CrouchAimDown;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < CrouchAimDownFrame
                                 || _animationFrame > CrouchAimDownFrame + 1 ) {
                                _animationFrame = CrouchAimDownFrame;
                            }
                            SetImage(_crouchAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                        _currentAnimation = Animation.CrouchAimDiagonalUp;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < CrouchAimUpRightFrame
                                 || _animationFrame > CrouchAimUpRightFrame + 1 ) {
                                _animationFrame = CrouchAimUpRightFrame;
                            }
                            SetImage(_crouchAnimation[_animationFrame], residualTime);
                        }
                        break;
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        _currentAnimation = Animation.CrouchAimDiagonalDown;
                        if ( _timeSinceLastAnimationUpdate > timeTillAnimationChange
                             || _prevAnimation != _currentAnimation ) {
                            _animationFrame++;
                            if ( _animationFrame < CrouchAimDownRightFrame
                                 || _animationFrame > CrouchAimDownRightFrame + 1 ) {
                                _animationFrame = CrouchAimDownRightFrame;
                            }
                            SetImage(_crouchAnimation[_animationFrame], residualTime);
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

        private bool IsRunningSpeed() {
            return Math.Abs(_body.LinearVelocity.X) > Constants[PlayerMaxGroundSpeedMs] * .6f;
        }

        private bool IsWalkingSpeed() {
            return Math.Abs(_body.LinearVelocity.X) <= (Constants[PlayerInitSpeedMs] + Constants[PlayerInitRunSpeedMs]) / 2f;
        }

        #endregion

        public void HitBy(IEnemy enemy) {
            DeactivateHover();

            // TODO: adjust based on equipment
            Health -= enemy.BaseDamage;
            if ( Health <= 0 ) {
                Health = 0;
                EnceladusGame.Instance.Die();
                return;
            }

            Vector2 diff = Position - enemy.Position;
            _body.LinearVelocity = new Vector2(0);
            _body.ApplyLinearImpulse(diff * Constants[PlayerKnockbackAmt] * _body.Mass);
            _timeUntilRegainControl = (long)(Constants[PlayerKnockbackTime] * 1000);

            _flashAnimation.SetFlashTime(1500);
            MakeInvulnerable(1500);

            SoundEffectManager.Instance.PlaySoundEffect("hurt");

            // Make sure we're in the air or on the ground as necessary
            _standingMonitor.IgnoreStandingUpdatesNextNumFrames = 0;
            UpdateStanding();
        }

        /// <summary>
        /// Makes the player invulnerable for the given time, 
        /// during which they will only collide with terrain.
        /// </summary>
        public void MakeInvulnerable(int timeMs) {
            _body.CollidesWith = EnceladusGame.TerrainCategory;
            _invulnerabilityTimer = new Timer(timeMs);
        }

        /// <summary>
        /// Immediately end the player's invulerability.
        /// </summary>
        public void RemoveInvulnerability() {
            _body.CollidesWith = EnceladusGame.TerrainCategory | EnceladusGame.EnemyCategory | EnceladusGame.PlayerSensorCategory;
            _invulnerabilityTimer = null;
        }

        public void Dispose() {
            if ( _body != null ) {
                _body.Dispose();
            }
        }

        public void Pickup(HealthPickup healthPickup) {
            Health += 10;
            Health = Math.Min(HealthCapacity, Health);
            SoundEffectManager.Instance.PlaySoundEffect("pickup");
        }

        protected World _world;
        protected Body _body;
        protected readonly FlashAnimation _flashAnimation = new FlashAnimation();
        protected readonly StandingMonitor _standingMonitor = new StandingMonitor();
        private Timer _invulnerabilityTimer;
        private int _rapidFireSetting;
        private double _timeSinceRunButton;

        private bool _isDashing;
        private Timer _dashTimer;
        private float _dashReturnVelocity;

        public void Save(SaveState save) {
            Equipment.Save(save);
            save.PlayerHealth = Health;
            save.PlayerHealthCapacity = HealthCapacity;
        }

        public void LoadFromSave(SaveState save) {
            Equipment.LoadFromSave(save);
            HealthCapacity = save.PlayerHealthCapacity ?? 100;
            Health = save.PlayerHealth;
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
            if ( IsHoverActive ) {
                IsStanding = true;
            } else {
                _standingMonitor.UpdateStanding(_body, _world, GetStandingLocation(), Width - .05f);
                IsStanding = _standingMonitor.IsStanding;
                if ( IsStanding ) {
                    _canHover = true;
                }
            }
        }
        protected void UpdateFlash(GameTime gameTime) {
            _flashAnimation.UpdateFlash(gameTime);
        }

        /// <summary>
        /// Notifies the player that the item selected has changed to the given one.
        /// </summary>
        public void SelectedItemChanged(CollectibleItem item) {
            _activeItem = item;
        }

        public void SpringboardLaunch() {
            _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -20);
            _jumpInitiated = true;
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

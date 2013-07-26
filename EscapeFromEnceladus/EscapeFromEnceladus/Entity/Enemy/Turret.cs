using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {
    public class Turret : GameEntityAdapter, IGameEntity, IEnemy {

        protected const int NumFrames = 4;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
        private const int Barrel = 0;
        private const int Cover = 1;
        private const int WeakSpot = 2;
        private const int Hatch = 3;
        private static Texture2D _projectileImage;

        private const int ImageHeight = 64;
        private const int ImageWidth = 64;
        private const float Height = 1f;
        private const float Width = .5f;
        private const float Radius = Width;
        private const float TurretSpeedRps = .20f;
        private const int Range = 20;
        private const int ProjectileSpeed = 5;
        private const float ProjectileRadius = .125f;

        protected World _world;
        private readonly Body _body;
        private readonly Direction _facingDirection;
        private float _barrelTargetRadians;
        private float _barrelAimRadians;

        // The guns don't quite have 180 degrees of vision
        private const float MaxAngleOffset = Projectile.Pi / 16;
        private const float LeftTopAngle = Projectile.PiOverTwo + MaxAngleOffset;
        private const float LeftBottomAngle = -Projectile.PiOverTwo - MaxAngleOffset;
        private const float RightBottomAngle = -Projectile.PiOverTwo + MaxAngleOffset;
        private const float RightTopAngle = Projectile.PiOverTwo - MaxAngleOffset;
        private const float UpRightAngle = MaxAngleOffset;
        private const float UpLeftAngle = Projectile.Pi - MaxAngleOffset;
        private const float DownRightAngle = -MaxAngleOffset;
        private const float DownLeftAngle = -Projectile.Pi + MaxAngleOffset;

        protected readonly FlashAnimation _flashAnimation = new FlashAnimation();
        protected int _hitPoints;
        private int _numProjectilesInAir = 0;
        private const double MinimumTimeBetweenShotsMs = 3000;
        private double _timeSinceLastShotMs = MinimumTimeBetweenShotsMs;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Enemy/Turret/Turret{0:0000}", i));
            }

            _projectileImage = content.Load<Texture2D>("Projectile/Projectile0000");
        }

        public Turret(Vector2 position, World world, Direction facing) {
            _facingDirection = facing;
            _world = world;
            
            _body = BodyFactory.CreateSolidArc(world, 1f, Projectile.Pi, 8, Radius, Vector2.Zero, Projectile.PiOverTwo);

            switch ( _facingDirection ) {
                case Direction.Left:
                    position += new Vector2(0, Height / 2);
                    break;
                case Direction.Right:
                    position += new Vector2(0, Height / 2);
                    _body.Rotation = -Projectile.Pi;
                    break;
                case Direction.Up:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = Projectile.PiOverTwo;
                    break;
                case Direction.Down:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = -Projectile.PiOverTwo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _body.IsStatic = true;
            _body.IgnoreGravity = true;
            _body.Position = position;
            _body.CollisionCategories = EnceladusGame.EnemyCategory;
            _body.CollidesWith = Category.All;

            _body.UserData = UserData.NewEnemy(this);

            _body.OnCollision += (a, b, contact) => {
                if ( b.Body.GetUserData().IsPlayer ) {
                    Player.Instance.HitBy(this);
                }
                return true;
            };

            _hitPoints = 8;
        }

        public override void Dispose() {
            _body.Dispose();
        }

        public override bool Disposed {
            get { return _body.IsDisposed; }
        }

        /// <summary>
        /// We hang on to the sprite batch so that we can use it to draw on for the death animation.
        /// </summary>
        private SpriteBatch _spriteBatch;

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {

            Vector2 position = _body.Position;
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Draw(spriteBatch, camera, displayPosition);

            _spriteBatch = spriteBatch;
        }

        private void Draw(SpriteBatch spriteBatch, Camera2D camera, Vector2 displayPosition) {
            Vector2 origin = new Vector2(ImageWidth, ImageHeight / 2f);

            float bodyRotation = -_body.Rotation;
            if ( _facingDirection == Direction.Up || _facingDirection == Direction.Down ) {
                bodyRotation = _body.Rotation;
            }

            float barrelRotation = Projectile.Pi - _barrelAimRadians;

            Color color = camera == null ? Color.White : _flashAnimation.IsActive ? _flashAnimation.FlashColor : SolidColorEffect.DisabledColor;
            spriteBatch.Draw(Animation[Barrel], displayPosition, null, color, barrelRotation, origin, 1f,
                             SpriteEffects.None, 0);

            spriteBatch.Draw(Animation[WeakSpot], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Hatch], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Cover], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
        }

        public override void Update(GameTime gameTime) {
            if ( _hitPoints <= 0 ) {
                Destroyed();
                return;
            }

            _timeSinceLastShotMs += gameTime.ElapsedGameTime.TotalMilliseconds;

            DetermineTargetAngle();
            UpdateBarrelAngle(gameTime);
            ShootPlayer();
            _flashAnimation.UpdateFlash(gameTime);
        }

        /// <summary>
        /// Shoots the player if there is a line of sight, 
        /// unless there is already a bullet in the air.
        /// </summary>
        private void ShootPlayer() {
            if ( _numProjectilesInAir > 0 || _timeSinceLastShotMs < MinimumTimeBetweenShotsMs ) {
                return;
            }

            bool playerInRange = false;
            Vector2 angle = new Vector2((float) (Math.Cos(_barrelAimRadians)), (float) -(Math.Sin(_barrelAimRadians)));
            Vector2 start = Position + (Radius + ProjectileRadius + .1f) * angle;
            float closestFraction = 1;
            Fixture closestFixture = null;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fraction < closestFraction ) {
                    closestFraction = fraction;
                    closestFixture = fixture;
                }
                if ( fixture.GetUserData().IsPlayer )
                    playerInRange = true;
                return fraction;
            }, start, start + Range * angle);

            if ( playerInRange && closestFixture.GetUserData().IsPlayer ) {
                EnemyProjectile proj = new EnemyProjectile(_projectileImage, _world, start, angle * ProjectileSpeed, _barrelAimRadians, ProjectileRadius);
                proj.ProjectileDisposed += projectile => {
                    _numProjectilesInAir--;
                };
                EnceladusGame.Instance.Register(proj);
                _numProjectilesInAir++;
                _timeSinceLastShotMs = 0;
                SoundEffectManager.Instance.PlaySoundEffect("turretShoot");
            }            
        }

        private void Destroyed() {
            _body.Dispose();

            // Draw all our composite parts onto a buffer
            GraphicsDevice graphics = _spriteBatch.GraphicsDevice;
            RenderTarget2D renderTarget = new RenderTarget2D(graphics,
                                                             ImageWidth,
                                                             ImageHeight);
            graphics.SetRenderTarget(renderTarget);
            graphics.Clear(Color.Transparent);
            _spriteBatch.Begin();
            Vector2 displayPosition = Vector2.Zero;
            switch ( _facingDirection ) {
                case Direction.Left:
                    displayPosition = new Vector2(ImageWidth, ImageHeight / 2);
                    break;
                case Direction.Right:
                    displayPosition = new Vector2(0, ImageHeight / 2);
                    break;
                case Direction.Up:
                    displayPosition = new Vector2(ImageWidth / 2, ImageHeight);
                    break;
                case Direction.Down:
                    displayPosition = new Vector2(ImageWidth / 2, 0);
                    break;
            }
            Draw(_spriteBatch, null, displayPosition);
            _spriteBatch.End();
            graphics.SetRenderTarget(null);

            EnceladusGame.Instance.Register(new ShatterAnimation(_world, renderTarget, null,
                                                                 _body.Position + _body.GetWorldVector(new Vector2(-Radius - .05f, 0)), 8));
            SoundEffectManager.Instance.PlaySoundEffect("enemyExplode");
        }

        private void UpdateBarrelAngle(GameTime gameTime) {
            float target;
            float current;
            float maxMovement = (float) (TurretSpeedRps * gameTime.ElapsedGameTime.TotalSeconds * Projectile.Pi * 2);

            switch ( _facingDirection ) {
                case Direction.Left: // angle > pi/2 || angle < -pi/2
                    target = NormalizeAngle(_barrelTargetRadians);
                    current = NormalizeAngle(_barrelAimRadians);
                    break;
                case Direction.Right: // angle < pi/2 && angle > -pi/2
                case Direction.Up: // angle > 0 && angle < pi/2
                    target = _barrelTargetRadians;
                    current = _barrelAimRadians;
                    break;
                case Direction.Down: // angle < 0 && angle > -pi/2
                    target = _barrelTargetRadians;
                    // this orientation, uniquely, has to worry about rotating in 
                    // the opposite direction when pointing to the left.
                    if ( target == Projectile.Pi ) {
                        target = -Projectile.Pi;
                    }
                    current = _barrelAimRadians;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            float diff = target - current;
            if ( diff >= 0 ) {
                diff = Math.Min(diff, maxMovement);
            } else {
                diff = Math.Max(diff, -maxMovement);
            }
            _barrelAimRadians = DenormalizeAngle(current + diff);

        }

        /// <summary>
        /// Returns an angle in the range [0,2pi)
        /// </summary>
        private float NormalizeAngle(float angle) {
            if ( angle < 0 ) {
                return (float) (2 * Projectile.Pi + angle);
            } else {
                return angle;
            }
        }

        /// <summary>
        /// Returns an angle in the range (-pi,pi]
        /// </summary>
        private float DenormalizeAngle(float angle) {
            if ( angle <= Projectile.Pi ) {
                return angle;
            } else {
                return (float) (angle - Projectile.Pi * 2);
            }
        }

        /// <summary>
        /// Determines where the aim target should be, constrained to turret's range
        /// </summary>
        private void DetermineTargetAngle() {
            Vector2 diff = Player.Instance.Position - Position;
            float angle = (float) Math.Atan2(-diff.Y, diff.X);

            // Depending on our facing direction, we need to determine if the player
            // is in our 180 degree range and adjust the target accordingly.
            switch ( _facingDirection ) {
                case Direction.Left:
                    if ( angle >= LeftTopAngle || angle <= LeftBottomAngle) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle > 0 ? LeftTopAngle : LeftBottomAngle;
                    }
                    break;
                case Direction.Right:
                    if ( angle >= RightBottomAngle && angle <= RightTopAngle) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle > 0 ? RightTopAngle : RightBottomAngle;
                    }
                    break;
                case Direction.Up:
                    if ( angle >= UpRightAngle && angle <= UpLeftAngle ) {
                        _barrelTargetRadians = angle;
                    } else {
                        if ( angle >= 0 ) {
                            _barrelTargetRadians = angle > Projectile.PiOverTwo ? UpLeftAngle : UpRightAngle;
                        } else {
                            _barrelTargetRadians = angle > -Projectile.PiOverTwo ? UpRightAngle : UpLeftAngle;
                        }
                    }
                    break;
                case Direction.Down:
                    if ( angle <= DownRightAngle && angle >= DownLeftAngle ) {
                        _barrelTargetRadians = angle;
                    } else {
                        if ( angle < 0 ) {
                            _barrelTargetRadians = angle < -Projectile.PiOverTwo ? DownLeftAngle : DownRightAngle;
                        } else {
                            _barrelTargetRadians = angle > Projectile.PiOverTwo ? DownLeftAngle : DownRightAngle;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Vector2 Position {
            get { return _body.Position; }
        }

        public int BaseDamage {
            get { return 10; }
        }

        public void HitBy(Projectile projectile) {
            _hitPoints -= projectile.BaseDamage;
            _flashAnimation.SetFlashTime(150);
        }
    }
}

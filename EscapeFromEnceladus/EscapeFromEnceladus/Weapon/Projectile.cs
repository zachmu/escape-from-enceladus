using System;
using System.Linq;
using Enceladus.Entity;
using Enceladus.Entity.Enemy;
using Enceladus.Farseer;
using Enceladus.Map;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Enceladus.Weapon {
    /// <summary>
    /// Base class for projectiles
    /// </summary>
    public abstract class Projectile : IWeapon {
        
        // Important angles in our aiming system (sorry, tau)
        public const float Pi = (float) Math.PI;
        public const float PiOverTwo = (float) (Math.PI / 2f);
        public const float PiOverFour = (float) (Math.PI / 4f);
        public const float PiOverEight = (float) (Math.PI / 8f);
        public const float ThreePiOverFour = (float) (3f * (Math.PI / 4f));
        public const float ThreePiOverEight = (float) (3f * (Math.PI / 8f));
        public const float FivePiOverEight = (float) (5f * (Math.PI / 8f));
        public const float SevenPiOverEight = (float) (7f * (Math.PI / 8f));

        public const string ProjectileOffsetX = "Projectile offset X";
        public const string ProjectileOffsetY = "Projectile offset Y";

        protected Body _body;
        private bool _disposed;
        protected Direction _direction;
        protected double _timeToLiveMs = 10000;
        protected bool _defunct = false;

        private static readonly Random random = new Random();

        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position {
            get { return _body.Position; }
        }

        protected Projectile(Vector2 position, World world, Direction direction, int speed, float width, float height) {
            _body = BodyFactory.CreateRectangle(world, width, height, 1000f);
            _body.IsStatic = false;
            _body.Restitution = .2f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.IsBullet = true;
            _body.FixedRotation = true;
            _body.IgnoreGravity = true;
            _body.CollidesWith = EnceladusGame.TerrainCategory | EnceladusGame.EnemyCategory;
            _body.CollisionCategories = EnceladusGame.PlayerProjectileCategory;
            _body.UserData = UserData.NewProjectile(this);

            _body.OnCollision += CollisionHandler();

            _direction = direction;

            switch (direction) {
                case Direction.Left:
                    _body.LinearVelocity = new Vector2(-speed, 0);
                    break;
                case Direction.Right:
                    _body.LinearVelocity = new Vector2(speed, 0);
                    break;
                case Direction.Down:
                    _body.LinearVelocity = new Vector2(0, speed);
                    break;
                case Direction.Up:
                    _body.LinearVelocity = new Vector2(0, -speed);
                    break;
                case Direction.UpLeft:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * -speed), (float) (Math.Sqrt(2) * -speed));
                    break;
                case Direction.UpRight:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * speed), (float) (Math.Sqrt(2) * -speed));
                    break;
                case Direction.DownLeft:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * -speed), (float) (Math.Sqrt(2) * speed));
                    break;
                case Direction.DownRight:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * speed), (float) (Math.Sqrt(2) * speed));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        } 
        
        /// <summary>
        /// Returns the angle, in radians, to rotate this projectile's 
        /// sprite due to its direction.
        /// </summary>
        public static float GetSpriteRotation(Direction direction) {
            switch ( direction ) {
                case Direction.Left:
                    return (float) Math.PI;
                case Direction.Right:
                    return 0;
                case Direction.Up:
                    return -PiOverTwo;
                case Direction.Down:
                    return PiOverTwo;
                case Direction.UpLeft:
                    return -ThreePiOverFour;
                case Direction.UpRight:
                    return -PiOverFour;
                case Direction.DownLeft:
                    return ThreePiOverFour;
                case Direction.DownRight:
                    return PiOverFour;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns the angle corresponding to the given direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static float GetAngle(Direction direction) {
            switch ( direction ) {
                case Direction.Left:
                    return Pi;
                case Direction.Right:
                    return 0;
                case Direction.Up:
                    return Pi / 2;
                case Direction.Down:
                    return -Pi / 2;
                case Direction.UpLeft:
                    return 3f * Pi / 4f;
                case Direction.UpRight:
                    return Pi / 4f;
                case Direction.DownLeft:
                    return -3f * Pi / 4f;
                case Direction.DownRight:
                    return -Pi / 4f;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }


        /// <summary>
        /// Returns the collision handler for this projectile.
        /// </summary>
        protected virtual OnCollisionEventHandler CollisionHandler() {
            return (a, b, contact) => {
                _body.IgnoreGravity = false;

                if ( !_defunct ) {
                    if ( b.GetUserData().IsEnemy ) {
                        HitEnemy(b.GetUserData().Enemy);
                    } else if ( b.GetUserData().IsTerrain ) {
                        HitTerrain(contact);
                    } else if ( b.GetUserData().IsDoor ) {
                        if ( b.GetUserData().Door.IsOpen() ) {
                            return false;
                        } else {
                            HitDoor(b.GetUserData().Door);
                        }
                    } else {
                        _timeToLiveMs = 0;
                    }
                }

                return true;
            };
        }

        protected void HitDoor(Door door) {
            bool openedBy = door.HitBy(this);
            if ( !openedBy ) {
                BounceOff();
            } else {
                _timeToLiveMs = 0;
            }
        }

        private void BounceOff() {
            _body.IgnoreGravity = false;
            _body.FixedRotation = false;
            _body.Friction = .8f;
            _body.ApplyAngularImpulse(-20 + 40 * (float) random.NextDouble());
            _timeToLiveMs = 1000;
            SoundEffectManager.Instance.PlaySoundEffect("bulletBounce");
            _defunct = true;
        }

        protected float GetAlpha() {
            if ( _defunct ) {
                return (float) (_timeToLiveMs / 1000f);
            } else {
                return 1.0f;
            }
        }

        protected void HitTerrain(Contact contact) {
            if ( _defunct ) {
                return;
            }
            FixedArray2<Vector2> points;
            Vector2 normal;
            contact.GetWorldManifold(out normal, out points);
            var hitTile = TileLevel.CurrentLevel.GetCollidedTile(points[0], normal);
            if ( hitTile != null ) {
                bool tileDestroyed = TileLevel.CurrentLevel.TileHitBy(hitTile, this);
                if ( !tileDestroyed ) {
                    BounceOff();
                } else {
                    _timeToLiveMs = 0;
                }
            } else {
                Console.WriteLine("Missed a tile. Collision was {0},{1} with normal {2}",
                                  points[0], points[1], normal);
            }
        }

        protected void HitEnemy(IEnemy enemy) {
            if ( enemy.HitBy(this) ) {
                _timeToLiveMs = 0;
            } else {
                BounceOff();
            }
        }

        public virtual void Update(GameTime gameTime) {
            _timeToLiveMs -= gameTime.ElapsedGameTime.Milliseconds;
            if ( _timeToLiveMs <= 0 ) {
                Dispose();
            }
        }

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
        }

        public abstract int DestructionFlags { get; }
        public abstract float BaseDamage { get; }
    }
}
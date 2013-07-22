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

namespace Enceladus.Weapon {

    /// <summary>
    /// Base class for projectiles
    /// </summary>
    public abstract class Projectile {
        
        // Important angles in our aiming system (sorry, tau)
        public const float Pi = (float) Math.PI;
        public const float PiOverTwo = (float) (Math.PI / 2f);
        public const float PiOverFour = (float) (Math.PI / 4f);
        public const float PiOverEight = (float) (Math.PI / 8f);
        public const float ThreePiOverFour = (float) (3f * (Math.PI / 4f));
        public const float ThreePiOverEight = (float) (3f * (Math.PI / 8f));
        public const float FivePiOverEight = (float) (5f * (Math.PI / 8f));
        public const float SevenPiOverEight = (float) (7f * (Math.PI / 8f));

        protected Body _body;
        private bool _disposed;
        protected Direction _direction;
        protected int _timeToLiveMs = 10000;

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
        protected float GetSpriteRotation() {
            switch ( _direction ) {
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
        /// Returns the collision handler for this projectile.
        /// </summary>
        protected virtual OnCollisionEventHandler CollisionHandler() {
            return (a, b, contact) => {
                       _body.IgnoreGravity = false;
                       _timeToLiveMs = 0;

                       if ( b.Body.GetUserData().IsEnemy ) {
                           HitEnemy(b.Body.GetUserData().Enemy);
                       } else if ( b.Body.GetUserData().IsTerrain ) {
                           HitTerrain(contact);
                       } else if ( b.Body.GetUserData().IsDoor ) {
                           HitDoor(b.Body.GetUserData().Door);
                       }

                       return true;
                   };
        }

        protected void HitDoor(Door door) {
            door.HitBy(this);
        }

        protected void HitTerrain(Contact contact) {
            FixedArray2<Vector2> points;
            Vector2 normal;
            contact.GetWorldManifold(out normal, out points);
            var hitTile = TileLevel.CurrentLevel.GetCollidedTile(points[0], normal);
            if ( hitTile != null ) {
                TileLevel.CurrentLevel.TileHitBy(hitTile, this);
            } else {
                Console.WriteLine("Missed a tile.  Collision was {0},{1} with normal {2}",
                                  points[0], points[1], normal);
            }
        }

        protected void HitEnemy(AbstractWalkingEnemy enemy) {
            enemy.HitBy(this);
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

        /// <summary>
        /// Returns the destruction flags for this projectile.
        /// </summary>
        public abstract int DestructionFlags { get; }

        /// <summary>
        /// Returns the base damage for this projectile.
        /// </summary>
        public abstract int BaseDamage { get; }
    }
}
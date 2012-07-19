using System;
using System.Diagnostics;
using System.Linq;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace Arena.Entity {

    /// <summary>
    /// Base class for projectiles
    /// </summary>
    public abstract class Projectile {
        
        // Important angles in our aiming system (sorry, tau)
        public static readonly float PiOverTwo = (float) (Math.PI / 2f);
        public static readonly float PiOverFour = (float) (Math.PI / 4f);
        public static readonly float PiOverEight = (float) (Math.PI / 8f);
        public static readonly float ThreePiOverFour = (float) (3f * (Math.PI / 4f));
        public static readonly float ThreePiOverEight = (float) (3f * (Math.PI / 8f));
        public static readonly float FivePiOverEight = (float) (5f * (Math.PI / 8f));
        public static readonly float SevenPiOverEight = (float) (7f * (Math.PI / 8f));

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

        public PolygonShape Shape {
            get { return (PolygonShape) _body.FixtureList.First().Shape; }
        }

        protected Projectile(Vector2 position, World world, Direction direction, int Speed, float width, float height) {
            _body = BodyFactory.CreateRectangle(world, width, height, 1000f);
            _body.IsStatic = false;
            _body.Restitution = .2f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.IgnoreGravity = true;
            _body.CollidesWith = Arena.TerrainCategory | Arena.EnemyCategory;
            _body.CollisionCategories = Arena.PlayerProjectileCategory;
            _body.UserData = UserData.NewProjectile(this);

            _body.OnCollision += CollisionHandler();

            _direction = direction;

            switch (direction) {
                case Direction.Left:
                    _body.LinearVelocity = new Vector2(-Speed, 0);
                    break;
                case Direction.Right:
                    _body.LinearVelocity = new Vector2(Speed, 0);
                    break;
                case Direction.Down:
                    _body.LinearVelocity = new Vector2(0, Speed);
                    break;
                case Direction.Up:
                    _body.LinearVelocity = new Vector2(0, -Speed);
                    break;
                case Direction.UpLeft:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * -Speed), (float) (Math.Sqrt(2) * -Speed));
                    break;
                case Direction.UpRight:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * Speed), (float) (Math.Sqrt(2) * -Speed));
                    break;
                case Direction.DownLeft:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * -Speed), (float) (Math.Sqrt(2) * Speed));
                    break;
                case Direction.DownRight:
                    _body.LinearVelocity = new Vector2((float) (Math.Sqrt(2) * Speed), (float) (Math.Sqrt(2) * Speed));
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

        protected void HitEnemy(Enemy enemy) {
            enemy.HitBy(this);
        }

        public void Update(GameTime gameTime) {
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
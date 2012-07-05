using System;
using System.Diagnostics;
using System.Linq;
using Arena.Entity;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {
    public class Shot : IGameEntity {
        private const int Speed = 25;

        private readonly Body _body;

        private static Texture2D Image { get; set; }
        private static SoundEffect Sfx { get; set; }

        private bool _disposed;
        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position {
            get { return _body.Position; }
        }

        public PolygonShape Shape {
            get { return (PolygonShape) _body.FixtureList.First().Shape; }
        }

        private readonly Direction _direction;
        private int _framesToLive = 200;

        public Shot(Vector2 position, World world, Direction direction) {
            _body = BodyFactory.CreateRectangle(world, .01f, .01f, 1000f);
            _body.IsStatic = false;
            _body.Restitution = .2f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.IgnoreGravity = true;
            _body.CollidesWith = Arena.TerrainCategory | Arena.EnemyCategory;
            _body.CollisionCategories = Arena.PlayerProjectileCategory;
            _body.UserData = UserData.NewProjectile(this);

            _body.OnCollision += (a, b, contact) => {
                _body.IgnoreGravity = false;
                _framesToLive = 1;

                if ( b.Body.GetUserData().IsEnemy ) {
                    HitEnemy(b.Body.GetUserData().Enemy);
                } else if ( b.Body.GetUserData().IsTerrain ) {
                    HitTerrain(contact);
                } else if ( b.Body.GetUserData().IsDoor ) {
                    HitDoor(b.Body.GetUserData().Door);
                }

                return true;
            };

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

            Sfx.Play();
        }

        private void HitDoor(Door door) {
            door.HitBy(this);
        }

        private void HitTerrain(Contact contact) {
            Stopwatch watch = new Stopwatch();
            watch.Start();
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
            watch.Stop();
            // Console.WriteLine("Took {0} ticks to evaluate hit", watch.ElapsedTicks);            
        }

        private void HitEnemy(Enemy enemy) {
            enemy.HitBy(this);
        }

        public void Update(GameTime gameTime) {
            if ( _framesToLive-- <= 0 ) { 
                Dispose();
            }
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("star");
            Sfx = content.Load<SoundEffect>("laser");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_body.IsDisposed ) {
                Vector2 position = _body.Position;
                Vector2 displayUnits = new Vector2();
                ConvertUnits.ToDisplayUnits(ref position, out displayUnits);
                spriteBatch.Draw(Image, displayUnits, Color.White);
            }
        }

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
        }
    }
}


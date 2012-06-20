using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Squared.Tiled;
using Test;

namespace Arena {
    public class Shot {

        private readonly Body _body;

        public static Texture2D Image { get; set; }

        public bool Disposed {
            get { return _body.IsDisposed; }
        }

        public Vector2 Position {
            get { return _body.Position; }
        }

        private readonly Player.Direction _direction;
        private int _framesToLive = 200;
        private Tile _destroyedTile = null;
        private readonly World _world;

        public Shot(Vector2 position, World world, Player.Direction direction) {
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

                if (((UserData) b.Body.UserData).IsEnemy) {
                    HitEnemy(((UserData) b.Body.UserData).Enemy);
                } else if ( ((UserData) b.Body.UserData).IsTerrain ) {
                    HitTerrain(contact);
                }

                return true;
            };

            _direction = direction;

            _world = world;

            switch ( direction ) {
                case Player.Direction.Left:
                    _body.LinearVelocity = new Vector2(-20, 0);
                    break;
                case Player.Direction.Right:
                    _body.LinearVelocity = new Vector2(20, 0);
                    break;
            }
        }

        private void HitTerrain(Contact contact) {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            FixedArray2<Vector2> points;
            Vector2 normal;
            contact.GetWorldManifold(out normal, out points);
            _destroyedTile = TileLevel.CurrentLevel.GetCollidedTile(points[0], normal);
            if ( _destroyedTile != null ) {
                // Console.WriteLine(String.Format("Hit tile at {0}", _destroyedTile.Position));
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

        public void Update() {
            if ( _framesToLive-- == 0 ) {
                _body.Dispose();
            }
            if (_destroyedTile != null) {
                TileLevel.CurrentLevel.DestroyTile(_destroyedTile);
                _destroyedTile = null;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D c) {
            if ( !_body.IsDisposed ) {
                Vector2 position = _body.Position;
                Vector2 displayUnits = new Vector2();
                ConvertUnits.ToDisplayUnits(ref position, out displayUnits);
                spriteBatch.Draw(Image, displayUnits, Color.White);
            }
        }
    }
}


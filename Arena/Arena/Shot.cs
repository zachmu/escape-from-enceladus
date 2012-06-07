using System;
using System.Collections.Generic;
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
using Test;

namespace Arena {
    class Shot {

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
        private Level.Tile _destroyedTile = null;
        private readonly World _world;

        public Shot(Vector2 position, World world, Player.Direction direction) {
            _body = BodyFactory.CreateRectangle(world, .01f, .01f, 1000f);
            _body.IsStatic = false;
            _body.Restitution = .2f;
            _body.Friction = 0f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.IgnoreGravity = true;

            _body.OnCollision += (a, b, contact) => {
                                     _body.IgnoreGravity = false;
                                     _framesToLive = 1;

                                     if ( _destroyedTile == null ) {
                                         FixedArray2<Vector2> points;
                                         Vector2 normal;
                                         contact.GetWorldManifold(out normal, out points);
                                         _destroyedTile = Level.CurrentLevel.GetTile(points[0], normal);
                                         if ( _destroyedTile != null ) {
                                             Console.WriteLine(String.Format("Hit tile at {0}", _destroyedTile.Position));
                                         } else {
                                             Console.WriteLine("Missed a tile.  Collision was {0},{1} with normal {2}",
                                                               points[0], points[1], normal);
                                         }
                                     } else {
                                         Console.WriteLine(String.Format("Second collision at {0}", _destroyedTile.Position));
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

        public void Update() {
            if ( _framesToLive-- == 0 ) {
                _body.Dispose();
            }
            if (_destroyedTile != null) {
                Level.CurrentLevel.DestroyTile(_world, _destroyedTile);
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


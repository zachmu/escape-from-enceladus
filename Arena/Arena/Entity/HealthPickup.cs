using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena;
using Arena.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {
    public class HealthPickup : IGameEntity {

        private static Texture2D[] _animation = new Texture2D[NumFrames];
        private Texture2D Image { get; set; }
        private int _animationFrame = 0;

        private Body _body;

        private const int NumFrames = 6;
        private const float Radius = .25f / 2f;

        public bool Disposed { get; private set; }

        public Vector2 Position {
            get { return _body.Position; }
        }

        public HealthPickup(Vector2 position, World world) {
            _body = BodyFactory.CreateCircle(world, Radius, 0f);
            _body.IsSensor = true;
            _body.IsStatic = true;
            _body.Position = position;
            _body.CollisionCategories = Category.All;
            _body.CollidesWith = Arena.PlayerCategory;
            _body.OnCollision += (a, b, contact) => {
                                     if ( b.Body.GetUserData().IsPlayer ) {
                                         Player.Instance.Pickup(this);
                                         Dispose();
                                     }
                                     return true;
                                 };

            Image = _animation[_animationFrame];
        }

        public PolygonShape Shape {
            get { return null; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is center of sprite
            Vector2 position = _body.Position;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, SolidColorEffect.DisabledColor, 0f, new Vector2(Image.Width / 2, Image.Height / 2),
                             SpriteEffects.None, 0);
        }

        public void Update(GameTime gameTime) {
            _animationFrame = (_animationFrame + 1) % NumFrames;
            Image = _animation[_animationFrame];
        }

        public static void LoadContent(ContentManager cm) {
            for ( int i = 0; i < NumFrames; i++ ) {
                _animation[i] = cm.Load<Texture2D>(String.Format("Pickups/HealthPickup{0:0000}", i));
            }
        }

        public void Dispose() {
            _body.Dispose();
            Disposed = true;
        }
    }
}

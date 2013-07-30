using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// The beam weapon, which burns a continuous path through enemies.
    /// </summary>
    public class Beam : GameEntityAdapter {

        private bool _disposed;
        private static Texture2D _image;
        private Direction _direction;
        private Vector2 _start;
        private float _angle;
        private const int ImageHeight = 16;
        private const int ImageWidth = 16;        

        public override void Dispose() {
            _disposed = true;
        }

        public override bool Disposed {
            get { return _disposed; }
        }

        public static void LoadContent(ContentManager cm) {
            _image = cm.Load<Texture2D>("Projectile/Projectile0002");
        }

        public Beam(World world, Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_start);
            Vector2 origin = new Vector2(0, ImageHeight / 2);
            Rectangle drawRectangle = new Rectangle((int) displayPosition.X,
                                                    (int) (displayPosition.Y - ImageHeight * 10), ImageWidth * 10,
                                                    ImageHeight * 10);

            spriteBatch.Draw(_image, displayPosition, null, SolidColorEffect.DisabledColor,
                             Projectile.GetSpriteRotation(_direction), origin, new Vector2(10f, 1f), SpriteEffects.None, 1.0f);
//            spriteBatch.Draw(_image, displayPosition, null, SolidColorEffect.DisabledColor, Projectile.GetSpriteRotation(_direction), origin,
//                             SpriteEffects.None, 0);
        }

        public override void Update(GameTime gameTime) {
        }
    }

}

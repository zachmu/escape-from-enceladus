using Enceladus.Entity;
using Enceladus.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    public class Missile : Projectile, IGameEntity {

        private const int Speed = 25;
        private const float Width = .5f;
        private const float Height = .25f;
        private static Texture2D Image { get; set; }

        public Missile(Vector2 position, World world, Direction direction)
            : base(position, world, direction, Speed, Width, Height) {
            _body.Rotation = GetSpriteRotation(_direction);
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("Pickups/Missile0000");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_body.IsDisposed ) {
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_body.Position);
                spriteBatch.Draw(Image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width,
                                               Image.Height),
                                 null, SolidColorEffect.DisabledColor * GetAlpha(), _body.Rotation, new Vector2(Image.Width / 2, Image.Height / 2),
                                 SpriteEffects.None, 0);
            }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        public override DestructionFlags DestructionFlags {
            get { return 0; }
        }

        public override float BaseDamage {
            get { return 5; }
        }
    }
}

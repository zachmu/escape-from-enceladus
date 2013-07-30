using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Overlay;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// Basic shot from base weapon
    /// </summary>
    public class Shot : Projectile, IGameEntity {
        private const int Speed = 25;

        private static Texture2D Image { get; set; }       

        // Destruction flags
        public const int Flags = 1;

        public Shot(Vector2 position, World world, Direction direction)
            : base(position, world, direction, Speed, ConvertUnits.ToSimUnits(16), ConvertUnits.ToSimUnits(6)) {
            _body.Rotation = GetSpriteRotation(_direction);
            SoundEffectManager.Instance.PlaySoundEffect("laser");
        }

        public static void LoadContent(ContentManager content) {
            Image = SharedGraphicalAssets.Projectiles[SharedGraphicalAssets.ProjectilePlayerBasic];
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_body.IsDisposed ) {
                Vector2 position = _body.Position;
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                spriteBatch.Draw(Image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                                 null, SolidColorEffect.DisabledColor * GetAlpha(), _body.Rotation,
                                 new Vector2(Image.Width / 2, Image.Height / 2),
                                 SpriteEffects.None, 0);
            }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        public override int DestructionFlags {
            get { return Flags; }
        }

        public override int BaseDamage {
            get { return 1; }
        }
    }
}


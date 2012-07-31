﻿using Arena.Entity;
using Arena.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Weapon {

    /// <summary>
    /// Basic shot from base weapon
    /// </summary>
    public class Shot : Projectile, IGameEntity {
        private const int Speed = 25;

        private static Texture2D Image { get; set; }
        private static SoundEffect Sfx { get; set; }

        // Destruction flags
        public const int Flags = 1;

        public Shot(Vector2 position, World world, Direction direction)
            : base(position, world, direction, Speed, .05f, .05f) {
            Sfx.Play();
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("star");
            Sfx = content.Load<SoundEffect>("Sounds/laser");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_body.IsDisposed ) {
                Vector2 position = _body.Position;
                Vector2 displayUnits = new Vector2();
                ConvertUnits.ToDisplayUnits(ref position, out displayUnits);
                spriteBatch.Draw(Image, displayUnits, SolidColorEffect.DisabledColor);
            }
        }

        public override int DestructionFlags {
            get { return Flags; }
        }

        public override int BaseDamage {
            get { return 1; }
        }
    }
}

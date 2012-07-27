using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {

    /// <summary>
    /// The bomb isn't really a projectile since it just sits there, but it does
    /// everything projectiles do, like hurt enemies and damage terrain.
    /// </summary>
    public class Bomb : Projectile, IGameEntity {

        public const float Width = .375f;
        public static readonly float Height = ConvertUnits.ToSimUnits(26f);
        private const int NumFrames = 12;
        private const int FrameTime = 150;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];

        private int _animationFrame = 0;
        private long _timeSinceLastAnimationUpdate;
        private Texture2D _image;
        private Texture2D Image {
            get { return _image; }
            set {
                _image = value;
                _timeSinceLastAnimationUpdate = 0;
            }
        }

        public const int Flags = 4;

        public Bomb(Vector2 position, World world, Direction direction)
            : base(position, world, direction, 0, Width, Height) {
            Image = Animation[0];
            _timeToLiveMs = FrameTime * (NumFrames + 2);
        }

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Character/Bomb/Bomb{0:0000}", i));
            }
        }

        public override int DestructionFlags {
            get { return Flags; }
        }

        public override int BaseDamage {
            get { return 3; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is center
            Vector2 position = _body.Position;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            if ( _direction == Direction.Left ) {
                displayPosition += new Vector2(12, 0);
            } else {
                displayPosition -= new Vector2(12, 0);                
            }
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, Color.White, 0f, new Vector2(Image.Width / 2, Image.Height / 2),
                             _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        }

        public override void Update(GameTime gameTime) {
            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;
            if ( _timeSinceLastAnimationUpdate > FrameTime ) {
                if (_animationFrame >= NumFrames) {
                    _animationFrame = NumFrames - 1;
                }
                Image = Animation[_animationFrame++];
            }

            base.Update(gameTime);
        }

        protected override OnCollisionEventHandler CollisionHandler() {
            return (a, b, contact) =>
                   false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Util;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity {

    public class HoverBoots : GameEntityAdapter {
        private readonly Timer _timeToLive;

        private static Texture2D[] _animation;
        private const int NumImages = 8;
        private readonly Counter _animationFrame = new Counter(80, NumImages);

        public HoverBoots(double durationMs) {
            _timeToLive = new Timer(durationMs);
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 position = Player.Instance.Position;
            position += new Vector2(0,  Player.Instance.Height / 2);
            Vector2 displayPos = ConvertUnits.ToDisplayUnits(position);

            Vector2 origin = new Vector2(32, 0);
            spriteBatch.Draw(_animation[_animationFrame.StateNumber], displayPos, null, Color.Turquoise, 0f, origin, 1f, SpriteEffects.None, 0f);
        }

        public static void LoadContent(ContentManager cm) {
            _animation = new Texture2D[NumImages];
            for ( int i = 0; i < NumImages; i++ ) {
                _animation[i] = cm.Load<Texture2D>(string.Format("Projectile/HoverBoots{0:0000}", i));
            }
        }

        public override void Update(GameTime gameTime) {
            _animationFrame.Update(gameTime);
            _timeToLive.Update(gameTime);
            if ( _timeToLive.IsTimeUp() ) {
                Dispose();
            }
        }

        public override Vector2 Position {
            get { return Player.Instance.Position; }
        }
    }
}

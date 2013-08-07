using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Entity.NPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {
    
    public class HealthStatus {
        private const int ContainerOffset = 28;
        private const int ContainerCapacity = 100;

        private static readonly Vector2 Location = new Vector2(50, 50);

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            DrawHealthBar(spriteBatch);
            DrawHealthText(spriteBatch);
        }

        private void DrawHealthText(SpriteBatch spriteBatch) {
            Vector2 pos = Location + new Vector2(_outline.Width + 10, 0);
            SpriteFont font = SharedGraphicalAssets.DialogFont;
            String health = String.Format("{0:###}/{1:###}", Player.Instance.Health, Player.Instance.HealthCapacity);
            TextDrawing.DrawStringShadowed(font, spriteBatch, Color.Crimson, health, pos);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch) {
            Vector2 pos = Location;
            spriteBatch.Draw(_outline, pos, Color.White);
            pos += new Vector2(2, 2);

            Rectangle rect = new Rectangle(0, 0, (int) ((Player.Instance.Health / Player.Instance.HealthCapacity) * _bar.Width), _bar.Height);
            spriteBatch.Draw(_bar, pos, rect, Color.White);
        }

        public void LoadContent(ContentManager cm) {
            _outline = cm.Load<Texture2D>("Overlay/HealthBar/Outline0000");
            _bar = cm.Load<Texture2D>("Overlay/HealthBar/Bar0000");
        }

        private Texture2D _outline;
        private Texture2D _bar;
    }
}

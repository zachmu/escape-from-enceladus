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
    
    public class HealthStatus : IOverlayElement {
        private static readonly Vector2 Location = new Vector2(30, 30);

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            DrawBackdrop(spriteBatch);
            DrawHealthBar(spriteBatch);
            DrawHealthText(spriteBatch);
        }

        private void DrawBackdrop(SpriteBatch spriteBatch) {
            int width = 322;
            int height = 44;
            Rectangle src = new Rectangle(0, 0, width, height);
            Rectangle dest = new Rectangle((int) (Location.X - 10), (int) (Location.Y - 10), width, height);
            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop, dest, src, Color.White * .65f);
        }

        private void DrawHealthText(SpriteBatch spriteBatch) {
            Vector2 pos = Location + new Vector2(_outline.Width + 10, -10);
            SpriteFont font = SharedGraphicalAssets.OverlayFont;
            String health = String.Format("{0,3:000}/{1,3:000}", Player.Instance.Health, Player.Instance.HealthCapacity);
            TextDrawing.DrawStringShadowed(font, spriteBatch, Color.Crimson * .65f, health, pos);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch) {
            Vector2 pos = Location;
            spriteBatch.Draw(_outline, pos, Color.White);
            pos += new Vector2(2, 2);

            Rectangle rect = new Rectangle(0, 0, (int) ((Player.Instance.Health / Player.Instance.HealthCapacity) * _bar.Width), _bar.Height);
            spriteBatch.Draw(_bar, pos, rect, Color.White * .65f);
        }

        private float _lastHealth;
        private float _lastCapacity;

        public bool Update(GameTime gameTime) {
            bool needsRedraw = _lastHealth != Player.Instance.Health || _lastCapacity != Player.Instance.HealthCapacity;
            _lastHealth = Player.Instance.Health;
            _lastCapacity = Player.Instance.HealthCapacity;
            return needsRedraw;
        }

        public void LoadContent(ContentManager cm) {
            _outline = cm.Load<Texture2D>("Overlay/HealthBar/Outline0000");
            _bar = cm.Load<Texture2D>("Overlay/HealthBar/Bar0000");
        }

        private Texture2D _outline;
        private Texture2D _bar;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Overlay {
    
    public class HealthStatus {
        private const int ContainerOffset = 28;
        private const int ContainerCapacity = 100;

        private static Vector2 Location = new Vector2(50, 50);

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            DrawContainers(spriteBatch);
            DrawHealthBar(spriteBatch);
        }
        
        private void DrawContainers(SpriteBatch spriteBatch) {
            Vector2 pos = Location;
            for ( int i = 0; i < Player.Instance.HealthCapacity / ContainerCapacity; i++ ) {
                Texture2D image = Player.Instance.Health > ContainerCapacity * (i + 1) ? _fullContainer : _emptyContainer;                
                spriteBatch.Draw(image, pos, Color.White);
                pos += new Vector2(ContainerOffset, 0);
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch) {
            Vector2 pos = Location + new Vector2(0, 16);
            spriteBatch.Draw(_outline, pos, Color.White);
            pos += new Vector2(2, 2);

            Rectangle rect = new Rectangle(0, 0, ((Player.Instance.Health - 1) % ContainerCapacity ) * 2, 8);
            spriteBatch.Draw(_bar, pos, rect, Color.White);
        }

        public void LoadContent(ContentManager cm) {
            _outline = cm.Load<Texture2D>("Overlay/HealthBar/Outline0000");
            _bar = cm.Load<Texture2D>("Overlay/HealthBar/Bar0000");
            _fullContainer = cm.Load<Texture2D>("Overlay/HealthBar/FullContainer0000");
            _emptyContainer = cm.Load<Texture2D>("Overlay/HealthBar/EmptyContainer0000");
        }

        private Texture2D _outline;
        private Texture2D _bar;
        private Texture2D _fullContainer;
        private Texture2D _emptyContainer;
    }
}

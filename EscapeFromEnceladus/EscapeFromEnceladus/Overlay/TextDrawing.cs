using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {
    
    public class TextDrawing {
        public static void DrawStringShadowed(SpriteFont font, SpriteBatch spriteBatch, Color color, string text, Vector2 displayPosition) {
            Color shadow = Color.Lerp(color, Color.Black, .5f);
            spriteBatch.DrawString(font, text, displayPosition + new Vector2(3), shadow);
            spriteBatch.DrawString(font, text, displayPosition, color);
        }
    }
}

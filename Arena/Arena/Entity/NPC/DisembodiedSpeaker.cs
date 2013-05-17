using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.NPC {

    /// <summary>
    /// A speaker who isn't physically present or visible, appearing as a title at the top of the screen
    /// </summary>
    public class DisembodiedSpeaker : IDialogEntity {

        private const int DrawOffsetX = 600;
        private const int DrawOffsetY = 150;

        private readonly Color _color;
        private readonly string _name;

        public DisembodiedSpeaker(Color color, string name) {
            _color = color;
            _name = name;
        }

        public void DrawConversationText(SpriteBatch spriteBatch, Camera2D camera, string text) {
            // Our origin is the top-left corner of the screen, plus some offsets.
            Vector2 cameraOffset = new Vector2(-spriteBatch.GraphicsDevice.Viewport.Bounds.Width / 2f,
                                               -spriteBatch.GraphicsDevice.Viewport.Bounds.Height / 2f);
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(camera.Position) + cameraOffset +
                                      new Vector2(DrawOffsetX, DrawOffsetY);
            NPC.DrawText(spriteBatch, _color, camera, text, displayPosition);
        }

        public void StartConversation() {
        }

        public void StopConversation() {
        }

        public Color Color {
            get { return _color; }
        }

        public string Name {
            get { return _name; }
        }
    }
}

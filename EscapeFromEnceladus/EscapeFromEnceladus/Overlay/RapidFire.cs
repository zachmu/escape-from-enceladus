using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Control;
using Enceladus.Entity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Rapid fire status screen overlay
    /// </summary>
    public class RapidFire {

        private const int Margin = 10;
        private const int TopOffset = 20;
        private const int ImageWidth = 64;
        private const int ImageHeight = 32;
        private const int NumImages = 5;
        private const int BackdopWidth = 2 * Margin + ImageWidth;
        private const int BackdropHeight = 2 * Margin + ImageHeight;

        private int _rapidFireLevel = 0;

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            int width = spriteBatch.GraphicsDevice.Viewport.Width;
            Vector2 topLeftPos = new Vector2(width / 2 - ImageWidth / 2 - Margin - 200, TopOffset);

            DrawBackdrop(topLeftPos, spriteBatch);
            DrawItemSelection(topLeftPos, spriteBatch);
        }

        private void DrawBackdrop(Vector2 topLeftPos, SpriteBatch spriteBatch) {
            Rectangle src = new Rectangle(0, 0, BackdopWidth, BackdropHeight);
            Rectangle dest = new Rectangle((int) topLeftPos.X, (int) topLeftPos.Y, BackdopWidth, BackdropHeight);
            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop, dest, src, Color.White * .65f);
        }

        private void DrawItemSelection(Vector2 topLeftPos, SpriteBatch spriteBatch) {
            Vector2 pos = topLeftPos + new Vector2(Margin);
            spriteBatch.Draw(_images[_rapidFireLevel], pos, Color.White * .65f);
        }

        public static void LoadContent(ContentManager cm) {
            _images = new Texture2D[NumImages];
            for ( int i = 0; i < NumImages; i++ ) {
                _images[i] = cm.Load<Texture2D>(string.Format("Overlay/RapidFire/RapidFire{0:0000}", i));
            }
        }

        public void Update(GameTime gameTime) {
            if ( PlayerControl.Control.IsNewRapidFireDecrease() ) {
                _rapidFireLevel--;
                if ( _rapidFireLevel < 0 ) {
                    _rapidFireLevel = Player.Instance.Equipment.NumSelectableTools - 1;
                }
                Player.Instance.RapidFireSetting = _rapidFireLevel;
            } else if ( PlayerControl.Control.IsNewRapidFireIncrease() ) {
                _rapidFireLevel++;
                if ( _rapidFireLevel >= NumImages ) {
                    _rapidFireLevel = NumImages - 1;
                }
                Player.Instance.RapidFireSetting = _rapidFireLevel;
            }
        }

        private static Texture2D[] _images;
    }

}

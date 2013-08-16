using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Responsible for drawing all overlay elements onto the screen
    /// </summary>
    public class OverlayManager : GameEntityAdapter {
        
        private List<IOverlayElement> _overlayElements = new List<IOverlayElement>();

        public OverlayManager(params IOverlayElement[] elements) {
            _overlayElements.AddRange(elements);
        }

        private RenderTarget2D _screenOverlay;
        private Rectangle _screenRectangle;

        /// <summary>
        /// Draws all overlay elements. 
        /// If the elements haven't changed frame to frame, use last frame's image.
        /// </summary>
        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            spriteBatch.Begin();
            spriteBatch.Draw(_screenOverlay, _screenRectangle, Color.White);
            spriteBatch.End();
        }

        public void PrepareDraw(SpriteBatch spriteBatch) {
            if ( _screenOverlay == null || _needsRedraw ) {
                GraphicsDevice graphics = spriteBatch.GraphicsDevice;
                PresentationParameters pp = graphics.PresentationParameters;
                _screenRectangle = new Rectangle(0, 0,
                                                 pp.BackBufferWidth,
                                                 pp.BackBufferHeight);
                _screenOverlay = new RenderTarget2D(graphics, pp.BackBufferWidth, pp.BackBufferHeight);

                graphics.SetRenderTarget(_screenOverlay);
                graphics.Clear(Color.Transparent);
                spriteBatch.Begin();
                _overlayElements.ForEach(element => element.Draw(spriteBatch));
                spriteBatch.End();
                graphics.SetRenderTarget(null);

                _needsRedraw = false;
            }
        }

        private bool _needsRedraw = false;
        public override void Update(GameTime gameTime) {
            _overlayElements.ForEach(element => _needsRedraw = element.Update(gameTime) || _needsRedraw);
        }
    }
}

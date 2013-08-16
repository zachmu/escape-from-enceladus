using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Overlay element that doesn't need to be redrawn every single frame (usually).
    /// </summary>
    public interface IOverlayElement {

        /// <summary>
        /// Updates this element and returns whether it needs to be redrawn as a result.
        /// </summary>
        bool Update(GameTime gameTime);

        /// <summary>
        /// Draws the overlay onto the screen.
        /// </summary>
        void Draw(SpriteBatch spriteBatch);
    }
}
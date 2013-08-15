using Microsoft.Xna.Framework;

namespace Enceladus.Overlay {

    /// <summary>
    /// Overlay element that doesn't need to be redrawn every single frame (usually).
    /// </summary>
    public interface IOverlayElement {

        /// <summary>
        /// Updates this element and returns whether it needs to be redrawn as a result.
        /// </summary>
        bool Update(GameTime gameTime);
    }
}
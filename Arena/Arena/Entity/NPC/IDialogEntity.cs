using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.NPC {

    /// <summary>
    ///  An entity that knows how to read dialog.
    /// </summary>
    public interface IDialogEntity {

        /// <summary>
        /// Draws the given conversation text for this entity.
        /// </summary>
        void DrawConversationText(SpriteBatch spriteBatch, Camera2D camera, string text);

        /// <summary>
        /// Notifies the receiver that conversation is beginning.
        /// </summary>
        void StartConversation();

        /// <summary>
        /// Notifies the receiver that conversation is ending.
        /// </summary>
        void StopConversation();

        /// <summary>
        /// The color of this dialog
        /// </summary>
        Color Color { get; }
    }
}

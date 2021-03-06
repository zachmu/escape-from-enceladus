﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.NPC {

    /// <summary>
    /// The NPC stand-in for the character
    /// </summary>
    class PlayerDialog : IDialogEntity {

        public void DrawConversationText(SpriteBatch spriteBatch, Camera2D camera, string text) {
            Vector2 position = Player.Instance.Position;
            position.Y -= Player.Instance.Height / 2 + 1.5f;
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            NPC.DrawText(spriteBatch, Player.Instance.Color, camera, text, displayPosition);
        }

        public void StartConversation() {
        }

        public void StopConversation() {
        }

        public Color Color { get { return Player.Instance.Color; } }
        public string Name { get { return "Roark"; } }
    }
}

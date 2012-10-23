using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Entity.NPC {

    /// <summary>
    /// A conversation between one or more characters.
    /// </summary>
    public class Conversation {

        private List<String> _lines = new List<string>();
        private List<IDialogEntity> _characters = new List<IDialogEntity>();

        private int _currentLine = 0;

        public Conversation(ContentManager content, String conversationName) {
            string file = Path.Combine(content.RootDirectory, Path.Combine("Conversations", conversationName));
            foreach ( String line in File.ReadLines(file) ) {
                int split = line.IndexOf(":");
                String character = line.Substring(0, split);
                String speech = line.Substring(split + 1);
                _characters.Add(NPCFactory.Get(character));
                _lines.Add(speech);
            }
        }

        /// <summary>
        /// Draws this conversation as an overlay onto the screen.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            IDialogEntity speakingCharacter = _characters[_currentLine];
            string text = _lines[_currentLine];
            speakingCharacter.DrawConversationText(spriteBatch, camera, text);
        }

        /// <summary>
        /// Notifies the speakers involved that the conversation has begun.
        /// </summary>
        internal void NotifySpeakersConversationStarted() {
            _characters.ForEach(npc => npc.StartConversation());
        }

        /// <summary>
        /// Updates the conversation with a button press.
        /// </summary>
        public void Update(GameTime gameTime) {
            if ( new Buttons[] { Buttons.A, Buttons.B, Buttons.Y, Buttons.X }.ToList().Any(
                button => InputHelper.Instance.IsNewButtonPress(button)) ) {
                _currentLine++;
                if ( _currentLine >= _lines.Count ) {
                    Arena.Instance.EndConversation();
                    _characters.ForEach(npc => npc.StopConversation());
                }
            }
        }
    }
}

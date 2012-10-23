using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace Arena.Entity.NPC {
    /// <summary>
    /// Manages conversations initiated by talking to an NPC or various events, 
    /// creating and distributing them as needed.
    /// </summary>
    public class ConversationManager {

        private readonly ContentManager _cm;

        private static ConversationManager _instance;
        public static ConversationManager Instance {
            get { return _instance; }
        }

        /// <summary>
        /// Creates a new conversation manager.
        /// </summary>
        public ConversationManager(ContentManager cm) {
            _cm = cm;
            _instance = this;
        }

        /// <summary>
        /// Returns the appropriate conversation to initiate, at this point of the game, with the NPC given.
        /// </summary>
        public void StartConversation(NPC initiatingNPC) {
            // For now, we just return the one, static conversation for each character.
            Conversation conversation = new Conversation(_cm, String.Format("{0}-default.txt", initiatingNPC.Name));
            conversation.NotifySpeakersConversationStarted();
            Arena.Instance.ConversationStarted(conversation);
        }
    }
}

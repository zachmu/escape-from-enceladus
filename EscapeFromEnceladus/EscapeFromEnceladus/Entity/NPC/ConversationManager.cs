using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;
using Microsoft.Xna.Framework.Content;

namespace Enceladus.Entity.NPC {
    /// <summary>
    /// Manages conversations initiated by talking to an NPC or various events, 
    /// creating and distributing them as needed.
    /// </summary>
    public class ConversationManager {

        private readonly ContentManager _cm;

        public event ConversationStartedHandle ConversationStarted;
        public event ConversationEndedHandle ConversationEnded;

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
            switch ( initiatingNPC.Name ) {
                case NPCFactory.CharCaptainPurchase:
                    StartConversationWithCaptainPurchase();
                    break;
                case NPCFactory.CharChefHawser:
                    StartConversationWithChefHawser();
                    break;
                case NPCFactory.CharChiefMizzen:
                    StartConversationWithChiefMizzen();
                    break;
                case NPCFactory.CharCommanderTaffrail:
                    StartConversationWithCommanderTaffrail();
                    break;
                case NPCFactory.CharEnsignGibe:
                    StartConversationWithEnsignGibe();
                    break;
                case NPCFactory.CharEnsignLeeward:
                    StartConversationWithEnsignLeeward();
                    break;
                case NPCFactory.CharLieutenantForecastle:
                    StartConversationWithLieutenantForecastle();
                    break;
                case NPCFactory.CharProfessorIapetus:
                    StartConversationWithProfessorIapetus();
                    break;
            }
        }

        private void StartConversationWithProfessorIapetus() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/ProfessorIapetusFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/ProfessorIapetusSecond.txt"));
            }
        }

        private void StartConversationWithLieutenantForecastle() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/LieutenantForecastleFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/LieutenantForecastleSecond.txt"));
            }
        }

        private void StartConversationWithEnsignLeeward() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/CommanderTaffrailFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/EnsignLeewardSecond.txt"));
            }
        }

        private void StartConversationWithEnsignGibe() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/EnsignGibeFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/EnsignGibeSecond.txt"));
            }
        }

        private void StartConversationWithChiefMizzen() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/ChiefMizzenFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/ChiefMizzenSecond.txt"));
            }
        }

        private void StartConversationWithCommanderTaffrail() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/CommanderTaffrailFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/CommanderTaffrailSecond.txt"));
            }
        }

        private void StartConversationWithChefHawser() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/ChefHawserFirst.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/ChefHawserFirst.txt"));
            }
        }

        private void StartConversationWithCaptainPurchase() {
            if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToCaptain) ) {
                StartConversation(new Conversation(_cm, "Prologue/CaptainPurchaseFirst.txt"));
            } else if ( !GameState.HasMilestoneOccurred(GameMilestone.TalkedToEveryone) ) {
                StartConversation(new Conversation(_cm, "Prologue/CaptainPurchaseSecond.txt"));
            } else {
                StartConversation(new Conversation(_cm, "Prologue/CaptainPurchaseThird.txt"));
            }
        }

        /// <summary>
        /// Does the necessary bookkeeping to notify all interested parties that a conversation has started
        /// </summary>
        private void StartConversation(Conversation conversation) {
            conversation.NotifySpeakersConversationStarted();
            ConversationStarted(conversation);
        }

        /// <summary>
        /// Starts the conversation with the name given.
        /// </summary>
        public void StartConversation(string conversationName) {
            StartConversation(new Conversation(_cm, conversationName));
        }

        public void EndConversation(Conversation conversation) {
            ConversationEnded(conversation);
        }
    }

    public delegate void ConversationEndedHandle(Conversation conversation);
    public delegate void ConversationStartedHandle(Conversation conversation);
}

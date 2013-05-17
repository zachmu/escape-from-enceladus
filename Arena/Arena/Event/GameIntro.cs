using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity.NPC;

namespace Arena.Event {

    /// <summary>
    /// An event for the introduction to the game
    /// </summary>
    class GameIntro : GameEvent {
        public const string ID = "GameIntro";

        public override void ConversationStarted(Conversation conversation) {
        }

        public override void Update() {

        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            ConversationManager.Instance.StartConversation("InitialCaptainAnnouncement.txt");
            GameState.MilestoneAcheived(GameMilestone.GameStarted);
            EventManager.Instance.LoadEvent(TalkToCaptain.ID);
            base.Apply();
        }
    }

    /// <summary>
    /// After the captain's announcement on the loudspeaker, you should go talk to him.
    /// </summary>
    class TalkToCaptain : GameEvent {
        public const string ID = "TalkToCaptain";

        public override void ConversationStarted(Conversation conversation) {
            if ( conversation.Participants.Any(entity => entity.Name == NPCFactory.CharCaptainPurchase) ) {
                Apply();
            }
        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            ConversationManager.Instance.StartConversation("InitialCaptainConversation.txt");
            GameState.MilestoneAcheived(GameMilestone.TalkedToCaptain);
            EventManager.Instance.LoadEvent(Prologue.ID);
            base.Apply();
        }

    }
}

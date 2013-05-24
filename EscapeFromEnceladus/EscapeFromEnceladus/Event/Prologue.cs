using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity.NPC;
using Arena.Map;

namespace Arena.Event {

    /// <summary>
    /// Talk to everyone on the ship to trigger this event.
    /// </summary>
    internal class Prologue : GameEvent {
        public const string ID = "PrologueOnShip";

        private readonly HashSet<string> _toTalkTo = new HashSet<string>();

        public Prologue() {
            _toTalkTo.Add(NPCFactory.CharChefHawser);
            _toTalkTo.Add(NPCFactory.CharChiefMizzen);
            _toTalkTo.Add(NPCFactory.CharLieutenantForecastle);
            _toTalkTo.Add(NPCFactory.CharEnsignGibe);
            _toTalkTo.Add(NPCFactory.CharEnsignLeeward);
            _toTalkTo.Add(NPCFactory.CharCommanderTaffrail);
            _toTalkTo.Add(NPCFactory.CharProfessorIapetus);
        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            EventManager.Instance.LoadEvent(IapetusEscapes.ID);
            GameState.MilestoneAcheived(GameMilestone.TalkedToEveryone);
            base.Apply();
        }

        public override void ConversationOver(Conversation conversation) {
            conversation.Participants.ForEach(entity => _toTalkTo.Remove(entity.Name));
            if ( _toTalkTo.Count == 0 )
                Apply();
        }
    }

    /// <summary>
    /// Talk to the captain to trigger this event. Iapetus escapes!
    /// </summary>
    internal class IapetusEscapes : GameEvent {
        public static string ID = "IapetusEscapes";

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            EventManager.Instance.LoadEvent(Embarking.ID);
            ConversationManager.Instance.StartConversation("Prologue/IapetusEscapes.txt");
            base.Apply();
        }

        public override void ConversationOver(Conversation conversation) {
            if ( conversation.Participants.Any(entity => entity.Name == NPCFactory.CharCaptainPurchase) ) {
                Apply();
            }
        }
    }

    /// <summary>
    /// An auto-event, which will trigger naturally because of IapetusEscapes
    /// </summary>
    internal class Embarking : GameEvent {
        public static string ID = "Embarking";

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            ConversationManager.Instance.StartConversation("Prologue/Embarking.txt");
            TileLevel.CurrentLevel.DoorNamed("shipDoor").Unlock();
            GameState.MilestoneAcheived(GameMilestone.Embarked);
            base.Apply();
        }

        public override void ConversationOver(Conversation conversation) {
            if ( conversation.Name == "Prologue/IapetusEscapes.txt" ) {
                Apply();
            }
        }
    }
}

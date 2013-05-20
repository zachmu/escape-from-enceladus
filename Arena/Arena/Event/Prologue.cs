using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity.NPC;

namespace Arena.Event {
    internal class Prologue : GameEvent {
        public const string ID = "PrologueOnShip";

        private readonly HashSet<IDialogEntity> _toTalkTo = new HashSet<IDialogEntity>();

        public Prologue() {
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.CaptainPurchase));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.ChefHawser));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.ChiefMizzen));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.EnsignClew));
            _toTalkTo.Add(NPCFactory.GetDialogEntity(NPCFactory.CharLieutenantForecastle));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.EnsignGibe));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.EnsignLeeward));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.EnsignTaffrail));
//            _toTalkTo.Add(NPCFactory.Get(NPCFactory.ProfessorIapetus));
        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {

        }

        public override void ConversationStarted(Conversation conversation) {
            List<IDialogEntity> speakers = conversation.Participants;
            _toTalkTo.RemoveWhere(speakers.Contains);
            if ( _toTalkTo.Count == 0 )
                Apply();
        }
    }
}

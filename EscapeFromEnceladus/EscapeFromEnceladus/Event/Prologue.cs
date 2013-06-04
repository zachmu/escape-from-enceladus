using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.NPC;
using Enceladus.Map;
using Newtonsoft.Json;

namespace Enceladus.Event {

    /// <summary>
    /// Talk to everyone on the ship to trigger this event.
    /// </summary>
    internal class Prologue : GameEvent {
        public const string ID = "PrologueOnShip";

        [JsonProperty(PropertyName = "toTalkTo")]
// ReSharper disable FieldCanBeMadeReadOnly.Local
        private HashSet<string> _toTalkTo = new HashSet<string>();
// ReSharper restore FieldCanBeMadeReadOnly.Local

        public Prologue() {            
        }

        /// <summary>
        /// The param is used to differentiate this from the nullary constructor, 
        /// used by save-state persistence.
        /// </summary>
        public Prologue(EventManager parent) {
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
            GameMilestones.Instance.MilestoneAcheived(GameMilestone.TalkedToEveryone);
            base.Apply();
        }

        public override void ConversationOver(Conversation conversation) {
            conversation.Participants.ForEach(entity => _toTalkTo.Remove(entity.Name));
            if ( _toTalkTo.Count == 0 )
                Apply();
        }

        public override void LoadFromSave(SaveState save) {
            Prologue savedCopy = save.ActiveEvents.First(@event => @event.Id == this.Id) as Prologue;
            if ( savedCopy != null ) {
                _toTalkTo.Clear();
                _toTalkTo.UnionWith(savedCopy._toTalkTo);
            }
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
            MusicManager.Instance.SetMusicTrack("disaster");
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
            MusicManager.Instance.ResumePrevTrack();
            TileLevel.CurrentLevel.DoorNamed("shipDoor").Unlock();
            GameMilestones.Instance.MilestoneAcheived(GameMilestone.Embarked);
            base.Apply();
        }

        public override void ConversationOver(Conversation conversation) {
            if ( conversation.Name == "Prologue/IapetusEscapes.txt" ) {
                Apply();
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.NPC;
using Enceladus.Map;
using Microsoft.Xna.Framework;

namespace Enceladus.Event {

    /// <summary>
    /// An event for the introduction to the game
    /// </summary>
    class GameIntro : GameEvent {
        public const string ID = "GameIntro";

        private const int MillisecondsBeforeTrigger = 3000;
        private double MsLeftUntilTrigger = MillisecondsBeforeTrigger; 

        public override void ConversationOver(Conversation conversation) {
        }

        public override void Update(GameTime gameTime) {
            MsLeftUntilTrigger -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( MsLeftUntilTrigger <= 0 ) {
                Apply();
            }
        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            TileLevel.CurrentLevel.DoorNamed("shipDoor").Lock();

            ConversationManager.Instance.StartConversation("Prologue/InitialCaptainAnnouncement.txt");
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

        public override void ConversationOver(Conversation conversation) {
            if ( conversation.Name == "Prologue/CaptainPurchaseFirst.txt" ) {
                Apply();
            }
        }

        public override string Id {
            get { return ID; }
        }

        public override void Apply() {
            GameState.MilestoneAcheived(GameMilestone.TalkedToCaptain);
            EventManager.Instance.LoadEvent(Prologue.ID);
            base.Apply();
        }

    }
}

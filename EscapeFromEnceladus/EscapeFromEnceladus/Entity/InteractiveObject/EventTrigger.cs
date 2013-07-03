using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.NPC;
using Enceladus.Map;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.InteractiveObject {
    public abstract class EventTrigger : GameEntityAdapter {
        private bool disposed = false;
        protected Region eventTriggerRegion;
        protected Body eventTriggerSensor;

        public override void Dispose() {
            eventTriggerSensor.Dispose();
        }

        public override bool Disposed {
            get { return eventTriggerSensor.IsDisposed; }
        }

        internal EventTrigger(Vector2 topLeft, Vector2 bottomRight, World world) {
            eventTriggerRegion = new Region(topLeft, bottomRight);
            eventTriggerSensor = eventTriggerRegion.CreatePlayerSensor(world);

            eventTriggerSensor.OnCollision += (a, b, contact) => {
                Trigger();
                return true;
            };
        }

        protected abstract void Trigger();
    }

    public class RobotSentry : EventTrigger {
        public RobotSentry(Vector2 topLeft, Vector2 bottomRight, World world) : base(topLeft, bottomRight, world) {
        }

        protected override void Trigger() {
            MusicManager.Instance.SetMusicTrack("disaster");
            ConversationManager.Instance.StartConversation("MainQuest/RobotSentry.txt");
            Dispose();
        }
    }

}

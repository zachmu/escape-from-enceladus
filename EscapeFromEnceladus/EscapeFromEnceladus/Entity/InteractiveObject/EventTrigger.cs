using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.Enemy;
using Enceladus.Entity.NPC;
using Enceladus.Event;
using Enceladus.Map;
using Enceladus.Util;
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
            EventManager.Instance.LoadEvent(new RobotSentryDefeated());
            TileLevel.CurrentLevel.DoorNamed("RobotSentryExit").Lock();
            TileLevel.CurrentLevel.DoorNamed("LabDoor").Lock();
        }

        /// <summary>
        /// Event to monitor for when the robot sentry is defeated
        /// </summary>
        class RobotSentryDefeated : GameEvent {
            public override string Id {
                get { return "RobotSentryDefeated"; }
            }

            private int _numTurrets = 8;
            private bool _delayActivated;
            private readonly Timer _countdown = new Timer(500);

            public override void EnemyRemoved(IEnemy enemy) {
                if ( enemy is Turret ) {
                    if ( --_numTurrets <= 0 ) {
                        _delayActivated = true;
                    }
                }
            }

            // Once the event is triggered, wait 500 ms to take action
            public override void Update(GameTime gameTime) {
                if ( _delayActivated ) {
                    _countdown.Update(gameTime);
                    if ( _countdown.IsTimeUp() ) {
                        Apply();
                    }
                }
            }

            public override void Apply() {
                GameMilestones.Instance.MilestoneAcheived(GameMilestone.DefeatedRobotSentry);
                MusicManager.Instance.ResumePrevTrack();
                ConversationManager.Instance.StartConversation("MainQuest/RobotSentryDefeated.txt");
                TileLevel.CurrentLevel.DoorNamed("RobotSentryExit").Unlock();
                TileLevel.CurrentLevel.DoorNamed("LabDoor").Unlock();
                base.Apply();
            }
        }
    }
}

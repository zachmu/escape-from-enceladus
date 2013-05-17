using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity.NPC;

namespace Arena.Event {

    /// <summary>
    /// An in-game event, which must have certain circumstances met to occur
    /// </summary>
    interface IGameEvent {

        event EventTriggeredHandler Triggered;

        /// <summary>
        /// The unique id of this event
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Apply this event to the game world
        /// </summary>
        void Apply();

        /// <summary>
        /// Update the status of this event using global game state, triggering if necessary.
        /// </summary>
        void Update();

        /// <summary>
        /// Notifies this event that the given conversation is taking place
        /// </summary>
        /// <param name="conversation"></param>
        void ConversationStarted(Conversation conversation);

        /// <summary>
        /// Whether to remove this event when it's triggered
        /// </summary>
        /// <returns></returns>
        bool IsRemoveOnTrigger();
    }

    abstract class GameEvent : IGameEvent {
        
        public event EventTriggeredHandler Triggered;
        public abstract string Id { get; }

        public virtual void Apply() {
            Triggered(this);
        }

        public virtual void Update() {            
        }

        public virtual void ConversationStarted(Conversation conversation) {            
        }

        public bool IsRemoveOnTrigger() {
            return true;
        }
    }

    delegate void EventTriggeredHandler(IGameEvent sender);
}

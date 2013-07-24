using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.Enemy;
using Enceladus.Entity.NPC;
using Microsoft.Xna.Framework;

namespace Enceladus.Event {

    /// <summary>
    /// An in-game event, which must have certain circumstances met to occur
    /// </summary>
    public interface IGameEvent : ISaveable {

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
        /// <param name="gameTime"></param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Notifies this event that the given conversation just happened.
        /// </summary>
        /// <param name="conversation"></param>
        void ConversationOver(Conversation conversation);

        /// <summary>
        /// Notifies this event that the given enemy was just added to the simulation.
        /// </summary>
        /// <param name="enemy"></param>
        void EnemyAdded(IEnemy enemy);

        /// <summary>
        /// Notifies this event that the given enemy was just removed from the simulation.
        /// </summary>
        /// <param name="enemy"></param>
        void EnemyRemoved(IEnemy enemy);

        /// <summary>
        /// Whether to remove this event when it's triggered
        /// </summary>
        /// <returns></returns>
        bool IsRemoveOnTrigger();
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class GameEvent : IGameEvent {
        
        public event EventTriggeredHandler Triggered;
        public abstract string Id { get; }

        public virtual void Apply() {
            Triggered(this);
        }

        public virtual void Update(GameTime gameTime) {            
        }

        public virtual void ConversationOver(Conversation conversation) {
        }

        public virtual bool IsRemoveOnTrigger() {
            return true;
        }

        public virtual void EnemyAdded(IEnemy enemy) {
        }

        public virtual void EnemyRemoved(IEnemy enemy) {
        }

        public virtual void Save(SaveState save) {
        }

        public virtual void LoadFromSave(SaveState save) {
        }
    }

    public delegate void EventTriggeredHandler(IGameEvent sender);
}

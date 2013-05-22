using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity.NPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Arena.Event {

    /// <summary>
    /// Responsible for maintaining global state about game-progress events
    /// </summary>
    public class EventManager {

        private static EventManager _instance;

        public static EventManager Instance {
            get { return _instance; }
        }

        public EventManager() {
            _instance = this;
        }

        private readonly Dictionary<string, IGameEvent> _events = new Dictionary<string, IGameEvent>();
        private readonly List<IGameEvent> _activeEvents = new List<IGameEvent>();

        /// <summary>
        /// Loads every possible event, along with any persisted state they might have. 
        /// Events aren't actively listening for changes until loaded via LoadEvent()
        /// </summary>
        /// <param name="contentManager"></param>
        public void LoadContent(ContentManager contentManager) {
            IGameEvent[] allEvents = new IGameEvent[] { 
                new GameIntro(),
                new TalkToCaptain(),
                new Prologue(),
                new IapetusEscapes(), 
                new Embarking(), 
            };

            allEvents.ToList().ForEach(@event => _events[@event.Id] = @event);
            Init();
        }

        /// <summary>
        /// Initialize the state of the event space based on persisted data.
        /// </summary>
        private void Init() {
            LoadEvent(GameIntro.ID);
        }

        /// <summary>
        /// Triggers the event with the ID given, which must be active
        /// </summary>
        /// <param name="eventId"></param>
        public void TriggerEvent(string eventId) {
            _activeEvents.First(@event => @event.Id == eventId).Apply();
        }

        /// <summary>
        /// Loads the event with the ID given and makes it receive notifications about in-game actions.
        /// </summary>
        /// <param name="eventId"></param>
        public void LoadEvent(string eventId) {
            IGameEvent gameEvent = _events[eventId];
            if ( gameEvent.IsRemoveOnTrigger() ) {
                gameEvent.Triggered += GameEventOnTriggered;
            }
            _activeEvents.Add(gameEvent);
        }

        /// <summary>
        /// Delegate method called when an event is triggered to notify us it has happened.
        /// </summary>
        /// <param name="event"></param>
        private void GameEventOnTriggered(IGameEvent @event) {            
            UnloadEvent(@event.Id);
        }

        /// <summary>
        /// Unloads the event with the ID given. It will no longer receive notifications about in-game actions.
        /// </summary>
        /// <param name="eventId"></param>
        public void UnloadEvent(string eventId) {
            _activeEvents.RemoveAll(@event => @event.Id == eventId);
            _events[eventId].Triggered -= GameEventOnTriggered;
        }

        public void NotifyConversationOver(Conversation conversation) {
            _activeEvents.ForEach(@event => @event.ConversationOver(conversation));
        }

        /// <summary>
        /// Updates all active events. Called when some event takes place, such as 
        /// collecting an important item, that might cause another event to be triggered.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime) {
            _activeEvents.ForEach(@event => @event.Update(gameTime));
        }
    }

    /// <summary>
    /// Class to store whether particular events have occurred or not, 
    /// as a shorthand to needing to keep all events and their status around all the time.
    /// </summary>
    public class GameState {

        private static readonly HashSet<GameMilestone> _milestones = new HashSet<GameMilestone>();

        public static bool HasMilestoneOccurred(GameMilestone milestone) {
            return _milestones.Contains(milestone);
        }

        public static void MilestoneAcheived(GameMilestone milestone) {
            _milestones.Add(milestone);
        }
    }

    public enum GameMilestone {GameStarted, TalkedToCaptain, TalkedToEveryone, Embarked}
}

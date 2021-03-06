﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.Enemy;
using Enceladus.Entity.NPC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Enceladus.Event {

    /// <summary>
    /// Responsible for maintaining global state about game-progress events
    /// </summary>
    public class EventManager : ISaveable {

        public static EventManager Instance { get; private set; }

        public EventManager() {
            Instance = this;
        }

        private readonly Dictionary<string, IGameEvent> _events = new Dictionary<string, IGameEvent>();
        private readonly List<IGameEvent> _activeEvents = new List<IGameEvent>();

        /// <summary>
        /// Loads every possible event, along with any persisted state they might have. 
        /// Events aren't actively listening for changes until loaded via LoadEvent()
        /// </summary>
        public void LoadContent(ContentManager contentManager) {
            IGameEvent[] allEvents = new IGameEvent[] { 
                new GameIntro(),
                new TalkToCaptain(),
                new Prologue(this),
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
            ConversationManager.Instance.ConversationEnded += NotifyConversationOver;
            EnemyMonitor.Instance.OnEnemyAdded += NotifyEnemyAdded;
            EnemyMonitor.Instance.OnEnemyRemoved += NotifyEnemyRemoved;
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
        public IGameEvent LoadEvent(string eventId) {
            IGameEvent gameEvent = _events[eventId];
            return LoadEvent(gameEvent);
        }

        /// <summary>
        /// Loads the event given and makes it receive notifications about in-game actions.
        /// </summary>
        public IGameEvent LoadEvent(IGameEvent gameEvent) {
            if ( gameEvent.IsRemoveOnTrigger() ) {
                gameEvent.Triggered += GameEventOnTriggered;
            }
            _activeEvents.Add(gameEvent);
            return gameEvent;
        }

        /// <summary>
        /// Delegate method called when an event is triggered to notify us it has happened.
        /// </summary>        
        private void GameEventOnTriggered(IGameEvent @event) {
            if ( @event.IsRemoveOnTrigger() ) {
                UnloadEvent(@event);
            }
        }

        /// <summary>
        /// Unloads the event with the ID given. 
        /// It will no longer receive notifications about in-game actions.
        /// </summary>
        /// <param name="eventId"></param>
        public void UnloadEvent(string eventId) {
            IGameEvent gameEvent = _events[eventId];
            UnloadEvent(gameEvent);
        }

        /// <summary>
        /// Unloads the event given. 
        /// It will no longer receive notifications about in-game actions.
        /// </summary>
        public void UnloadEvent(IGameEvent gameEvent) {
            gameEvent.Triggered -= GameEventOnTriggered;
            _activeEvents.RemoveAll(@event => @event.Id == gameEvent.Id);
        }

        private void NotifyConversationOver(Conversation conversation) {
            _activeEvents.ForEach(@event => @event.ConversationOver(conversation));
        }

        private void NotifyEnemyAdded(IEnemy enemy) {
            _activeEvents.ForEach(@event => @event.EnemyAdded(enemy));
        }

        private void NotifyEnemyRemoved(IEnemy enemy) {
            _activeEvents.ForEach(@event => @event.EnemyRemoved(enemy));
        }

        /// <summary>
        /// Updates all active events.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime) {
            _activeEvents.ForEach(@event => @event.Update(gameTime));
        }

        public void Save(SaveState save) {
            save.ActiveEvents = new List<IGameEvent>();
            save.ActiveEvents.AddRange(_activeEvents);
        }

        public void LoadFromSave(SaveState save) {
            _activeEvents.Clear();
            foreach ( var gameEvent in save.ActiveEvents ) {
                LoadEvent(gameEvent.Id).LoadFromSave(save);
            }
        }
    }

    /// <summary>
    /// Class to store whether particular events have occurred or not, 
    /// as a shorthand to needing to keep all events and their status around all the time.
    /// </summary>
    public class GameMilestones  : ISaveable {

        private static readonly GameMilestones _instance = new GameMilestones();
        public static GameMilestones Instance {
            get { return _instance; }
        }

        private GameMilestones() {
        }

        private HashSet<GameMilestone> _milestones = new HashSet<GameMilestone>();

        public bool HasMilestoneOccurred(GameMilestone milestone) {
            return _milestones.Contains(milestone);
        }

        public void MilestoneAcheived(GameMilestone milestone) {
            _milestones.Add(milestone);
        }

        public void Save(SaveState save) {
            save.Milestones = _milestones;
        }

        public void LoadFromSave(SaveState save) {
            _milestones = save.Milestones;
        }
    }

    public enum GameMilestone {GameStarted, TalkedToCaptain, TalkedToEveryone, Embarked, DefeatedRobotSentry}
}

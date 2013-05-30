using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;
using Microsoft.Xna.Framework;

namespace Enceladus.Map {

    /// <summary>
    /// The collection state for all unique items in the game.
    /// </summary>
    public class ItemCollectionState : ISaveable {

        private static readonly ItemCollectionState _instance = new ItemCollectionState();

        public static ItemCollectionState Instance {
            get { return _instance; }
        }

        private ItemCollectionState() {
        }

        private readonly HashSet<Vector2> _collectedLocations = new HashSet<Vector2>();

        /// <summary>
        /// Returns whether the item with the given location has been collected. 
        /// Since all item coordinates are unique, this is good enough.
        /// </summary>
        public bool IsItemCollected(Vector2 location) {
            return _collectedLocations.Contains(location);
        }

        /// <summary>
        /// Marks the item with the given location as having been collected.
        /// </summary>
        public void Collected(Vector2 location) {
            _collectedLocations.Add(location);
        }

        public void Save(SaveState save) {
            save.CollectedItems = _collectedLocations;
        }

        public void LoadFromSave(SaveState save) {
            _collectedLocations.Clear();
            _collectedLocations.UnionWith(save.CollectedItems);
        }
    }
}

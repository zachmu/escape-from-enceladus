using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity.InteractiveObject;
using Enceladus.Event;
using Enceladus.Map;

namespace Enceladus.Entity {
    
    /// <summary>
    /// All the player's current equipment
    /// </summary>
    public class Equipment : ISaveable {

        private readonly HashSet<CollectibleItem> _CollectibleItems = new HashSet<CollectibleItem>();

        public Equipment() {
            _CollectibleItems.Add(CollectibleItem.BasicGun);
        }

        /// <summary>
        /// Returns whether the player has collected the given CollectibleItem
        /// </summary>
        public bool HasCollectibleItem(CollectibleItem collectibleItem) {
            return _CollectibleItems.Contains(collectibleItem);
        }

        /// <summary>
        /// Add the given CollectibleItem to the set the player has collected. 
        /// Some CollectibleItems can be collected more than once.
        /// </summary>
        public void Collected(CollectibleItem collectibleItem) {
            _CollectibleItems.Add(collectibleItem);
        }

        public void Save(SaveState save) {
            save.Equipment = this;
        }

        public int NumSelectableTools {
            get { return 2; }
        }

        public void LoadFromSave(SaveState save) {
            _CollectibleItems.Clear();
            _CollectibleItems.UnionWith(save.Equipment._CollectibleItems);
        }
    }
}

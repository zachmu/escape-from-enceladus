using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;
using Newtonsoft.Json;

namespace Enceladus.Map {
    
    /// <summary>
    /// Persistent store of which doors are locked, etc.
    /// </summary>
    public class DoorState : ISaveable {

        private static readonly DoorState _instance = new DoorState();

        public static DoorState Instance {
            get { return _instance; }
        }

        private readonly HashSet<string> _lockedDoors = new HashSet<string>();

        private DoorState() {
        }

        /// <summary>
        /// Notifies the state mechanism that the door with the given name is locked.
        /// </summary>
        /// <param name="doorName"></param>
        internal void DoorLocked(string doorName) {
            if ( doorName == null )
                return;
            _lockedDoors.Add(doorName);
        } 

        /// <summary>
        /// Notifies the state mechanism that the door with the given name is unlocked.
        /// </summary>
        /// <param name="doorName"></param>
        internal void DoorUnlocked(string doorName) {
            if ( doorName == null )
                return;
            _lockedDoors.Remove(doorName);
        }

        public void Save(SaveState save) {
            save.LockedDoors = _lockedDoors;
        }

        public void LoadFromSave(SaveState save) {
           _lockedDoors.Clear();
           _lockedDoors.UnionWith(save.LockedDoors);
        }
    }
}

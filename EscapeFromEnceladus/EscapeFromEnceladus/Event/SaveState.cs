using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Enceladus.Entity;
using Enceladus.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
using Newtonsoft.Json;

namespace Enceladus.Event {
    /// <summary>
    /// Encapsulates everything about a save state.
    /// </summary>
    public class SaveState {

        /*
         * Storage fields for game state
         */
        public Vector2 SaveStationLocation;
        public HashSet<string> LockedDoors; 
        public List<int> VisitedScreensX;
        public List<int> VisitedScreensY;
        public List<int> KnownScreensX;
        public List<int> KnownScreensY;
        public List<IGameEvent> ActiveEvents;
        public HashSet<GameMilestone> Milestones;
        public DateTime? SaveTime = null;
        public Equipment Equipment;
        public float PlayerHealth;
        public float? PlayerHealthCapacity;
        public HashSet<Vector2> CollectedItems; 

        /*
         * Other fields
         */
        private SaveWaiter _future;
        public PlayerIndex Slot { get; private set; }

        private static StorageDevice _device;
        private static readonly object Lock = new object();

        static SaveState() {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
            JsonConvert.DefaultSettings = () => jsonSerializerSettings;
        }

        /// <summary>
        /// Parameterless constructor only for persistence.
        /// </summary>
        public SaveState() {
        }

        /// <summary>
        /// Creates a blank save state with the given slot. 
        /// Valid only for loading a save.
        /// </summary>
        public SaveState(PlayerIndex slot) {
            Slot = slot;
        }

        /// <summary>
        /// Creates a new save state using the current game state.
        /// </summary>
        public SaveState(PlayerIndex slot, VisitationMap map) {
            Slot = slot;
            SaveTime = DateTime.Now;

            new ISaveable[] {
                GameMilestones.Instance,
                map, 
                Player.Instance,
                DoorState.Instance,
                ItemCollectionState.Instance,
                EventManager.Instance
            }.ToList().ForEach(saveable => saveable.Save(this));
        }

        /// <summary>
        /// Populates the game state with the state from this save.
        /// </summary>
        public void ApplyToGameState(VisitationMap map) {

            new ISaveable[] {
                GameMilestones.Instance,
                map, 
                Player.Instance,
                DoorState.Instance,
                ItemCollectionState.Instance,
                EventManager.Instance
            }.ToList().ForEach(saveable => saveable.LoadFromSave(this));
        }

        /// <summary>
        /// Persists this save state to the storage container, 
        /// returning a handle to wait for this async operation.
        /// </summary>
        public SaveWaiter Persist() {
            _future = new SaveWaiter { SaveState = this };
            new Thread(AsyncPersist).Start();
            return _future;
        }

        /// <summary>
        /// Loads this save state from disk, returning a future with 
        /// the result of the load and wait handle.
        /// </summary>
        public SaveWaiter Load() {
            _future = new SaveWaiter();
            new Thread(AsyncLoad).Start();
            return _future;
        }

        private string SaveFileName() {
            return "savegame" + Slot + ".sav";
        }

        private void AsyncPersist() {
            GetDevice();

            // Open a storage container
            IAsyncResult openContainerResult =
                _device.BeginOpenContainer("Escape from Enceladus", null, null);
            openContainerResult.AsyncWaitHandle.WaitOne();
            StorageContainer container = _device.EndOpenContainer(openContainerResult);
            openContainerResult.AsyncWaitHandle.Close();

            string filename = SaveFileName();
            // Check to see whether the save exists.
            if ( container.FileExists(filename) )
                // Delete it so that we can create one fresh.
                container.DeleteFile(filename);

            String json = JsonConvert.SerializeObject(this);
           
            Stream stream = container.CreateFile(filename);
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            writer.Close();

            container.Dispose();

            _future.WaitHandle.Set();
        }

        private void GetDevice() {
            lock ( Lock ) {
                if ( _device == null ) {
                    IAsyncResult selectStorageResult = StorageDevice.BeginShowSelector(null, null);
                    selectStorageResult.AsyncWaitHandle.WaitOne();
                    _device = StorageDevice.EndShowSelector(selectStorageResult);
                    selectStorageResult.AsyncWaitHandle.Close();
                }
            }
        }

        private void AsyncLoad() {
            GetDevice();

            // Open a storage container
            IAsyncResult openContainerResult =
                _device.BeginOpenContainer("Escape from Enceladus", null, null);
            openContainerResult.AsyncWaitHandle.WaitOne();
            StorageContainer container = _device.EndOpenContainer(openContainerResult);
            openContainerResult.AsyncWaitHandle.Close();

            // Console.WriteLine("Loading save for player " + _slot);

            // Check to see whether the save exists.
            lock ( Lock ) {
                try {
                    string filename = SaveFileName();
                    if ( container.FileExists(filename) ) {
                        Stream stream = container.OpenFile(filename, FileMode.Open);
                        StreamReader reader = new StreamReader(stream);
                        string json = reader.ReadToEnd();
                        _future.SaveState = JsonConvert.DeserializeObject<SaveState>(json);
                        _future.SaveState.Slot = this.Slot;
                        reader.Close();

                        container.Dispose();
                    }
                } catch ( Exception e ) {
                    Console.WriteLine("Failed to load save state");
                    Console.WriteLine(e);
                    return;
                }
            }

            _future.WaitHandle.Set();
        }
    }

    /// <summary>
    /// Simple class to encapsulate a future save state result and its wait handle.
    /// </summary>
    public class SaveWaiter {
        public readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);
        public SaveState SaveState;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Storage;

namespace Enceladus.Event {
    /// <summary>
    /// Encapsulates everything about a save state.
    /// </summary>
    public class SaveState {
        public HashSet<GameMilestone> milestones;
        public string SaveStationId;
        private ManualResetEvent _waitHandle;

        /// <summary>
        /// Creates a new save state using the current game state and save station with the ID given.
        /// </summary>
        public static SaveState Create(String saveStationId) {
            SaveState state = new SaveState();
            state.milestones = GameState.Milestones;
            state.SaveStationId = saveStationId;
            return state;
        }

        /// <summary>
        /// Persists this save state to the storage container, 
        /// returning a handle to wait for this async operation.
        /// </summary>
        public ManualResetEvent Persist() {

            _waitHandle = new ManualResetEvent(false);
            new Thread(AsyncPersist).Start();

            return _waitHandle;
        }

        private void AsyncPersist() {
            // Get a storage device
            IAsyncResult selectStorageResult = StorageDevice.BeginShowSelector(null, null);
            selectStorageResult.AsyncWaitHandle.WaitOne();
            StorageDevice device = StorageDevice.EndShowSelector(selectStorageResult);
            selectStorageResult.AsyncWaitHandle.Close();

            // Open a storage container
            IAsyncResult openContainerResult =
                device.BeginOpenContainer("Escape from Enceladus", null, null);
            openContainerResult.AsyncWaitHandle.WaitOne();
            StorageContainer container = device.EndOpenContainer(openContainerResult);
            openContainerResult.AsyncWaitHandle.Close();

            string filename = "savegame.sav";

            // Check to see whether the save exists.
            if ( container.FileExists(filename) )
                // Delete it so that we can create one fresh.
                container.DeleteFile(filename);

            Stream stream = container.CreateFile(filename);
            XmlSerializer serializer = new XmlSerializer(typeof ( SaveState ));
            serializer.Serialize(stream, this);
            stream.Close();

            container.Dispose();

            _waitHandle.Set();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Storage;

namespace Arena.Event {
    /// <summary>
    /// Encapsulates everything about a save state.
    /// </summary>
    public class SaveState {
        public HashSet<GameMilestone> milestones;
        public string SaveStationId;

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
        /// Persists this save state to the storage container.
        /// </summary>
        public void Persist() {
            // Get a storage device
            IAsyncResult selectStorageResult = StorageDevice.BeginShowSelector(null, null);
            StorageDevice device = StorageDevice.EndShowSelector(selectStorageResult);
            selectStorageResult.AsyncWaitHandle.Close();            

            // Open a storage container
            IAsyncResult result =
                device.BeginOpenContainer("Escape from Enceladus", null, null);
            StorageContainer container = device.EndOpenContainer(result);
            result.AsyncWaitHandle.Close();

            string filename = "savegame.sav";
            
            // Check to see whether the save exists.
            if ( container.FileExists(filename) )
                // Delete it so that we can create one fresh.
                container.DeleteFile(filename);

            Stream stream = container.CreateFile(filename);
            XmlSerializer serializer = new XmlSerializer(typeof(SaveState));
            serializer.Serialize(stream, this);
            stream.Close();

            container.Dispose();
        }
    }
}

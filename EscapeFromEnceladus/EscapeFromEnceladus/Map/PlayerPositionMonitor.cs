using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Microsoft.Xna.Framework;

namespace Enceladus.Map {

    /// <summary>
    /// Helper class to keep track of where in the gameworld the player currently is and has been.
    /// </summary>
    public class PlayerPositionMonitor {

        private Player _player;
        private static PlayerPositionMonitor _instance;
        private Region _currentRegion;
        private Region _previousRegion;
        private Room _currentRoom;
        private Room _previousRoom;

        public static PlayerPositionMonitor Instance { get { return _instance; } }

        public PlayerPositionMonitor(Player player) {
            _player = player;
            _instance = this;
        }

        /// <summary>
        /// The current region the player is in.
        /// </summary>
        public Region CurrentRegion {
            get { return _currentRegion; }
        }

        /// <summary>
        /// The region the player was in the last time update was called.
        /// </summary>
        public Region PreviousRegion {
            get { return _previousRegion; }
        }

        /// <summary>
        /// The current room the player is in.
        /// </summary>
        public Room CurrentRoom {
            get { return _currentRoom; }
        }

        /// <summary>
        /// The room the player was in the last time update was called.
        /// </summary>
        public Room PreviousRoom {
            get { return _previousRoom; }
        }

        /// <summary>
        /// Returns whether a room change occurred on this update.
        /// </summary>
        /// <returns></returns>
        public bool IsNewRoomChange() {
            return _currentRoom != _previousRoom;
        }

        /// <summary>
        /// Returns whether a region change occurred on this update, whether in the 
        /// same room or as part of a room change.
        /// </summary>
        /// <returns></returns>
        public bool IsNewRegionChange() {
            return _currentRegion != _previousRegion;
        }

        /// <summary>
        /// Updates the player's current and previous position, and notifies 
        /// the game of a room change as necessary.
        /// </summary>
        public void Update() {
            _previousRegion = CurrentRegion;
            _previousRoom = CurrentRoom;

            _currentRoom = TileLevel.CurrentLevel.RoomAt(_player.Position);
            List<Region> regions = CurrentRoom.RegionsAt(_player.Position);
            if ( _currentRegion == null || !regions.Contains(_currentRegion) ) {
                _currentRegion = regions.First(r => r.Contains(_player.Position));
            }

            if ( IsNewRoomChange() ) {
                RoomChanged(_previousRoom, _currentRoom);
            }
        }

        public event RoomChangedHandle RoomChanged;

    }

    public delegate void RoomChangedHandle(Room oldRoom, Room newRoom);
}

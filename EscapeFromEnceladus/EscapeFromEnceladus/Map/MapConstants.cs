using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enceladus.Map {

    /// <summary>
    /// Mapping system constants for universal access
    /// </summary>
    class MapConstants {

        /*
         * Room width and height in tiles.  No exceptions.
         */
        public const int RoomHeight = 11;
        public const int RoomWidth = 20;

        /*
         * Tile offset between the top-left corner of a map and where tiles start.
         * Never let the player see the actual physical end of the map.
         */
        public const int TileOffsetX = 1;
        public const int TileOffsetY = 1;
    }
}

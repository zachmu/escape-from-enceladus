using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Map {

    /// <summary>
    /// A room is a logical unit of the map bounded by one or more doors or secret passages.
    /// It comprises one or more overlapping rectangular regions.
    /// </summary>
    [DebuggerDisplay("Room at {_regions.First().TopLeft}")]
    public class Room {
        public string BackgroundImage { get; private set; }
        public string MusicTrack { get; private set; }
        public string Id { get; private set; }

        private readonly List<Region> _regions = new List<Region>();
        private readonly List<Region> _roomGridAlignedRegions = new List<Region>(); 

        /// <summary>
        /// Creates a room consisting of exactly one region.
        /// </summary>
        public Room(Object region) {
            AddRegion(region);
        }

        /// <summary>
        /// Creates a room consisting of multiple overlapping regions.
        /// </summary>
        public Room(List<Object> regions) {
            foreach ( Object region in regions ) {
                AddRegion(region);
            }
        }

        /// <summary>
        /// Adds a region to the list of regions that comprise this room.
        /// </summary>
        /// <param name="region"></param>
        private void AddRegion(Object region) {
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(region.X, region.Y));
            var bottomRight = ConvertUnits.ToSimUnits(new Vector2(region.X + region.Width, region.Y + region.Height));

            topLeft = Region.AdjustToTileBoundary(topLeft);
            bottomRight = Region.AdjustToTileBoundary(bottomRight);

            if ( region.Properties.ContainsKey("id") ) {
                Id = region.Properties["id"];
            }
            if ( region.Properties.ContainsKey("background") ) {
                BackgroundImage = region.Properties["background"];
            }
            if ( region.Properties.ContainsKey("musicTrack") ) {
                MusicTrack = region.Properties["musicTrack"];
            }

            _regions.Add(new Region(topLeft, bottomRight));
            _roomGridAlignedRegions.Add(new Region(SnapToRoomGrid(topLeft), SnapToRoomGrid(bottomRight)));
        }

        public bool Contains(IGameEntity entity) {
            if ( entity is Door ) {
                return Intersects((Door) entity);
            } else {
                return Contains(entity.Position);
            }
        }

        public bool Contains(Vector2 position) {
            return _roomGridAlignedRegions.Any(region => region.Contains(position));
        }

        public bool Contains(Vector2 position, float f) {
            return _roomGridAlignedRegions.Any(region => region.Contains(position, f));
        }

        public bool Contains(int x, int y) {
            return _roomGridAlignedRegions.Any(region => region.Contains(x, y));
        }

        public bool Intersects(Door door) {
            return _roomGridAlignedRegions.Any(region => region.Intersects(door));
        }

        /// <summary>
        /// Returns all regions that contain the point given. 
        /// These regions are tile-aligned as the map indicates, but not necessarily room-aligned.
        /// </summary>
        public List<Region> RegionsAt(Vector2 position) {
            return _regions.Where(region => region.Contains(position)).ToList();
        }

        /// <summary>
        /// Returns a room-grid-adjusted point
        /// </summary>
        public static Vector2 SnapToRoomGrid(Vector2 point) {
            Vector2 gridPosition =
                new Vector2(
                    ((int) (point.X - MapConstants.TileOffsetX) / MapConstants.RoomWidth) * MapConstants.RoomWidth +
                    MapConstants.TileOffsetX,
                    (((int) (point.Y - MapConstants.TileOffsetY) / MapConstants.RoomHeight) * MapConstants.RoomHeight +
                     MapConstants.TileOffsetY));
            return gridPosition;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Map {
    
    /// <summary>
    /// A room is a logical unit of the map bounded by one or more doors or secret passages.
    /// It comprises one or more overlapping rectangular regions.
    /// </summary>
    public class Room {
        private string _id;
        private string _backgroundImage;
        private readonly List<Region> _regions = new List<Region>(); 

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
                _id = region.Properties["id"];
            }
            if ( region.Properties.ContainsKey("background") ) {
                _backgroundImage = region.Properties["background"];
            }
            _regions.Add(new Region(topLeft, bottomRight));
        }

        public String BackgroundImage { get { return _backgroundImage; } }

        public String ID { get { return _id; } }

        public bool Contains(Vector2 position) {
            return _regions.Any(region => region.Contains(position));
        }

        public bool Contains(Vector2 position, float f) {
            return _regions.Any(region => region.Contains(position, f));
        }

        public bool Contains(int x, int y) {
            return _regions.Any(region => region.Contains(x, y));
        }

        public bool Intersects(Door door) {
            return _regions.Any(region => region.Intersects(door));
        }

        /// <summary>
        /// Returns a representative region
        /// TODO: remove
        /// </summary>
        public Region Region {
          get { return _regions.First(); }
        }
    }
}

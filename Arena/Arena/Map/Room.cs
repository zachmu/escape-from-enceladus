﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Microsoft.Xna.Framework;

namespace Arena.Map {
    
    /// <summary>
    /// A room is a logical unit of the map bounded by one or more doors or secret passages.
    /// </summary>
    public class Room : Region {
        public Room(Vector2 topLeft, Vector2 bottomRight) {
            _topLeft = AdjustToTileBoundary(topLeft);
            _bottomRight = AdjustToTileBoundary(bottomRight);
        }

        /// <summary>
        /// Returns the relative direction of the point given to this room.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Direction GetRelativeDirection(Vector2 position) {
            if (position.X < TopLeft.X) {
                return Direction.Left;
            } else if (position.X > BottomRight.X) {
                return Direction.Right;
            } else if (position.Y < TopLeft.Y) {
                return Direction.Up;
            } else if (position.Y > BottomRight.Y) {
                return Direction.Down;
            } else {
                throw new Exception("Couldn't determine relative position");
            }
        }

        /// <summary>
        /// Returns whether this region intersects another.  
        /// This method considers co-linear regions to intesect.
        /// </summary>
        public bool Intersects(Region r) {
            return Contains(r.TopLeft, .01f)
                   || Contains(r.TopRight, .01f)
                   || Contains(r.BottomLeft, .01f)
                   || Contains(r.BottomRight, .01f);
        }
    }
}

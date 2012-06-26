using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Microsoft.Xna.Framework;

namespace Arena.Map {
    
    /// <summary>
    /// A room is a logical unit of the map bounded by one or more doors or secret passages.
    /// </summary>
    public class Room {

        private Vector2 _topLeft;
        private Vector2 _bottomRight;

        public Room(Vector2 topLeft, Vector2 bottomRight) {
            _topLeft = topLeft;
            _bottomRight = bottomRight;
        }

        public Vector2 BottomRight {
            get { return _bottomRight; }
        }

        public Vector2 TopLeft {
            get { return _topLeft; }
        }

        /// <summary>
        /// Returns whether the given point is in this room.
        /// </summary>
        public bool Contains(Vector2 position) {
            return Contains(position, 0f);
        }

        /// <summary>
        /// Same as Contains, but includes a buffer of the given thickness around the rectangle.
        /// </summary>
        public bool Contains(Vector2 position, float buffer) {
            return (position.X >= TopLeft.X - buffer 
                && position.X <= BottomRight.X + buffer
                && position.Y >= TopLeft.Y - buffer
                && position.Y <= BottomRight.Y + buffer);
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

    }
}

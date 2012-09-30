using System;
using Arena.Entity;
using FarseerPhysics.Collision;
using Microsoft.Xna.Framework;

namespace Arena.Map {
    public class Region {
        protected Vector2 _topLeft;
        protected Vector2 _bottomRight;

        public Region(Vector2 topLeft, Vector2 bottomRight) {
            _topLeft = topLeft;
            _bottomRight = bottomRight;
        }

        public Vector2 BottomRight {
            get { return _bottomRight; }
        }

        public Vector2 TopLeft {
            get { return _topLeft; }
        }

        public Vector2 BottomLeft {
            get { return _topLeft + new Vector2(0, Height); }
        }

        public Vector2 TopRight {
            get { return _topLeft + new Vector2(Width, 0); }
        }

        protected float Width {
            get { return BottomRight.X - TopLeft.X; }
        }

        protected float Height {
            get { return BottomRight.Y - TopLeft.Y; }
        }

        public Vector2 Position {
            get { return TopLeft + new Vector2(Width / 2, Height / 2); }
        }

        public static Vector2 AdjustToTileBoundary(Vector2 pos) {
            return new Vector2((float) Math.Round(pos.X), (float) Math.Round(pos.Y));
        }

        public AABB Aabb {
            get { return new AABB(TopLeft, BottomRight); }
        }

        /// <summary>
        /// Same as Contains, but includes a buffer of the given thickness around the rectangle.
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

        public bool Contains(int x, int y) {
            return Contains(new Vector2(x, y));
        }

        /// <summary>
        /// Converts to a rectangle.  Any fractional measurements are lost with this method, 
        /// so this is only valid with tile-aligned regions.
        /// </summary>
        /// <returns></returns>
        public Rectangle ToRectangle(int padding) {
            return new Rectangle((int) TopLeft.X - padding, (int) TopLeft.Y - padding, (int) Width + padding * 2, (int) Height + padding * 2);
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

        /// <summary>
        /// Returns the relative direction of the point given to this region.
        /// </summary>
        /// <param name="position"></param>
        public Direction GetRelativeDirection(Vector2 position) {
            if ( position.X < TopLeft.X ) {
                return Direction.Left;
            } else if ( position.X > BottomRight.X ) {
                return Direction.Right;
            } else if ( position.Y < TopLeft.Y ) {
                return Direction.Up;
            } else if ( position.Y > BottomRight.Y ) {
                return Direction.Down;
            } else {
                throw new Exception("Couldn't determine relative position");
            }
        }
        
        /// <summary>
        /// Returns a region shifted a given amount from this one.
        /// </summary>
        public Region Shift(Vector2 delta) {
            return new Region(_topLeft + delta, _bottomRight + delta);
        }
    }
}
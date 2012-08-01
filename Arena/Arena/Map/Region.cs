using System;
using Microsoft.Xna.Framework;

namespace Arena.Map {
    public abstract class Region {
        protected Vector2 _topLeft;
        protected Vector2 _bottomRight;

        public Vector2 BottomRight {
            get { return _bottomRight; }
        }

        public Vector2 TopLeft {
            get { return _topLeft; }
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

        protected static Vector2 AdjustToTileBoundary(Vector2 pos) {
            return new Vector2((float) Math.Round(pos.X), (float) Math.Round(pos.Y));
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
    }
}
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

        public String BackgroundImage {
            get { return _backgroundImage; }
        }

        public String ID {
            get { return _id; }
        }

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
        /// Returns all regions that contain the point given.
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

        #region cameracontrol

        /// <summary>
        /// Parralel lists of lines of camera constraint.  The camera will restrict its movement to these lines.
        /// </summary>
        private List<Vector2> _cameraConstraintEndpoint1;

        private List<Vector2> _cameraConstraintEndpoint2;

        /// <summary>
        /// For single-screen rooms, the camera constraint is just a point.
        /// </summary>
        private bool _singlePointConstraint;
        private Vector2 _cameraCenter;

        public void ClosestAreaOfConstraint(Vector2 point, out Vector2 min, out Vector2 max) {
            InitializeMinMaxCache();

            if ( _singlePointConstraint ) {
                min = max = _cameraCenter;
            } else {

                min = new Vector2();
                max = new Vector2();

                float minDist = float.MaxValue;
                for ( int i = 0; i < _cameraConstraintEndpoint1.Count; i++ ) {
                    float dist = DistanceBetweenPointAndLine(_cameraConstraintEndpoint1[i],
                                                             _cameraConstraintEndpoint2[i],
                                                             point);
                    if ( dist < minDist ) {
                        minDist = dist;
                        min = _cameraConstraintEndpoint1[i];
                        max = _cameraConstraintEndpoint2[i];
                    }
                }
            }
        }

        private void InitializeMinMaxCache() {
            if ( _cameraConstraintEndpoint1 == null ) {
                _cameraConstraintEndpoint1 = new List<Vector2>();
                _cameraConstraintEndpoint2 = new List<Vector2>();
                foreach ( Region region in _regions ) {
                    Vector2 viewportCenter = ConvertUnits.ToSimUnits(Arena.Instance.GraphicsDevice.Viewport.Width / 2f,
                                                                     Arena.Instance.GraphicsDevice.Viewport.Height / 2f);

                    // Some rooms don't line up with the grid, so pretend they do for camera purposes.
                    Vector2 topLeft = Room.SnapToRoomGrid(region.TopLeft);
                    Vector2 bottomRight = Room.SnapToRoomGrid(region.BottomRight + new Vector2(1));

                    Vector2 minPosition = topLeft + viewportCenter - new Vector2(0, .125f);
                    Vector2 maxPosition = bottomRight - viewportCenter + new Vector2(0, .125f);

                    //Console.WriteLine("Max = {0}, min = {1}", maxPosition, minPosition);

                    if ( maxPosition.X < minPosition.X ) {
                        float avgX = (maxPosition.X + minPosition.X) / 2;
                        maxPosition.X = avgX;
                        minPosition.X = avgX;
                    }

                    if ( maxPosition.Y < minPosition.Y ) {
                        float avgY = (maxPosition.Y + minPosition.Y) / 2;
                        maxPosition.Y = avgY;
                        minPosition.Y = avgY;
                    }

                    // If min = max, then this is a one-screen space.  Record it.
                    // TODO: this will only work if the entire room is one screen.
                    if ( maxPosition == minPosition ) {
                        _singlePointConstraint = true;
                        _cameraCenter = maxPosition;
                    }

                    // For each set of min, max, we store the (at most) four edges of that rectangle for easy lookup
                    CacheCameraConstraintEndpoints(minPosition, new Vector2(maxPosition.X, minPosition.Y));
                    CacheCameraConstraintEndpoints(minPosition, new Vector2(minPosition.X, maxPosition.Y));
                    CacheCameraConstraintEndpoints(new Vector2(minPosition.X, maxPosition.Y), maxPosition);
                    CacheCameraConstraintEndpoints(new Vector2(maxPosition.X, minPosition.Y), maxPosition);
                }
            }
        }

        private void CacheCameraConstraintEndpoints(Vector2 endpoint1, Vector2 endpoint2) {
            if ( endpoint1 != endpoint2 ) {
                _cameraConstraintEndpoint1.Add(endpoint1);
                _cameraConstraintEndpoint2.Add(endpoint2);
            }
        }

        private float DistanceBetweenPointAndLine(Vector2 endpoint1, Vector2 endpoint2, Vector2 point) {
            Vector2 a = endpoint1;
            Vector2 n = endpoint2 - endpoint1;
            n.Normalize();
            return ((a - point) - (Vector2.Dot(a - point, n) * n)).Length();
        }
    }

    #endregion
}

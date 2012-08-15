using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Map {
    
    public class VisitationMap {

        private readonly TileLevel _level;
        private readonly bool[,] _visitedScreens;
        private readonly bool[,] _knownScreens;
        private readonly int _numScreensX;
        private readonly int _numScreensY;
        
        private bool _activeFlash;
        private const int FlashTimeMs = 500;
        private int _timeSinceFlashChange = 0;

        private const int MapOverlayWidth = 7;
        private const int MapOverlayHeight = 7;
        private const int MiddleCellX = MapOverlayWidth / 2;
        private const int MiddleCellY = MapOverlayHeight / 2;

        /*
         * Drawing members
         */
        private const int CellWidthPixels = 32;
        private const int CellHeightPixels = 18;

        private const int OverlayHeightPixels = CellHeightPixels * MapOverlayHeight;
        private const int OverlayWidthPixels = CellWidthPixels * MapOverlayWidth;

        private static Texture2D _backdrop;

        private const int NumCells = 3;
        private const int UnvisitedCell = 0;
        private const int VisitedCell = 1;
        private const int ActiveCell = 2;
        private static readonly Texture2D[] _cells = new Texture2D[NumCells];

        private const int NumDoors = 4;
        private const int LeftDoor = 0;
        private const int TopDoor = 1;
        private const int RightDoor = 2;
        private const int BottomDoor = 3;
        private static readonly Texture2D[] _doors = new Texture2D[NumDoors];

        private const int NumWalls = 4;
        private const int RightWall = 0;
        private const int LeftWall = 1;
        private const int BottomWall = 2;
        private const int TopWall = 3;
        private static readonly Texture2D[] _walls = new Texture2D[NumWalls];

        // X offset is from the right side of the screen, since that's its orientation
        private const int DrawOffsetX = 40 + MapOverlayWidth + CellWidthPixels * MapOverlayWidth;
        private const int DrawOffsetY = 40;

        public VisitationMap(TileLevel level) {
            _level = level;
            int width = level.Width;
            int height = level.Height;

            _numScreensX = (width - MapConstants.TileOffsetX) / MapConstants.RoomWidth;
            _numScreensY = (height - MapConstants.TileOffsetY) / MapConstants.RoomHeight;

            _visitedScreens = new bool[_numScreensX, _numScreensY];
            _knownScreens = new bool[_numScreensX, _numScreensY];
        }

        public static void LoadContent(ContentManager cm) {
            for ( int i = 0; i < NumCells; i++ ) {
                _cells[i] = cm.Load<Texture2D>(String.Format("Overlay/Map/Cells/Cell{0:0000}", i));
            }
            for ( int i = 0; i < NumWalls; i++ ) {
                _walls[i] = cm.Load<Texture2D>(String.Format("Overlay/Map/Walls/Wall{0:0000}", i));
            }
            for ( int i = 0; i < NumDoors; i++ ) {
                _doors[i] = cm.Load<Texture2D>(String.Format("Overlay/Map/Doors/Door{0:0000}", i));
            }

            _backdrop = cm.Load<Texture2D>("Overlay/Map/Backdrop");
        }

        public void Update(GameTime gameTime) {
            int playerScreenX, playerScreenY;
            GetPlayerScreen(out playerScreenX, out playerScreenY);
            _visitedScreens[playerScreenX, playerScreenY] = true;
            _knownScreens[playerScreenX, playerScreenY] = true;
            _timeSinceFlashChange += gameTime.ElapsedGameTime.Milliseconds;
            if ( _timeSinceFlashChange >= FlashTimeMs ) {
                _activeFlash = !_activeFlash;
                _timeSinceFlashChange = 0;
            }
        }

        /// <summary>
        /// Draws the map as an overlay.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch) {
            int playerScreenX, playerScreenY;
            GetPlayerScreen(out playerScreenX, out playerScreenY);

            Vector2 drawOffset = new Vector2(spriteBatch.GraphicsDevice.Viewport.Width - DrawOffsetX, DrawOffsetY);
            spriteBatch.Draw(_backdrop, new Rectangle((int) drawOffset.X, (int) drawOffset.Y, OverlayWidthPixels, OverlayHeightPixels), Color.Black * .25f);

            for ( int y = 0; y < MapOverlayHeight; y++ ) {
                int cellY = playerScreenY - MapOverlayHeight / 2 + y;
                if ( cellY >= 0 && cellY < _numScreensY ) {
                    for ( int x = 0; x < MapOverlayWidth; x++ ) {
                        int cellX = playerScreenX - MapOverlayWidth / 2 + x;
                        if ( cellX >= 0 && cellX < _numScreensX ) {
                            DrawCell(spriteBatch, drawOffset, x, y, cellX, cellY);
                        }
                    }
                }
            }
        }

        private void DrawCell(SpriteBatch spriteBatch, Vector2 drawOffset, int x, int y, int cellX, int cellY) {
            Vector2 drawPosition = drawOffset + new Vector2(x * CellWidthPixels, y * CellHeightPixels);
            int cellCenterX = cellX * MapConstants.RoomWidth + MapConstants.RoomWidth / 2 +
                              MapConstants.TileOffsetX;
            int cellCenterY = cellY * MapConstants.RoomHeight + MapConstants.RoomHeight / 2 +
                              MapConstants.TileOffsetY;

            Room room = _level.RoomAt(cellCenterX, cellCenterY);

            if ( room != null && _knownScreens[cellX, cellY] ) {
                // Draw the cell background 
                float wallAlpha = .65f;
                if ( x == MiddleCellX && y == MiddleCellY ) {
                    spriteBatch.Draw(_cells[ActiveCell], drawPosition, Color.White * wallAlpha);
                } else {
                    spriteBatch.Draw(_visitedScreens[cellX, cellY] ? _cells[VisitedCell] : _cells[UnvisitedCell],
                                     drawPosition, Color.White * wallAlpha);
                }

                float doorAlpha = .9f;
                // Draw each of the four walls if the room doesn't continue in that direction, 
                // and a door if there's one in that wall.

                // top
                if ( !RoomContainsPoint(room, cellCenterX, cellCenterY - MapConstants.RoomHeight) ) {
                    spriteBatch.Draw(_walls[TopWall], drawPosition, Color.White * wallAlpha);
                    Rectangle topWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX + 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY - 1,
                                      MapConstants.RoomWidth - 1, 2);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => topWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[TopDoor], drawPosition, Color.White * doorAlpha);
                    }
                }

                // right
                if ( !RoomContainsPoint(room, cellCenterX + MapConstants.RoomWidth, cellCenterY) ) {
                    spriteBatch.Draw(_walls[RightWall], drawPosition, Color.White * wallAlpha);
                    Rectangle rightWallIntersection =
                        new Rectangle((cellX + 1) * MapConstants.RoomWidth + MapConstants.TileOffsetX - 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY + 1,
                                      2, MapConstants.RoomHeight - 1);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => rightWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[RightDoor], drawPosition, Color.White * doorAlpha);
                    }
                }

                // bottom
                if ( !RoomContainsPoint(room, cellCenterX, cellCenterY + MapConstants.RoomHeight) ) {
                    spriteBatch.Draw(_walls[BottomWall], drawPosition, Color.White * wallAlpha);
                    Rectangle bottomWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX + 1,
                                      (cellY + 1) * MapConstants.RoomHeight + MapConstants.TileOffsetY - 1,
                                      MapConstants.RoomWidth - 1, 2);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => bottomWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[BottomDoor], drawPosition, Color.White * doorAlpha);
                    }
                }

                // left
                if ( !RoomContainsPoint(room, cellCenterX - MapConstants.RoomWidth, cellCenterY) ) {
                    spriteBatch.Draw(_walls[LeftWall], drawPosition, Color.White * wallAlpha);
                    Rectangle leftWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX - 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY + 1,
                                      2, MapConstants.RoomHeight + 1);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => leftWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[LeftDoor], drawPosition, Color.White * doorAlpha);
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether the room given contains the point given, consider non-rectangular room conglomerations.
        /// </summary>
        private bool RoomContainsPoint(Room room, int x, int y) {
            if ( room.ID == null )
                return room.Contains(x, y);
            Room otherRoom = TileLevel.CurrentLevel.RoomAt(x, y);
            return otherRoom != null && room.ID == otherRoom.ID;
        }

        /// <summary>
        /// Retrieves the player's coordinate's in screens
        /// </summary>
        private void GetPlayerScreen(out int playerScreenX, out int playerScreenY) {
            Vector2 playerPos = Player.Instance.Position - new Vector2(1);
            playerScreenX = (int) playerPos.X / MapConstants.RoomWidth;
            playerScreenY = (int) playerPos.Y / MapConstants.RoomHeight;
        }
    }
}

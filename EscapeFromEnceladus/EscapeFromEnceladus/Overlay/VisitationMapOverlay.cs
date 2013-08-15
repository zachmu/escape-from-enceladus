using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Map;
using Enceladus.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {
    public class VisitationMapOverlay : IOverlayElement {

        private readonly Oscillator _flash = new Oscillator(FlashTimeMs, false);
        private const int FlashTimeMs = 500;

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
        private const int DrawOffsetX = 20 + MapOverlayWidth + CellWidthPixels * MapOverlayWidth;
        private const int DrawOffsetY = 20;
        private const float WallAlpha = .65f;
        private const float DoorAlpha = .9f;

        private readonly VisitationMap _visitationMap;
        private readonly TileLevel _level;

        public VisitationMapOverlay(VisitationMap visitationMap) {
            _visitationMap = visitationMap;
            _level = TileLevel.CurrentLevel;
        }

        /// <summary>
        /// Draws the map as an overlay.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch) {
            int playerScreenX, playerScreenY;
            _visitationMap.GetPlayerScreen(out playerScreenX, out playerScreenY);

            Vector2 drawOffset = new Vector2(spriteBatch.GraphicsDevice.Viewport.Width - DrawOffsetX, DrawOffsetY);
            spriteBatch.Draw(_backdrop, new Rectangle((int) drawOffset.X, (int) drawOffset.Y, OverlayWidthPixels, OverlayHeightPixels), Color.Black * .25f);

            for ( int y = 0; y < MapOverlayHeight; y++ ) {
                int cellY = playerScreenY - MapOverlayHeight / 2 + y;
                if ( cellY >= 0 && cellY < _visitationMap.NumScreensY ) {
                    for ( int x = 0; x < MapOverlayWidth; x++ ) {
                        int cellX = playerScreenX - MapOverlayWidth / 2 + x;
                        if ( cellX >= 0 && cellX < _visitationMap.NumScreensX ) {
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

            if ( room != null && _visitationMap.KnownScreens[cellX, cellY] ) {
                // Draw the cell background 
                if ( x == MiddleCellX && y == MiddleCellY ) {
                    spriteBatch.Draw(_cells[ActiveCell], drawPosition, Color.White * WallAlpha);
                } else {
                    spriteBatch.Draw(_visitationMap.VisitedScreens[cellX, cellY] ? _cells[VisitedCell] : _cells[UnvisitedCell],
                                     drawPosition, Color.White * WallAlpha);
                }

                // Draw each of the four walls if the room doesn't continue in that direction, 
                // and a door if there's one in that wall.

                // top
                if ( !room.Contains(cellCenterX, cellCenterY - MapConstants.RoomHeight) ) {
                    spriteBatch.Draw(_walls[TopWall], drawPosition, Color.White * WallAlpha);
                    Rectangle topWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX + 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY - 1,
                                      MapConstants.RoomWidth - 1, 2);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => topWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[TopDoor], drawPosition, Color.White * DoorAlpha);
                    }
                }

                // right
                if ( !room.Contains(cellCenterX + MapConstants.RoomWidth, cellCenterY) ) {
                    spriteBatch.Draw(_walls[RightWall], drawPosition, Color.White * WallAlpha);
                    Rectangle rightWallIntersection =
                        new Rectangle((cellX + 1) * MapConstants.RoomWidth + MapConstants.TileOffsetX - 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY + 1,
                                      2, MapConstants.RoomHeight - 1);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => rightWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[RightDoor], drawPosition, Color.White * DoorAlpha);
                    }
                }

                // bottom
                if ( !room.Contains(cellCenterX, cellCenterY + MapConstants.RoomHeight) ) {
                    spriteBatch.Draw(_walls[BottomWall], drawPosition, Color.White * WallAlpha);
                    Rectangle bottomWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX + 1,
                                      (cellY + 1) * MapConstants.RoomHeight + MapConstants.TileOffsetY - 1,
                                      MapConstants.RoomWidth - 1, 2);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => bottomWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[BottomDoor], drawPosition, Color.White * DoorAlpha);
                    }
                }

                // left
                if ( !room.Contains(cellCenterX - MapConstants.RoomWidth, cellCenterY) ) {
                    spriteBatch.Draw(_walls[LeftWall], drawPosition, Color.White * WallAlpha);
                    Rectangle leftWallIntersection =
                        new Rectangle(cellX * MapConstants.RoomWidth + MapConstants.TileOffsetX - 1,
                                      cellY * MapConstants.RoomHeight + MapConstants.TileOffsetY + 1,
                                      2, MapConstants.RoomHeight + 1);
                    if (
                        _level.GetDoorsAttachedToRoom(room).Any(
                            door => leftWallIntersection.Intersects(door.ToRectangle(0))) ) {
                        spriteBatch.Draw(_doors[LeftDoor], drawPosition, Color.White * DoorAlpha);
                    }
                }
            }
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


        public bool Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Event;
using Enceladus.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Map {
    
    /// <summary>
    /// The visitation map records where the player is been, 
    /// and also which additional parts of the map they know of.
    /// </summary>
    public class VisitationMap : ISaveable {

        private readonly bool[,] _visitedScreens;
        private readonly bool[,] _knownScreens;
        private readonly int _numScreensX;
        private readonly int _numScreensY;

        public VisitationMap(TileLevel level) {
            _numScreensX = (level.Width - MapConstants.TileOffsetX) / MapConstants.RoomWidth;
            _numScreensY = (level.Height - MapConstants.TileOffsetY) / MapConstants.RoomHeight;

            _visitedScreens = new bool[NumScreensX, NumScreensY];
            _knownScreens = new bool[NumScreensX, NumScreensY];
        }

        public int NumScreensY {
            get { return _numScreensY; }
        }

        public int NumScreensX {
            get { return _numScreensX; }
        }

        public bool[,] KnownScreens {
            get { return _knownScreens; }
        }

        public bool[,] VisitedScreens {
            get { return _visitedScreens; }
        }

        /// <summary>
        /// Loads the visitation info from the save state given.
        /// </summary>
        public void LoadFromSave(SaveState save) {
            Array.Clear(VisitedScreens, 0, VisitedScreens.Length);
            Array.Clear(KnownScreens, 0, KnownScreens.Length);
            for ( int i = 0; i < save.VisitedScreensX.Count; i++ ) {
                VisitedScreens[save.VisitedScreensX[i], save.VisitedScreensY[i]] = true;
            }
            for ( int i = 0; i < save.KnownScreensX.Count; i++ ) {
                KnownScreens[save.KnownScreensX[i], save.KnownScreensY[i]] = true;
            }
        }

        /// <summary>
        /// Saves the visitation info into the save state given.
        /// </summary>
        public void Save(SaveState save) {
            save.VisitedScreensX = new List<int>();
            save.VisitedScreensY = new List<int>();
            save.KnownScreensX = new List<int>();
            save.KnownScreensY = new List<int>();
            for ( int x = 0; x < NumScreensX; x++ ) {
                for ( int y = 0; y < NumScreensY; y++ ) {
                    if ( VisitedScreens[x, y] ) {
                        save.VisitedScreensX.Add(x);
                        save.VisitedScreensY.Add(y);
                    }
                    if ( KnownScreens[x, y] ) {
                        save.KnownScreensX.Add(x);
                        save.KnownScreensY.Add(y);
                    }
                }
            }
        }

        public void Update(GameTime gameTime) {
            int playerScreenX, playerScreenY;
            GetPlayerScreen(out playerScreenX, out playerScreenY);
            VisitedScreens[playerScreenX, playerScreenY] = true;
            KnownScreens[playerScreenX, playerScreenY] = true;
        }

        /// <summary>
        /// Retrieves the player's coordinates in screens
        /// </summary>
        public void GetPlayerScreen(out int playerScreenX, out int playerScreenY) {
            Vector2 playerPos = Player.Instance.Position - new Vector2(MapConstants.TileOffsetX, MapConstants.TileOffsetY);
            playerScreenX = (int) playerPos.X / MapConstants.RoomWidth;
            playerScreenY = (int) playerPos.Y / MapConstants.RoomHeight;
        }
    }
}

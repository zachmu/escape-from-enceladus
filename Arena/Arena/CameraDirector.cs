using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
using Arena.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {

    /// <summary>
    /// Controls all camera movement
    /// </summary>
    class CameraDirector {
        private enum Mode {
            TrackPlayer,
            ManualControl,
            SnapToGrid,
            MoveBetweenRooms,
        }

        private const int HorizontalMargin = 200;
        private const int VerticalMargin = 200;

        private const int RoomHeight = 11;
        private const int RoomWidth = 20;
        private const int TileOffsetX = 1;
        private const int TileOffsetY = 1;

        private readonly Camera2D _camera;
        private readonly Player _player;
        private readonly GraphicsDeviceManager _graphics;
        private readonly InputHelper _inputHelper;
        private Mode _mode;
        private Vector2 _nextCameraPosition;

        public CameraDirector(Camera2D camera, Player player, GraphicsDeviceManager graphics, InputHelper inputHelper) {
            _camera = camera;
            _player = player;
            _graphics = graphics;
            _inputHelper = inputHelper;
            _mode = Mode.TrackPlayer;
        }

        /// <summary>
        /// Updates the camera, and returns whether or not to pause the game action.
        /// </summary>
        public bool Update(GameTime gameTime) {

            HandleManualControl();

            switch (_mode) {
                case Mode.TrackPlayer:
                    TrackPlayer();
                    CheckForRoomTransition();
                    break;
                case Mode.ManualControl:
                    break;
                case Mode.SnapToGrid:
                    if ( _camera.IsAtTarget() ) {
                        _mode = Mode.MoveBetweenRooms;
                        _camera.Position = _nextCameraPosition;
                    }
                    break;
                case Mode.MoveBetweenRooms:
                    if ( _camera.IsAtTarget() ) {
                        _mode = Mode.TrackPlayer;
                        ClampCameraToRoom();
                        Arena.Instance.ResumeSimulation();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
        }

        private void TrackPlayer() {
            // If the character goes outside a margin area, move the camera
            Rectangle viewportMargin = new Rectangle(_graphics.GraphicsDevice.Viewport.X + HorizontalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Y + VerticalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Width - 2 * HorizontalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Height - 2 * VerticalMargin);
            Vector2 spriteScreenPosition = _camera.ConvertWorldToScreen(_player.Position);
            float spriteHeight = ConvertUnits.ToDisplayUnits(_player.Height);
            float spriteWidth = ConvertUnits.ToDisplayUnits(_player.Width);

            int maxx =
                (int) (viewportMargin.Right - spriteWidth);
            int minx = viewportMargin.Left;
            int maxy = (int) (viewportMargin.Bottom - spriteHeight);
            int miny = viewportMargin.Top;

            // Move the camera just enough to position the sprite at the edge of the margin
            if ( spriteScreenPosition.X > maxx ) {
                float delta = spriteScreenPosition.X - maxx;
                _camera.MoveCamera(ConvertUnits.ToSimUnits(delta, 0));
            } else if ( spriteScreenPosition.X < minx ) {
                float delta = spriteScreenPosition.X - minx;
                _camera.MoveCamera(ConvertUnits.ToSimUnits(delta, 0));
            }

            if ( spriteScreenPosition.Y > maxy ) {
                float delta = spriteScreenPosition.Y - maxy;
                _camera.MoveCamera(ConvertUnits.ToSimUnits(0, delta));
            } else if ( spriteScreenPosition.Y < miny ) {
                float delta = spriteScreenPosition.Y - miny;
                _camera.MoveCamera(ConvertUnits.ToSimUnits(0, delta));
            }
        }

        private void CheckForRoomTransition() {
            Room currentRoom = TileLevel.CurrentRoom;

            if ( !currentRoom.Contains(_player.Position) ) {
                UnclampCamera();
                Arena.Instance.PauseSimulation();

                _mode = Mode.SnapToGrid;
                Direction directionOfTravel = currentRoom.GetRelativeDirection(_player.Position);

                currentRoom = TileLevel.CurrentLevel.SetCurrentRoom(_player.Position);
                Arena.Instance.CurrentRoomChanged(currentRoom);

                float halfScreenWidth = _graphics.GraphicsDevice.Viewport.Width / 2f;
                float halfScreenHeight = _graphics.GraphicsDevice.Viewport.Height / 2f;

                Vector2 gridPosition =
                    new Vector2(((int) (_camera.Position.X - TileOffsetX) / RoomWidth) * RoomWidth + RoomWidth / 2f + TileOffsetX,
                                (((int) (_camera.Position.Y - TileOffsetY) / RoomHeight) * RoomHeight + RoomHeight / 2f + TileOffsetY));
                _camera.Position = gridPosition;

                switch ( directionOfTravel ) {
                    case Direction.Left:

                        _nextCameraPosition =
                            new Vector2(
                                currentRoom.BottomRight.X -
                                ConvertUnits.ToSimUnits(halfScreenWidth),
                                gridPosition.Y);

                        break;
                    case Direction.Right:
                        _nextCameraPosition =
                            new Vector2(
                                currentRoom.TopLeft.X +
                                ConvertUnits.ToSimUnits(halfScreenWidth),
                                gridPosition.Y);
                        break;
                    case Direction.Up:
                        _nextCameraPosition =
                            new Vector2(
                                gridPosition.X, currentRoom.BottomRight.Y -
                                                ConvertUnits.ToSimUnits(halfScreenHeight));
                        break;
                    case Direction.Down:
                        _nextCameraPosition =
                            new Vector2(
                                gridPosition.X, currentRoom.TopLeft.Y +
                                                ConvertUnits.ToSimUnits(halfScreenHeight));
                        break;
                }
            }
        }

        private void HandleManualControl() {
            foreach ( var pressedKey in _inputHelper.KeyboardState.GetPressedKeys() ) {
                switch ( pressedKey ) {
                    case Keys.Up:
                        SetManualCamera();
                        _camera.MoveCamera(new Vector2(0, -1));
                        break;
                    case Keys.Down:
                        SetManualCamera();
                        _camera.MoveCamera(new Vector2(0, 1));
                        break;
                    case Keys.Left:
                        SetManualCamera();
                        _camera.MoveCamera(new Vector2(-1, 0));
                        break;
                    case Keys.Right:
                        SetManualCamera();
                        _camera.MoveCamera(new Vector2(1, 0));
                        break;
                    case Keys.LeftAlt:
                        _mode = Mode.TrackPlayer;
                        break;
                }
            }
        }

        /// <summary>
        /// Constrains the camera position to the current room in the level.
        /// </summary>
        public void ClampCameraToRoom() {
            Vector2 viewportCenter = ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Width / 2f,
                                                             _graphics.GraphicsDevice.Viewport.Height / 2f);
            
            Vector2 minPosition = TileLevel.CurrentRoom.TopLeft + viewportCenter - new Vector2(0, .125f);
            Vector2 maxPosition = TileLevel.CurrentRoom.BottomRight - viewportCenter + new Vector2(0, .125f);

            Console.WriteLine("Max = {0}, min = {1}", maxPosition, minPosition);

            if (maxPosition.X < minPosition.X) {
                float avgX = (maxPosition.X + minPosition.X) / 2;
                maxPosition.X = avgX;
                minPosition.X = avgX;
            } 

            if ( maxPosition.Y < minPosition.Y ) {
                float avgY = (maxPosition.Y + minPosition.Y) / 2;
                maxPosition.Y = avgY;
                minPosition.Y = avgY;
            }

            _camera.MinPosition = minPosition;
            _camera.MaxPosition = maxPosition;

            _camera.ConstrainPosition = true;
        }

        private void UnclampCamera() {
            _camera.MinPosition = _camera.MaxPosition;
            _camera.ConstrainPosition = false;
        }

        private void SetManualCamera() {
            _mode = Mode.ManualControl;
            UnclampCamera();           
        }
    }

}

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

        private const int HorizontalMargin = 450;
        private const int VerticalMargin = 250;

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
        /// Updates the camera.
        /// </summary>
        public void Update(GameTime gameTime) {

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
                        if ( Arena.Instance.BackgroundAlpha <= 0 ) {
                            _mode = Mode.MoveBetweenRooms;
                            _camera.Position = _nextCameraPosition;
                        }
                    }
                    break;
                case Mode.MoveBetweenRooms:
                    if ( _camera.IsAtTarget() ) {
                        _mode = Mode.TrackPlayer;
                        ClampCameraToRegion(PlayerPositionMonitor.Instance.CurrentRegion);
                        Arena.Instance.LoadRoom(PlayerPositionMonitor.Instance.CurrentRoom);
                        Arena.Instance.ResumeSimulation();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TrackPlayer() {
            // If the character goes outside a margin area, move the camera
            Rectangle viewportMargin = new Rectangle(_graphics.GraphicsDevice.Viewport.X + HorizontalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Y + VerticalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Width - 2 * HorizontalMargin,
                                                     _graphics.GraphicsDevice.Viewport.Height - 2 * VerticalMargin);

            // Project the player's future position so that the camera leads him by a half second
            Vector2 velocity = _player.LinearVelocity / 2;
            Vector2 minVelocity = new Vector2(-12,-3);
            Vector2 maxVelocity = new Vector2(12, 3);
            Vector2.Clamp(ref velocity, ref minVelocity, ref maxVelocity, out velocity);
            Vector2 futurePosition = _player.Position + velocity;

            Vector2 spriteScreenPosition = _camera.ConvertWorldToScreen(futurePosition);
            float spriteHeight = ConvertUnits.ToDisplayUnits(_player.Height);
            float spriteWidth = ConvertUnits.ToDisplayUnits(_player.Width);

            int maxx =
                (int) (viewportMargin.Right - spriteWidth);
            int minx = viewportMargin.Left;
            int maxy = (int) (viewportMargin.Bottom - spriteHeight);
            int miny = viewportMargin.Top;

            // Move the camera just enough to position the sprite at the edge of the margin
            Vector2 cameraDelta = Vector2.Zero;
            if ( spriteScreenPosition.X > maxx ) {
                float delta = spriteScreenPosition.X - maxx;
                cameraDelta += ConvertUnits.ToSimUnits(delta, 0);
            } else if ( spriteScreenPosition.X < minx ) {
                float delta = spriteScreenPosition.X - minx;
                cameraDelta += ConvertUnits.ToSimUnits(delta, 0);
            }

            if ( spriteScreenPosition.Y > maxy ) {
                float delta = spriteScreenPosition.Y - maxy;
                cameraDelta += ConvertUnits.ToSimUnits(0, delta);
            } else if ( spriteScreenPosition.Y < miny ) {
                float delta = spriteScreenPosition.Y - miny;
                cameraDelta += ConvertUnits.ToSimUnits(0, delta);
            }

            _camera.MoveTarget(cameraDelta);
        }

        private void CheckForRoomTransition() {

            if ( PlayerPositionMonitor.Instance.IsNewRoomChange() ) {

                UnclampCamera();
                Arena.Instance.PauseSimulation();

                _mode = Mode.SnapToGrid;
                Direction directionOfTravel =
                    PlayerPositionMonitor.Instance.PreviousRegion.GetRelativeDirection(_player.Position);

                float halfScreenWidth = _graphics.GraphicsDevice.Viewport.Width / 2f;
                float halfScreenHeight = _graphics.GraphicsDevice.Viewport.Height / 2f;

                int xOffset = 0, yOffset = 0;
                switch ( directionOfTravel ) {
                    case Direction.Left:
                        xOffset = 1;
                        break;
                    case Direction.Right:
                        xOffset = -1;
                        break;
                    case Direction.Up:
                        yOffset = 1;
                        break;
                    case Direction.Down:
                        yOffset = -1;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Vector2 gridPosition =
                    new Vector2(
                        ((int) (_player.Position.X - MapConstants.TileOffsetX + xOffset) /
                         MapConstants.RoomWidth) * MapConstants.RoomWidth + MapConstants.RoomWidth / 2f +
                        MapConstants.TileOffsetX,
                        (((int) (_player.Position.Y - MapConstants.TileOffsetY + yOffset) /
                          MapConstants.RoomHeight) * MapConstants.RoomHeight +
                         MapConstants.RoomHeight / 2f + MapConstants.TileOffsetY));
                _camera.Position = gridPosition;

                switch ( directionOfTravel ) {
                    case Direction.Left:
                        _nextCameraPosition =
                            new Vector2(
                                PlayerPositionMonitor.Instance.CurrentRegion.BottomRight.X -
                                ConvertUnits.ToSimUnits(halfScreenWidth),
                                gridPosition.Y);

                        break;
                    case Direction.Right:
                        _nextCameraPosition =
                            new Vector2(
                                PlayerPositionMonitor.Instance.CurrentRegion.TopLeft.X +
                                ConvertUnits.ToSimUnits(halfScreenWidth),
                                gridPosition.Y);
                        break;
                    case Direction.Up:
                        _nextCameraPosition =
                            new Vector2(
                                gridPosition.X, PlayerPositionMonitor.Instance.CurrentRegion.BottomRight.Y -
                                                ConvertUnits.ToSimUnits(halfScreenHeight));
                        break;
                    case Direction.Down:
                        _nextCameraPosition =
                            new Vector2(
                                gridPosition.X, PlayerPositionMonitor.Instance.CurrentRegion.TopLeft.Y +
                                                ConvertUnits.ToSimUnits(halfScreenHeight));
                        break;
                }
            } else if (PlayerPositionMonitor.Instance.IsNewRegionChange()) {
                ClampCameraToRegion(PlayerPositionMonitor.Instance.CurrentRegion);
            }
        }

        /// <summary>
        /// Constrains the camera position to the current room in the level.
        /// </summary>
        public void ClampCameraToRegion(Region region) {
            Vector2 viewportCenter = ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Width / 2f,
                                                             _graphics.GraphicsDevice.Viewport.Height / 2f);
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

            _camera.ConstrainToRegion(minPosition, maxPosition);
        }

        /// <summary>
        /// Moves the target to center on the player.
        /// </summary>
        public void TargetPlayer() {
            _camera.Position = _player.Position;
        }

        /// <summary>
        /// Immediately move the camera to its target position.
        /// </summary>
        public void JumpToTarget() {
            _camera.Jump2Target();
        }

        private void UnclampCamera() {
            _camera.IsConstrainPosition = false;
        }

        private void SetManualCamera() {
            _mode = Mode.ManualControl;
            UnclampCamera();           
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
    }
}

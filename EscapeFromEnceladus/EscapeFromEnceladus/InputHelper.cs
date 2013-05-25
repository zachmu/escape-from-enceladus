using System;
using System.Collections.Generic;
using System.Linq;
using Enceladus.Entity;
using Enceladus.Weapon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Enceladus {
    /// <summary>
    ///   an enum of all available mouse buttons.
    /// </summary>
    public enum MouseButtons {
        LeftButton,
        MiddleButton,
        RightButton,
        ExtraButton1,
        ExtraButton2
    }

    public class InputHelper {
        private GamePadState _currentGamePadState;
        private KeyboardState _currentKeyboardState;
        private MouseState _currentMouseState;

        private GamePadState _lastGamePadState;
        private KeyboardState _lastKeyboardState;
        private MouseState _lastMouseState;

        private static readonly InputHelper _instance = new InputHelper();

        public static InputHelper Instance {
            get { return _instance; }
        }

        /// <summary>
        /// Constructs a new input state.  
        /// Construct only if you need to use a separate input state than the global one.
        /// </summary>
        public InputHelper() {
            _currentKeyboardState = new KeyboardState();
            _currentGamePadState = new GamePadState();
            _currentMouseState = new MouseState();

            _lastKeyboardState = new KeyboardState();
            _lastGamePadState = new GamePadState();
            _lastMouseState = new MouseState();
        }

        public GamePadState GamePadState {
            get { return _currentGamePadState; }
        }

        public KeyboardState KeyboardState {
            get { return _currentKeyboardState; }
        }

        public MouseState MouseState {
            get { return _currentMouseState; }
        }

        public GamePadState PreviousGamePadState {
            get { return _lastGamePadState; }
        }

        public KeyboardState PreviousKeyboardState {
            get { return _lastKeyboardState; }
        }

        public MouseState PreviousMouseState {
            get { return _lastMouseState; }
        }

        public void LoadContent(GraphicsDevice gd) {
        }

        /// <summary>
        ///   Reads the latest state of the keyboard and gamepad and mouse/touchpad.
        /// </summary>
        public void Update(GameTime gameTime) {
            _lastKeyboardState = _currentKeyboardState;
            _lastGamePadState = _currentGamePadState;
            _lastMouseState = _currentMouseState;

            _currentKeyboardState = Keyboard.GetState();
            _currentGamePadState = GamePad.GetState(PlayerIndex.One);
            _currentMouseState = Mouse.GetState();
        }

        /// <summary>
        ///   Helper for checking if a key was newly pressed during this update.
        /// </summary>
        public bool IsNewKeyPress(Keys key) {
            return (_currentKeyboardState.IsKeyDown(key) &&
                    _lastKeyboardState.IsKeyUp(key));
        }

        public IEnumerable<Keys> GetNewKeyPresses() {
             return new List<Keys>(_currentKeyboardState.GetPressedKeys()).Where(IsNewKeyPress);
        }

        public bool IsNewKeyRelease(Keys key) {
            return (_lastKeyboardState.IsKeyDown(key) &&
                    _currentKeyboardState.IsKeyUp(key));
        }

        /// <summary>
        ///   Helper for checking if a button was newly pressed during this update.
        /// </summary>
        public bool IsNewButtonPress(Buttons button) {
            return (_currentGamePadState.IsButtonDown(button) &&
                    _lastGamePadState.IsButtonUp(button));
        }

        public bool IsNewButtonRelease(Buttons button) {
            return (_lastGamePadState.IsButtonDown(button) &&
                    _currentGamePadState.IsButtonUp(button));
        }

        public bool IsGamePadNewDirection(out Direction? direction) {
            Vector2 prevStick = _lastGamePadState.ThumbSticks.Left;
            Vector2 currStick = _currentGamePadState.ThumbSticks.Left;
            Direction? prevDirection = GetStickDirection(prevStick);
            Direction? currDirection = GetStickDirection(currStick);
            if ( currDirection != null && currDirection != prevDirection ) {
                direction = currDirection;
                return true;
            } else {
                direction = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the octant being aimed in by the stick given, 
        /// or null if there is no aimed direction.
        /// </summary>
        /// <param name="stick"> </param>
        /// <returns></returns>
        public Direction? GetStickDirection(Vector2 stick) {
            float length = stick.Length();
            if ( length < .5f ) {
                return null;
            }
            float angle = (float) Math.Atan2(stick.Y, stick.X);

            // doing it by quadrant is easier
            // QI
            if ( angle >= 0 && angle <= Projectile.PiOverTwo ) {
                if ( angle <= Projectile.PiOverEight ) {
                    return Direction.Right;
                } else if ( angle <= Projectile.ThreePiOverEight ) {
                    return Direction.UpRight;
                } else {
                    return Direction.Up;
                }
            }
            // QII
            if ( angle >= 0 && angle >= Projectile.PiOverTwo ) {
                if ( angle <= Projectile.FivePiOverEight ) {
                    return Direction.Up;
                } else if ( angle <= Projectile.SevenPiOverEight ) {
                    return Direction.UpLeft;
                } else {
                    return Direction.Left;
                }
            }
            // QIII
            if ( angle <= 0 && angle <= -Projectile.PiOverTwo ) {
                if ( angle >= -Projectile.FivePiOverEight ) {
                    return Direction.Down;
                } else if ( angle >= -Projectile.SevenPiOverEight ) {
                    return Direction.DownLeft;
                } else {
                    return Direction.Left;
                }
            }
            // QIV
            if ( angle <= 0 && angle >= -Projectile.PiOverTwo ) {
                if ( angle >= -Projectile.PiOverEight ) {
                    return Direction.Right;
                } else if ( angle >= -Projectile.ThreePiOverEight ) {
                    return Direction.DownRight;
                } else {
                    return Direction.Down;
                }
            }

            throw new Exception("Couldn't determine quadrant of atan2");
        }

        /// <summary>
        /// Helper for checking if a mouse button was newly pressed during this update.
        /// </summary>
        public bool IsNewMouseButtonPress(MouseButtons button) {
            switch ( button ) {
                case MouseButtons.LeftButton:
                    return (_currentMouseState.LeftButton == ButtonState.Pressed &&
                            _lastMouseState.LeftButton == ButtonState.Released);
                case MouseButtons.RightButton:
                    return (_currentMouseState.RightButton == ButtonState.Pressed &&
                            _lastMouseState.RightButton == ButtonState.Released);
                case MouseButtons.MiddleButton:
                    return (_currentMouseState.MiddleButton == ButtonState.Pressed &&
                            _lastMouseState.MiddleButton == ButtonState.Released);
                case MouseButtons.ExtraButton1:
                    return (_currentMouseState.XButton1 == ButtonState.Pressed &&
                            _lastMouseState.XButton1 == ButtonState.Released);
                case MouseButtons.ExtraButton2:
                    return (_currentMouseState.XButton2 == ButtonState.Pressed &&
                            _lastMouseState.XButton2 == ButtonState.Released);
                default:
                    return false;
            }
        }


        /// <summary>
        /// Checks if the requested mouse button is released.
        /// </summary>
        /// <param name="button">The button.</param>
        public bool IsNewMouseButtonRelease(MouseButtons button) {
            switch ( button ) {
                case MouseButtons.LeftButton:
                    return (_lastMouseState.LeftButton == ButtonState.Pressed &&
                            _currentMouseState.LeftButton == ButtonState.Released);
                case MouseButtons.RightButton:
                    return (_lastMouseState.RightButton == ButtonState.Pressed &&
                            _currentMouseState.RightButton == ButtonState.Released);
                case MouseButtons.MiddleButton:
                    return (_lastMouseState.MiddleButton == ButtonState.Pressed &&
                            _currentMouseState.MiddleButton == ButtonState.Released);
                case MouseButtons.ExtraButton1:
                    return (_lastMouseState.XButton1 == ButtonState.Pressed &&
                            _currentMouseState.XButton1 == ButtonState.Released);
                case MouseButtons.ExtraButton2:
                    return (_lastMouseState.XButton2 == ButtonState.Pressed &&
                            _currentMouseState.XButton2 == ButtonState.Released);
                default:
                    return false;
            }
        }

        /// <summary>
        ///   Checks for a "menu select" input action.
        /// </summary>
        public bool IsMenuSelect() {
            return IsNewKeyPress(Keys.Space) ||
                   IsNewKeyPress(Keys.Enter) ||
                   IsNewButtonPress(Buttons.A) ||
                   IsNewButtonPress(Buttons.Start) ||
                   IsNewMouseButtonPress(MouseButtons.LeftButton);
        }

        public bool IsMenuPressed() {
            return _currentKeyboardState.IsKeyDown(Keys.Space) ||
                   _currentKeyboardState.IsKeyDown(Keys.Enter) ||
                   _currentGamePadState.IsButtonDown(Buttons.A) ||
                   _currentGamePadState.IsButtonDown(Buttons.Start) ||
                   _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool IsMenuReleased() {
            return IsNewKeyRelease(Keys.Space) ||
                   IsNewKeyRelease(Keys.Enter) ||
                   IsNewButtonRelease(Buttons.A) ||
                   IsNewButtonRelease(Buttons.Start) ||
                   IsNewMouseButtonRelease(MouseButtons.LeftButton);
        }

        /// <summary>
        ///   Checks for a "menu cancel" input action.
        /// </summary>
        public bool IsMenuCancel() {
            return IsNewKeyPress(Keys.Escape) ||
                   IsNewButtonPress(Buttons.Back);
        }
    }
}
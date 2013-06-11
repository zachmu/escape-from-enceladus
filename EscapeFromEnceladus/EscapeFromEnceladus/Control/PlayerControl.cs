using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Microsoft.Xna.Framework.Input;

namespace Enceladus.Control
{
    /// <summary>
    /// Abstraction for controlling the player via various means
    /// </summary>    
    public interface IPlayerControl {
        bool IsNewJump();
        bool IsJumpButtonDown();
        bool IsRunButtonDown();
        bool IsNewRunButton();
        bool IsNewShot();
        bool IsNewSonar();
        bool IsNewScooter();
        bool IsScooterButtonDown();
        Direction? GetAimDirection();
        Direction? GetMovementDirection();
        bool IsNewAction();
        bool IsNewPause();
        bool IsNewDirection(out Direction? direction);
        bool IsKeyboardControl();
        bool IsNewConfirmButton();
        bool IsNewCancelButton();
    }

    /// <summary>
    /// Public container for access to current control scheme
    /// </summary>
    public class PlayerControl {
        public static IPlayerControl Control { get; set; }
    }

    /// <summary>
    /// Control scheme using the gamepad
    /// </summary>    
    public class PlayerGamepadControl : IPlayerControl {
        private const Buttons JumpButton = Buttons.A;
        private const Buttons RunButton = Buttons.B;
        private const Buttons ShootButton = Buttons.X;
        private const Buttons SonarButton = Buttons.Y;

        public bool IsNewJump() {
            return InputHelper.Instance.IsNewButtonPress(JumpButton);
        }

        public bool IsJumpButtonDown() {
            return InputHelper.Instance.GamePadState.IsButtonDown(JumpButton);
        }

        public bool IsRunButtonDown() {
            return InputHelper.Instance.GamePadState.IsButtonDown(RunButton);
        }

        public bool IsNewRunButton() {
            return InputHelper.Instance.IsNewButtonPress(RunButton);
        }

        public bool IsNewShot() {
            return InputHelper.Instance.IsNewButtonPress(ShootButton);
        }

        public bool IsNewSonar() {
            return InputHelper.Instance.IsNewButtonPress(SonarButton);
        }

        public bool IsNewScooter() {
            return InputHelper.Instance.IsNewButtonPress(Buttons.LeftTrigger);
        }

        public bool IsScooterButtonDown() {
            return InputHelper.Instance.GamePadState.IsButtonDown(Buttons.LeftTrigger);
        }

        public Direction? GetAimDirection() {
            return InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Right);
        }

        public Direction? GetMovementDirection() {
            return InputHelper.Instance.GetStickDirection(InputHelper.Instance.GamePadState.ThumbSticks.Left);
        }

        public bool IsNewAction() {
            return InputHelper.Instance.IsNewButtonPress(Buttons.Y);
        }

        public bool IsNewPause() {
            return InputHelper.Instance.IsNewButtonPress(Buttons.Back);
        }

        public bool IsNewDirection(out Direction? direction) {
            return InputHelper.Instance.IsGamePadNewDirection(out direction);
        }

        public bool IsKeyboardControl() {
            return false;
        }

        public bool IsNewConfirmButton() {
            return InputHelper.Instance.IsNewButtonPress(Buttons.A);
        }

        public bool IsNewCancelButton() {
            return InputHelper.Instance.IsNewButtonPress(Buttons.B);
        }
    }

    /// <summary>
    /// Control scheme using the keyboard
    /// </summary>
    public class PlayerKeyboardControl : IPlayerControl {
        private const Keys JumpKey = Keys.Z;
        private const Keys RunKey = Keys.LeftShift;
        private const Keys ShootKey = Keys.X;
        private const Keys SonarKey = Keys.C;
        private const Keys ScooterKey = Keys.LeftControl;
        private const Keys ActionKey = Keys.Y;

        public bool IsNewJump() {
            return InputHelper.Instance.IsNewKeyPress(JumpKey);
        }

        public bool IsJumpButtonDown() {
            return InputHelper.Instance.KeyboardState.IsKeyDown(JumpKey);
        }

        public bool IsRunButtonDown() {
            return InputHelper.Instance.KeyboardState.IsKeyDown(RunKey);
        }

        public bool IsNewRunButton() {
            return InputHelper.Instance.IsNewKeyPress(RunKey);
        }

        public bool IsNewShot() {
            return InputHelper.Instance.IsNewKeyPress(ShootKey);
        }

        public bool IsNewSonar() {
            return InputHelper.Instance.IsNewKeyPress(SonarKey);
        }

        public bool IsNewScooter() {
            return InputHelper.Instance.IsNewKeyPress(ScooterKey);
        }

        public bool IsScooterButtonDown() {
            return InputHelper.Instance.KeyboardState.IsKeyDown(ScooterKey);
        }

        public Direction? GetAimDirection() {
            if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Up) ) {
                if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Right) ) {
                    return Direction.UpRight;
                } else if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Left) ) {
                    return Direction.UpLeft;
                } else {
                    return Direction.Up;
                }
            } else if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Down) ) {
                if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Right) ) {
                    return Direction.DownRight;
                } else if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Left) ) {
                    return Direction.DownLeft;
                } else {
                    return Direction.Down;
                }
            }
            return null;
        }

        public Direction? GetMovementDirection() {
            Direction? vertical = GetAimDirection();
            if ( vertical != null )
                return vertical;
            if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Right) ) {
                return Direction.Right;
            } else if ( InputHelper.Instance.KeyboardState.IsKeyDown(Keys.Left) ) {
                return Direction.Left;
            }

            return null;
        }

        public bool IsNewAction() {
            return InputHelper.Instance.IsNewKeyPress(ActionKey);
        }

        public bool IsNewPause() {
            return InputHelper.Instance.IsNewKeyPress(Keys.P);
        }

        public bool IsNewDirection(out Direction? direction) {
            if ( InputHelper.Instance.IsNewKeyPress(Keys.Up) ) {
                direction = Direction.Up;
                return true;
            } else if ( InputHelper.Instance.IsNewKeyPress(Keys.Down) ) {
                direction = Direction.Down;
                return true;
            } else if ( InputHelper.Instance.IsNewKeyPress(Keys.Left) ) {
                direction = Direction.Left;
                return true;
            } else if ( InputHelper.Instance.IsNewKeyPress(Keys.Right) ) {
                direction = Direction.Right;
                return true;
            }
            
            direction = null;
            return false;
        }

        public bool IsKeyboardControl() {
            return true;
        }

        public bool IsNewConfirmButton() {
            return InputHelper.Instance.IsNewKeyPress(Keys.Enter);
        }

        public bool IsNewCancelButton() {
            return InputHelper.Instance.IsNewKeyPress(Keys.Back);
        }
    }
}

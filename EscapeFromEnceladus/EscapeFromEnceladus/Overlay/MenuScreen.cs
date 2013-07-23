using Enceladus.Control;
using Enceladus.Entity;
using Enceladus.Util;
using Microsoft.Xna.Framework;

namespace Enceladus.Overlay {

    /// <summary>
    /// Abstract menu screen with flashing textual cues
    /// </summary>
    public abstract class MenuScreen {

        protected const double MsUntilColorChange = 250;
        protected int _selectedIndex = 0;
        protected Oscillator _flashTimer = new Oscillator(MsUntilColorChange, true);
        protected bool _flash;

        public void UpdateFlashTimer(GameTime gameTime) {
            _flashTimer.Update(gameTime);
            _flash = _flashTimer.IsActiveState;
        }

        protected void HandleMovementControl() {
            Direction? direction;
            if ( PlayerControl.Control.IsNewDirection(out direction) ) {
                switch ( direction ) {
                    case Direction.Up:
                        _selectedIndex = (_selectedIndex - 1) % NumMenuItems;
                        break;
                    case Direction.Down:
                        _selectedIndex = (_selectedIndex + 1) % NumMenuItems;
                        break;
                }
            } else if ( PlayerControl.Control.IsNewConfirmButton() ) {
                ApplyMenuSelection();
            }
        }

        protected abstract int NumMenuItems { get; }

        protected abstract void ApplyMenuSelection();

    }



}
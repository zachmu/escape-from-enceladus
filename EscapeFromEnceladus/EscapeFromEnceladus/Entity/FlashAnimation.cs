using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Util;
using Microsoft.Xna.Framework;

namespace Enceladus.Entity {
    
    /// <summary>
    /// Flash animation component for entities that want to keep track of 
    /// whether they should be flashing some color.
    /// </summary>
    public class FlashAnimation {
        private readonly Color _flashColor = Color.OrangeRed;
        private bool _drawSolidColor;
        private Timer _timer;
        private Oscillator _flash;

        private const double FlashChangeMs = 32;

        /// <summary>
        /// The color of the flash effect, when it's flashing
        /// </summary>
        public Color FlashColor {
            get { return _flashColor; }
        }

        /// <summary>
        /// Returns whether the flash effect should be drawn this frame
        /// </summary>
        public bool IsActive {
            get { return _drawSolidColor; }
        }
        
        /// <summary>
        /// Sets the flash effect to be active for the given millisecond interval
        /// </summary>
        public void SetFlashTime(int flashTimeMs) {
            _timer = new Timer(flashTimeMs);
            _flash = new Oscillator(FlashChangeMs, true);
        }

        public void UpdateFlash(GameTime gameTime) {
            if ( _timer != null && !_timer.IsTimeUp() ) {
                _timer.Update(gameTime);
                _flash.Update(gameTime);
                _drawSolidColor = _flash.IsActiveState;
            } else {
                _drawSolidColor = false;
            }
        }

    }
}

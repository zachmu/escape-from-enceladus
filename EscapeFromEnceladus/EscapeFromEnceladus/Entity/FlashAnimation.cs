using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Enceladus.Entity {
    
    /// <summary>
    /// Flash animation component for entities that want to keep track of 
    /// whether they should be flashing some color.
    /// </summary>
    public class FlashAnimation {
        private readonly Color _flashColor = Color.OrangeRed;
        private bool _drawSolidColor;
        private double _flashTime;
        private double _flashTimer;

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
            _flashTime = flashTimeMs;
            _flashTimer = 0;
        }

        public void UpdateFlash(GameTime gameTime) {
            if ( _flashTime > 0 ) {
                _flashTime -= gameTime.ElapsedGameTime.TotalMilliseconds;
                _flashTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( _flashTimer >= FlashChangeMs ) {
                    _flashTimer %= FlashChangeMs;
                    _drawSolidColor = !_drawSolidColor;
                }
            }
            if ( _flashTime <= 0 ) {
                _drawSolidColor = false;
            }
        }

    }
}

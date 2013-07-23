using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Enceladus.Util {

    /// <summary>
    /// Simple oscillator between states at a time interval of your choosing
    /// </summary>
    public class Oscillator {

        private readonly double _periodMs;
        private double _timer;

        public Oscillator(double periodMs, bool initialState) {
            _periodMs = periodMs;
            IsActiveState = initialState;
            _timer = periodMs;
        }

        public bool IsActiveState { get; private set; }

        public void Update(GameTime gameTime) {
            _timer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer >= _periodMs ) {
                IsActiveState = !IsActiveState;
                _timer %= _periodMs;
            }
        }
    }
}

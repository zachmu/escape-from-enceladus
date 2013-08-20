using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Enceladus.Util {
    
    /// <summary>
    /// Counter state machine. Counts up to a given value, then back to zero.
    /// </summary>
    public class Counter {
        
        private readonly double _periodMs;
        private double _timer;
        private int _numStates;

        public Counter(double periodMs, int numStates) {
            _periodMs = periodMs;
            StateNumber = 0;
            _timer = periodMs;
            _numStates = numStates;
        }

        public int StateNumber { get; private set; }

        public void Update(GameTime gameTime) {
            _timer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer >= _periodMs ) {
                _timer %= _periodMs;
                StateNumber = (StateNumber + 1) % _numStates;
            }
        }
    }
}

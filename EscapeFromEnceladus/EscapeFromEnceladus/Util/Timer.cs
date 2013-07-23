using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Enceladus.Util {
    
    /// <summary>
    /// A timer that will tell you when the time has elapsed.
    /// </summary>
    public class Timer {

        private double _timeTillAlarmMs;

        public Timer(double timeTillAlarmMs) {
            _timeTillAlarmMs = timeTillAlarmMs;
        }

        public void Update(GameTime gameTime) {
            _timeTillAlarmMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
        }

        public bool IsTimeUp() {
            return _timeTillAlarmMs <= 0;
        }
    }
}

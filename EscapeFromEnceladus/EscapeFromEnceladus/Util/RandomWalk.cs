using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enceladus.Util {
 
    /// <summary>
    /// A random walk up and down between a min and max
    /// </summary>
    public class RandomWalk {

        private static readonly Random Random = new Random();

        private float _value;
        private readonly float _stepSize;
        private readonly float _lowThreshold;
        private readonly float _highThreshold;

        public RandomWalk(float initialValue, float stepSize, float lowThreshold, float highThreshold) {
            _value = initialValue;
            _stepSize = stepSize;
            _lowThreshold = lowThreshold;
            _highThreshold = highThreshold;
        }

        public float Value {
            get { return _value; }
        }

        public void Update(float minValue, float maxValue) {
            double rand = Random.NextDouble();
            if ( rand < _lowThreshold && Value > minValue ) {
                _value = Math.Max(minValue, Value - _stepSize);
            } else if ( rand > _highThreshold && Value < maxValue ) {
                _value = Math.Min(maxValue, Value + _stepSize);
            }            
        }

    }
}

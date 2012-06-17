using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {
    /// <summary>
    /// Adjustable constants for game play
    /// </summary>
    public class Constants {

        private static readonly Dictionary<Keys, Constant> ConstantsByKey = new Dictionary<Keys, Constant>();
        private static readonly Dictionary<string, Constant> ConstantsByName = new Dictionary<string, Constant>();

        private static bool _helpMode;

        public static SpriteFont font;

        /// <summary>
        /// Only really useful for using the indexer
        /// </summary>
        private static readonly Constants _instance = new Constants();

        public static Constants Instance {
            get { return _instance; }
        }

        public static void Register(Constant c) {
            if (ConstantsByKey.ContainsKey(c._key)) {
                throw new ArgumentException(String.Format("Duplicate constant key registered: {0}", ConstantsByKey[c._key]));
            }
            if ( ConstantsByName.ContainsKey(c._name) ) {
                throw new ArgumentException(String.Format("Duplicate constant name registered: {0}", ConstantsByName[c._name]));
            }
            ConstantsByKey[c._key] = c;
            ConstantsByName[c._name] = c;
        }

        public float this[string name] {
            get { return Constants.Get(name); }
        }

        public static float Get(String name) {
            return ConstantsByName[name]._value;
        }

        public static void Update(InputHelper input) {
            foreach ( Keys key in input.GetNewKeyPresses() ) {
                if ( ConstantsByKey.ContainsKey(key) ) {
                    if ( input.KeyboardState.IsKeyDown(Keys.LeftShift) || input.KeyboardState.IsKeyDown(Keys.RightShift) ) {
                        ConstantsByKey[key].Decrement();
                    } else {
                        ConstantsByKey[key].Increment();
                    }
                }
                if ( key == Keys.RightAlt ) {
                    _helpMode = !_helpMode;
                }
            }

            foreach (
                Constant c in
                    ConstantsByKey.Values.Where(
                        constant => DateTime.Now.Ticks - Constant.DisplayTime > constant.LastDisplayTime) ) {
                c.Visible = false;
            }
        }


        public static void Draw(SpriteBatch batch) {
            int i = 2;
            foreach ( Constant c in ConstantsByKey.Values.Where(constant => _helpMode || constant.Visible) ) {
                batch.DrawString(font, c.ToString(), new Vector2(40f, i++ * 20), Color.GreenYellow);
            }
        }
    }

    public class Constant {
        internal string _name;
        internal float _value;
        internal Keys _key;

        public float Value {
            get { return _value; }
        }

        internal bool Visible { get; set; }
        internal long LastDisplayTime { get; set; }

        public Constant(string name, float value, Keys key) {
            _name = name;
            _value = value;
            _key = key;
        }

        internal static readonly long DisplayTime = 10000 * 2000; // 2 seconds in ticks

        public void Decrement() {
            _value -= .1f;
            LastDisplayTime = DateTime.Now.Ticks;
            Visible = true;
        }

        public void Increment() {
            _value += .1f;
            LastDisplayTime = DateTime.Now.Ticks;
            Visible = true;
        }

        public override string ToString() {
            return String.Format("{0}:{1:0.##} ({2})", _name, _value, _key);
        }
    }
}

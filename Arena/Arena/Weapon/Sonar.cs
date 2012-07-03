using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using FarseerPhysics.Collision.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Weapon {

    /// <summary>
    /// The sonar weapon.
    /// </summary>
    internal class Sonar : IGameEntity {

        static Sonar() {
            Constants.Register(new Constant(WaveTime, 1f, Keys.W));
        }

        private bool _disposed;
        public void Dispose() {
            _disposed = true;
        }

        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position {
            get { return _waveEffectCenter; }
        }

        public PolygonShape Shape {
            get { throw new NotImplementedException(); }
        }

        public static void LoadContent(ContentManager cm) {
            _waveEffect = cm.Load<Effect>("wave");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 screenPos = camera.ConvertWorldToScreen(_waveEffectCenter);
            Vector2 center = new Vector2(screenPos.X / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferWidth,
                                         screenPos.Y / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferHeight);
            _waveEffect.Parameters["Center"].SetValue(center);
        }

        private static Effect _waveEffect;
        private int _waveTimeMs = -1;
        private Direction _direction;
        private readonly Vector2 _waveEffectCenter;
        private const string WaveTime = "Wave effect travel time";

        public Sonar(Vector2 waveEffectCenter, Direction direction) {
            _waveEffectCenter = waveEffectCenter;
            _direction = direction;
            Arena.Instance.Register(new WaveEffect(this));
        }

        public void Update(GameTime gameTime) {
            _waveTimeMs += gameTime.ElapsedGameTime.Milliseconds;

            if ( _waveTimeMs > Constants.Get(WaveTime) * 1000 ) {
                _waveTimeMs = -1;
                _disposed = true;
            }
        }

        private void TunEffect() {
            _waveEffect.Parameters["Direction"].SetValue((int) _direction);
            _waveEffect.Parameters["Radius"].SetValue(_waveTimeMs / Constants.Get(WaveTime) / 1000f);            
        }

        private class WaveEffect : PostProcessingEffect {
            private readonly Sonar _sonar;

            public WaveEffect(Sonar sonar) {
                _sonar = sonar;
            }

            public override bool Disposed {
                get { return _sonar.Disposed; }
            }

            public override Effect Effect {
                get { return _waveEffect; }
            }

            public override void SetEffectParameters() {
                _sonar.TunEffect();
            }
        }
    }
}

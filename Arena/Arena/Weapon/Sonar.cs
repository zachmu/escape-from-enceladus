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
            // nothing to draw, just a post-processing effect
        }

        private static Effect _waveEffect;
        private int _waveTimeMs = -1;
        private readonly double _angle;
        private readonly Vector2 _waveEffectCenter;
        private const string WaveTime = "Wave effect travel time";

        public Sonar(Vector2 waveEffectCenter, Direction direction) {
            _waveEffectCenter = waveEffectCenter;
            switch (direction) {
                case Direction.Left:
                    _angle = Math.PI;
                    break;
                case Direction.Right:
                    _angle = 0;
                    break;
                case Direction.Up:
                    _angle = Math.PI / 2;
                    break;
                case Direction.Down:
                    _angle = -Math.PI / 2;
                    break;
                case Direction.UpLeft:
                    _angle = 3f * Math.PI / 4f;
                    break;
                case Direction.UpRight:
                    _angle = Math.PI / 4f;
                    break;
                case Direction.DownLeft:
                    _angle = -3f * Math.PI / 4f;
                    break;
                case Direction.DownRight:
                    _angle = -Math.PI / 4f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
            Arena.Instance.Register(new WaveEffect(this));
        }

        public void Update(GameTime gameTime) {
            _waveTimeMs += gameTime.ElapsedGameTime.Milliseconds;

            if ( _waveTimeMs > Constants.Get(WaveTime) * 1000 ) {
                _waveTimeMs = -1;
                _disposed = true;
            }
        }

        private void TuneEffect(Camera2D camera, SpriteBatch spriteBatch) {
            Vector2 screenPos = camera.ConvertWorldToScreen(_waveEffectCenter);
            Vector2 center = new Vector2(screenPos.X / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferWidth,
                                         screenPos.Y / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferHeight);

            _waveEffect.Parameters["Center"].SetValue(center);
            _waveEffect.Parameters["DirectionAngle"].SetValue((float) _angle);
            _waveEffect.Parameters["Radius"].SetValue(_waveTimeMs / Constants.Get(WaveTime) / 1000f);

            // TODO: move this into initialization
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height, 0, 0, 1);
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            _waveEffect.Parameters["MatrixTransform"].SetValue(halfPixelOffset * projection);
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

            public override void SetEffectParameters(Camera2D camera, SpriteBatch spriteBatch) {
                _sonar.TuneEffect(camera, spriteBatch);
            }
        }
    }
}

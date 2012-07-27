using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Weapon {

    /// <summary>
    /// The sonar weapon.
    /// </summary>
    public class Sonar : IGameEntity {

        private const string WaveSpeed = "Wave speed (m/s)";
        private const string SonarLocationDebug = "Sonar location debug";

        static Sonar() {
            Constants.Register(new Constant(WaveSpeed, 12.0f, Keys.Q));
            Constants.Register(new Constant(SonarLocationDebug, .9f, Keys.D0));
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

        private static Texture2D _debugLocation;
        private static SoundEffect _ping;
        private static SoundEffect _pong;

        public static void LoadContent(ContentManager cm) {
            _waveEffect = cm.Load<Effect>("wave");
            _debugLocation = cm.Load<Texture2D>("welcome16");
            _pong = cm.Load<SoundEffect>("SonarHit");
            _ping = cm.Load<SoundEffect>("SonarPing");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( Constants.Get(SonarLocationDebug) >= 1 ) {
                foreach ( FoundDestructibleRegion region in _foundDestructionRegions.Values ) {
                    foreach ( Vector2 location in region._locations ) {
                        Vector2 displayPosition = ConvertUnits.ToDisplayUnits(location);
                        spriteBatch.Draw(_debugLocation, displayPosition, Color.White);
                    }
                }
            }
        }

        private static Effect _waveEffect;
        private int _waveTimeMs = 0;
        private double _angle;
        private readonly Vector2 _waveEffectCenter;

        private const float WaveAngleWidth = (float) (Math.PI / 8f);
        private const int NumScanLines = 20;
        private const float ScanRadius = 16;

        public Sonar(World world, Vector2 waveEffectCenter, Direction direction) {
            _waveEffectCenter = waveEffectCenter;
            DetermineAngle(direction);
            DetermineIntersectedRegions(world, waveEffectCenter);
            Arena.Instance.Register(new WaveEffect(this));
            _ping.Play();
        }

        private void DetermineIntersectedRegions(World world, Vector2 waveEffectCenter) {
            for ( int i = 0; i < NumScanLines; i++ ) {
                Vector2 terminus = waveEffectCenter;
                double angle = _angle + ((i - (NumScanLines / 2f)) / (NumScanLines / 2) * (WaveAngleWidth / 2));
                terminus.X += (float) Math.Cos(angle) * ScanRadius;
                terminus.Y -= (float) Math.Sin(angle) * ScanRadius;

                world.RayCast(FindDestructibleSurfaces, waveEffectCenter, terminus);
            }
        }

        private void DetermineAngle(Direction direction) {
            switch ( direction ) {
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
        }

        private readonly Dictionary<FoundDestructibleRegion, FoundDestructibleRegion> _foundDestructionRegions =
            new Dictionary<FoundDestructibleRegion, FoundDestructibleRegion>();

        private float FindDestructibleSurfaces(Fixture fixture, Vector2 point, Vector2 normal, float fraction) {
            if ( fixture.GetUserData().IsDestructibleRegion && TileLevel.CurrentLevel.IsLiveTile(point) ) {
                FoundDestructibleRegion foundDestructibleRegion = new FoundDestructibleRegion(fraction * ScanRadius,
                                                                               fixture.Body.GetUserData().Destruction);
                if ( _foundDestructionRegions.ContainsKey(foundDestructibleRegion) ) {
                    foundDestructibleRegion = _foundDestructionRegions[foundDestructibleRegion];
                } else {
                    _foundDestructionRegions.Add(foundDestructibleRegion, foundDestructibleRegion);
                }
                foundDestructibleRegion._locations.Add(point);
            }
            return -1;
        }

        public void Update(GameTime gameTime) {
            _waveTimeMs += gameTime.ElapsedGameTime.Milliseconds;

            float currentRadius = GetCurrentRadius();

            // TODO: different sounds for different types of regions
            foreach (
                FoundDestructibleRegion region in
                    _foundDestructionRegions.Values.Where(region => region._radius <= currentRadius).ToList() ) {
                _pong.Play();
                _foundDestructionRegions.Remove(region);
            }

            if ( currentRadius >= ScanRadius ) {
                _disposed = true;
            }
        }

        private float GetCurrentRadius() {
            return Constants.Get(WaveSpeed) * _waveTimeMs / 1000f;
        }

        private void TuneEffect(Camera2D camera, SpriteBatch spriteBatch) {
            Vector2 screenPos = camera.ConvertWorldToScreen(_waveEffectCenter);
            Vector2 center = new Vector2(
                screenPos.X / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferWidth,
                screenPos.Y / spriteBatch.GraphicsDevice.PresentationParameters.BackBufferHeight);

            _waveEffect.Parameters["Center"].SetValue(center);
            _waveEffect.Parameters["DirectionAngle"].SetValue((float) _angle);
            _waveEffect.Parameters["Radius"].SetValue(ConvertUnits.ToDisplayUnits(GetCurrentRadius()) /
                                                      spriteBatch.GraphicsDevice.PresentationParameters.BackBufferWidth *
                                                      2);

            // TODO: move this into initialization
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, spriteBatch.GraphicsDevice.Viewport.Width,
                                                                   spriteBatch.GraphicsDevice.Viewport.Height, 0, 0, 1);
            Matrix halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
            _waveEffect.Parameters["MatrixTransform"].SetValue(halfPixelOffset * projection);
        }

        internal class FoundDestructibleRegion : IEquatable<FoundDestructibleRegion> {
            internal float _radius;
            internal readonly DestructionRegion _region;
            internal HashSet<Vector2> _locations = new HashSet<Vector2>();

            public FoundDestructibleRegion(float radius, DestructionRegion region) {
                _radius = radius;
                _region = region;
            }

            public bool Equals(FoundDestructibleRegion other) {
                if ( ReferenceEquals(null, other) ) {
                    return false;
                }
                if ( ReferenceEquals(this, other) ) {
                    return true;
                }
                return Equals(other._region, _region);
            }

            public override bool Equals(object obj) {
                if ( ReferenceEquals(null, obj) ) {
                    return false;
                }
                if ( ReferenceEquals(this, obj) ) {
                    return true;
                }
                if ( obj.GetType() != typeof ( FoundDestructibleRegion ) ) {
                    return false;
                }
                return Equals((FoundDestructibleRegion) obj);
            }

            public override int GetHashCode() {
                return (_region != null ? _region.GetHashCode() : 0);
            }

            public static bool operator ==(FoundDestructibleRegion left, FoundDestructibleRegion right) {
                return Equals(left, right);
            }

            public static bool operator !=(FoundDestructibleRegion left, FoundDestructibleRegion right) {
                return !Equals(left, right);
            }
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

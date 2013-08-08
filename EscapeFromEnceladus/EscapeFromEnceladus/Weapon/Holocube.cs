using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// A holocube placement "weapon"
    /// </summary>
    public class Holocube : GameEntityAdapter, IWeapon {

        private enum Mode {
            Projection,
            Creation,
            Solid,
            Dissolving
        }

        private Vector2 _start;
        private float _angle;
        private Vector2 _end;
        private Vector2 _cubeCorner;
        private Direction _direction;
        private World _world;
        private static Texture2D _image;
        private float _alpha;
        private bool _projecting;

        private const int MaxRange = 30;
        private const float MinAlpha = .2f;
        private const float MaxAlpha = .9f;

        public static void LoadContent(ContentManager cm) {
            _image = cm.Load<Texture2D>("Projectile/Holocube0000");
        }

        public Holocube(World world, Vector2 start, Direction direction) {
            UpdateProjection(world, start, direction);
        }

        public void UpdateProjection(World world, Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
            _world = world;
            _alpha = MinAlpha;
            DetermineLength();
            UpdateAlpha();
        }

        private static Random _random = new Random();
        // Simple randomized flicker effect
        private void UpdateAlpha() {
            double rand = _random.NextDouble();
            /*
            if ( rand > .9f ) {
                _alpha = (float)_random.NextDouble();
                if ( _alpha < MinAlpha ) {
                    _alpha = MinAlpha;
                } else if ( _alpha > MaxAlpha ) {
                    _alpha = MaxAlpha;
                }
            }
             * */
            if ( rand < .2f && _alpha > MinAlpha) {
                _alpha = Math.Max(MinAlpha, _alpha - .1f);
            } else if ( rand > .8f && _alpha < MaxAlpha ) {
                _alpha = Math.Min(MaxAlpha, _alpha + .1f);
            }
        }

        private void DetermineLength() {

            // Don't forget to invert the y coordinate because of the differing y axes
            Vector2 diff = new Vector2((float) Math.Cos(_angle) * MaxRange, (float) -Math.Sin(_angle) * MaxRange);
            Vector2 end = _start + diff;
            Vector2 angle = diff;
            angle.Normalize();

            float closestFraction = 1;
            Vector2 closestPoint = end;
            Vector2 closestCorner = end;
            _projecting = false;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( ((fixture.GetUserData().IsDoor && !fixture.GetUserData().Door.IsOpen())
                      || fixture.GetUserData().IsTerrain) && fraction < closestFraction ) {
                    closestFraction = fraction;
                    closestPoint = point;
                    // back up the ray just a tad
                    Vector2 less = _start + (diff * fraction) - (angle * .02f);
                    closestCorner = Region.GetContainingTile(less);
                    _projecting = true;
                }
                return 1;
            }, _start, end);

            _end = closestPoint;
            _cubeCorner = closestCorner;
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {            
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
            spriteBatch.Draw(_image, displayPosition, null, SolidColorEffect.DisabledColor * _alpha,
                             0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);
        }


        public int DestructionFlags {
            get { return EnceladusGame.HolocubeDestructionFlag; }
        }

        public float BaseDamage {
            get { return 1; }
        }
    }
}

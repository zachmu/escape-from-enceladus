using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.Enemy {
    
    class Worm : PacingEnemy {

        private int _animationCounter = 0;
        private int _halfCycleTimeMs = 750;

        private const float BaseWidth = 2;
        private const float BaseHeight = .55f;

        private const float MinWidth = 1;
        private const float MaxWidth = 3;
        private const float MinHeight = .3f;
        private const float MaxHeight = .8f;

        private const float HeightDiff = MaxHeight - MinHeight;
        private const float WidthDiff = MaxWidth - MinWidth;

        private float _width = BaseWidth;
        private float _height = BaseHeight;
        private Mode _mode = Mode.Stretch;

        private enum Mode {
            Stretch, Shrink
        }

        public Worm(Vector2 position, World world) : base(position, world, MaxWidth, MinHeight) {
        }

        private static Texture2D WormImage;
        public static void LoadContent(ContentManager content) {
            WormImage = content.Load<Texture2D>("Enemy/worm0000");
        }

        protected override Texture2D Image {
            get { return WormImage; }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // draw position is character's lower-left
                Vector2 position = _body.Position;
                position.Y += _height / 2f;
                position.X -= _width / 2f;

                Vector2 scale = new Vector2(_width / BaseWidth, _height / BaseHeight);
                Vector2 origin = new Vector2(1, Image.Height);

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _drawSolidColor ? _flashColor : SolidColorEffect.DisabledColor;

                spriteBatch.Draw(Image, displayPosition, null, color, 0f, origin, scale,
                                 _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // we don't need velocity -- we creep
            _body.LinearVelocity = new Vector2(0f, _body.LinearVelocity.Y);

            _animationCounter += (int) gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _animationCounter > _halfCycleTimeMs ) {
                if ( _mode == Mode.Stretch ) {
                    _mode = Mode.Shrink;
                } else {
                    _mode = Mode.Stretch;
                }
                _animationCounter = 0;
            }

            float warpDegree = (float) _animationCounter / (float) _halfCycleTimeMs;
            if ( _mode == Mode.Stretch ) {
                ResizeBody(MinWidth + warpDegree * WidthDiff, MaxHeight - warpDegree * HeightDiff);
            } else {
                ResizeBody(MaxWidth - warpDegree * WidthDiff, MinHeight + warpDegree * HeightDiff);
            }

            // roomba cliff sensor
            Vector2 frontEdge = _body.Position +
                                new Vector2(_direction == Direction.Right ? _width / 2 : -_width / 2, _height / 2);
            bool cliffSensed = true;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor ) {
                    cliffSensed = false;
                    return 0;
                }
                return -1;
            }, frontEdge, frontEdge + new Vector2(0, .5f));

            if ( cliffSensed ) {
                if ( _direction == Direction.Right ) {
                    _direction = Direction.Left;
                } else {
                    _direction = Direction.Right;
                }
            }
        }

        /// <summary>
        /// Resizes the body while keeping the lower edge in the same position and the X position constant.
        /// </summary>
        private void ResizeBody(float width, float height) {    
            float halfHeight = height / 2;
            var newPosition = GetNewBodyPosition(halfHeight, width / 2f, Vector2.Zero);
            _body.Position = newPosition;

            PolygonShape shape = (PolygonShape) _body.FixtureList.First().Shape;
            shape.SetAsBox(width / 2, halfHeight);
            _height = height;
            _width = width;
        }

        /// <summary>
        /// Returns the position of the body if the half-height is as indicated, 
        /// holding either the back corner or the front corner constant
        /// </summary>
        private Vector2 GetNewBodyPosition(float halfHeight, float halfWidth, Vector2 positionCorrection) {
            Vector2 position = _body.Position;
            float oldYPos = position.Y + _height / 2;
            float newYPos = position.Y + halfHeight;

            if ( _mode == Mode.Stretch ) {
                // hold back end steady
                if ( _direction == Direction.Right ) {
                    Vector2 backEnd = position + new Vector2(-_width / 2, +_height / 2);
                    return new Vector2(backEnd.X + halfWidth, position.Y + (oldYPos - newYPos)) + positionCorrection;
                } else {
                    Vector2 backEnd = position + new Vector2(_width / 2, +_height / 2);
                    return new Vector2(backEnd.X - halfWidth, position.Y + (oldYPos - newYPos)) + positionCorrection;
                }
            } else {
                // hold front end steady
                if ( _direction == Direction.Right ) {
                    Vector2 frontEnd = position + new Vector2(_width / 2, +_height / 2);
                    return new Vector2(frontEnd.X - halfWidth, position.Y + (oldYPos - newYPos)) + positionCorrection;
                } else {
                    Vector2 frontEnd = position + new Vector2(-_width / 2, +_height / 2);
                    return new Vector2(frontEnd.X + halfWidth, position.Y + (oldYPos - newYPos)) + positionCorrection;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {

    public class SkullBeetle : AbstractWalkingEnemy {

        private static readonly float Height = ConvertUnits.ToSimUnits(64f);
        private static readonly float Width = ConvertUnits.ToSimUnits(112f);
        private const float LegImageHeight = 16;
        private static readonly float LegHeight = ConvertUnits.ToSimUnits(LegImageHeight);
        private static readonly float Radius = Width / 2f;

        private const int NumFrames = 7;
        private const int TurningFrame = 7;
        private const double FrameTimeMs = 50;
        private const double TurnTimeMs = 200;

        private static Texture2D _image;
        private int _animationFrame = NumFrames - 1;
        private double _timeSinceLastUpdate;

        private static readonly Rectangle[] Sprites = new Rectangle[NumFrames + 1];

        private enum Mode {
            Walking, Turning, Turned,
        }

        private Mode _mode;

        static SkullBeetle() {
            const int marginLeft = 117;
            const int marginTop = 2;
            const int padding = 16;
            const int width = 112;
            const int height = 64;
            const int turningMarginLeft = 1;
            const int turningWidth = 88;

            for ( int i = 0; i < NumFrames; i++ ) {
                Sprites[i] = new Rectangle(marginLeft + width * i + padding * i, marginTop, width, height);
            }
            Sprites[TurningFrame] = new Rectangle(turningMarginLeft, marginTop, turningWidth, height);
        }
       
        private int LinearVelocity = 2;
        private SpriteBatch _spriteBatch;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                _image = content.Load<Texture2D>("Enemy/Beetle/beetleSheet");
            }
        }

        public SkullBeetle(Vector2 position, World world)
            : base(position, world, Width, Height) {
            _hitPoints = 10;
            _mode = Mode.Walking;
        }

        protected override void CreateBody(Vector2 position, World world, float width, float height) {
            _body = BodyFactory.CreateSolidArc(world, 1f, Projectile.SevenPiOverEight, 12, Radius, new Vector2(0, -LegHeight / 4), (float) Math.PI);
            FixtureFactory.AttachRectangle(Width - .05f, LegHeight, 1f, new Vector2(0, -LegHeight / 2), _body);
        }

        protected override void ConfigureBody(Vector2 position, float height) {
            base.ConfigureBody(position, height);
            _body.IgnoreGravity = false;
            _body.Friction = .5f;
            _body.Position = position - new Vector2(0, LegHeight);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            UpdateAnimation(gameTime);
            if ( _standingMonitor.IsStanding ) {
                if ( _direction == Direction.Left ) {
                    _body.LinearVelocity = new Vector2(-LinearVelocity, 0);
                } else {
                    _body.LinearVelocity = new Vector2(LinearVelocity, 0);
                }
            }
        }

        private void UpdateAnimation(GameTime gameTime) {
            _timeSinceLastUpdate += (int) gameTime.ElapsedGameTime.TotalMilliseconds;
            switch ( _mode ) {
                case Mode.Walking:
                    if ( _timeSinceLastUpdate > FrameTimeMs ) {
                        _animationFrame = (_animationFrame + 1) % NumFrames;
                        _timeSinceLastUpdate %= FrameTimeMs;
                    }
                    break;
                case Mode.Turning:
                    _animationFrame = TurningFrame;
                    if ( _timeSinceLastUpdate >= TurnTimeMs / 2 ) {
                        _mode = Mode.Turned;
                    }
                    break;
                case Mode.Turned:
                    _animationFrame = TurningFrame;
                    if ( _timeSinceLastUpdate >= TurnTimeMs ) {
                        _mode = Mode.Walking;
                        _timeSinceLastUpdate = 0;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // draw position is character's feet
                Vector2 position = _body.Position;

                //Vector2 origin = new Vector2();
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position) + new Vector2(0, -LegImageHeight + 2);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : SolidColorEffect.DisabledColor;
                
                Draw(spriteBatch, displayPosition, color);

                _spriteBatch = spriteBatch;
            }
        }

        private void Draw(SpriteBatch spriteBatch, Vector2 displayPosition, Color color) {
            Rectangle sourceRectangle = Sprites[_animationFrame];
            Vector2 origin = new Vector2(sourceRectangle.Width / 2, sourceRectangle.Height - LegImageHeight);
            var flip = IsFlippedHorizontally();
            spriteBatch.Draw(Image, displayPosition, sourceRectangle, color, _body.Rotation, origin, 1f,
                             flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        private bool IsFlippedHorizontally() {
            bool flip = false;
            switch ( _mode ) {
                case Mode.Walking:
                case Mode.Turned:
                    flip = _direction != Direction.Right;
                    break;
                case Mode.Turning:
                    flip = _direction != Direction.Left;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return flip;
        }

        protected override void Destroyed() {
            Dispose();

            // Draw ourselves onto a buffer to make the shatter image
            GraphicsDevice graphics = _spriteBatch.GraphicsDevice;
            Rectangle sprite = Sprites[_animationFrame];
            RenderTarget2D renderTarget = new RenderTarget2D(graphics,
                                                             sprite.Width,
                                                             sprite.Height);
            graphics.SetRenderTarget(renderTarget);
            graphics.Clear(Color.Transparent);
            _spriteBatch.Begin();
            Vector2 displayPosition = new Vector2(sprite.Width / 2f, sprite.Height - LegImageHeight);
            Draw(_spriteBatch, displayPosition, Color.White);
            _spriteBatch.End();
            graphics.SetRenderTarget(null);

            ShatterAnimation shatterAnimation = new ShatterAnimation(_world, renderTarget, SolidColorEffect.DisabledColor, null, GetStandingLocation() - new Vector2(0, Height / 2f + LegHeight), 4, 10);
            EnceladusGame.Instance.Register(shatterAnimation);

            SoundEffectManager.Instance.PlaySoundEffect("enemyExplode");
        }

        protected override Texture2D Image {
            get { return _image; } // TODO: draw only rectangle
            set { _image = value; }
        }

        /// <summary>
        /// Event handler for when the beetle runs into a solid object
        /// </summary>
        protected override void HitSolidObject(Contact contact, Fixture b) {
            if ( b.Body.GetUserData().IsPlayer || b.Body.GetUserData().IsTerrain || b.Body.GetUserData().IsDoor ) {
                if ( contact.GetPlayerNormal(_body).X > .9 && _direction == Direction.Left ) {
                    _direction = Direction.Right;
                    Turning();
                } else if ( contact.GetPlayerNormal(_body).X < -.9 && _direction == Direction.Right ) {
                    _direction = Direction.Left;
                    Turning();
                }
            }
        }

        private void Turning() {
            _mode = Mode.Turning;
            _timeSinceLastUpdate = 0;
        }

        /// <summary>
        /// Returns whether there is a cliff directly underneath the body.
        /// </summary>
        private bool CliffSensed(Vector2 underneath) {
            Vector2 cliffSensor = _body.GetWorldPoint(new Vector2(0, -.1f));
            bool cliffSensed = true;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor && !fixture.IsSensor ) {
                    cliffSensed = false;
                    return 0;
                }
                return -1;
            }, cliffSensor, cliffSensor + underneath);
            return cliffSensed;
        }

        protected override Vector2 GetStandingLocation() {
            return _body.GetWorldPoint(new Vector2(0, LegHeight));
        }

        public override int BaseDamage {
            get { return 10; }
        }
    }
}

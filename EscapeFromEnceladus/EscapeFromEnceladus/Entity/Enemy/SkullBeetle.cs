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

        private static Texture2D _image;
        private int _animationFrame = 0;
        private int _timeSinceLastUpdate;

        private static readonly float Height = ConvertUnits.ToSimUnits(64f);
        private static readonly float Width = ConvertUnits.ToSimUnits(112f);
        private static readonly float LegHeight = ConvertUnits.ToSimUnits(8f);
        private static readonly float Radius = Width / 2f;

        private const int NumFrames = 7;
        private const int TurningFrame = 7;
        private static readonly Rectangle[] Sprites = new Rectangle[NumFrames + 1];

        static SkullBeetle() {
            int marginLeft = 117;
            int marginTop = 2;
            int padding = 16;
            int width = 112;
            int height = 64;

            for ( int i = 0; i < NumFrames; i++ ) {
                Sprites[i] = new Rectangle(marginLeft + width * i + padding * i, marginTop, width, height);
            }
            Sprites[TurningFrame] = new Rectangle(marginLeft, marginTop, 88, height);
        }
       
        private int LinearVelocity = 2;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                _image = content.Load<Texture2D>("Enemy/Beetle/beetleSheet");
            }
        }

        public SkullBeetle(Vector2 position, World world)
            : base(position, world, Width, Height) {
            _hitPoints = 10;
        }

        protected override void CreateBody(Vector2 position, World world, float width, float height) {
            _body = BodyFactory.CreateSolidArc(world, 1f, Projectile.SevenPiOverEight, 12, Radius, Vector2.Zero, (float) Math.PI);
            FixtureFactory.AttachRectangle(Width - .05f, LegHeight, 1f, new Vector2(0, -LegHeight / 2), _body);
        }

        protected override void ConfigureBody(Vector2 position, float height) {
            base.ConfigureBody(position, height);
            _body.IgnoreGravity = false;
            _body.Friction = .5f;
            _body.Position = position;
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
            if (_timeSinceLastUpdate > 20) {
                _animationFrame = (_animationFrame + 1) % NumFrames;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // draw position is character's feet
                Vector2 position = _body.Position;

                //Vector2 origin = new Vector2();
                Rectangle sourceRectangle = Sprites[_animationFrame];
                Vector2 origin = new Vector2(sourceRectangle.Width / 2, sourceRectangle.Height - 8);

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : SolidColorEffect.DisabledColor;

                spriteBatch.Draw(Image, displayPosition, sourceRectangle, color, _body.Rotation, origin, 1f,
                                 _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        protected override Texture2D Image {
            get { return _image; } // TODO: draw only rectangle
            set { _image = value; }
        }

        /// <summary>
        /// Event handler for when the beetle runs into a solid object
        /// </summary>
        protected override void HitSolidObject(FarseerPhysics.Dynamics.Contacts.Contact contact, Fixture b) {
            if ( b.Body.GetUserData().IsPlayer || b.Body.GetUserData().IsTerrain || b.Body.GetUserData().IsDoor ) {
                if ( contact.Manifold.LocalNormal.X > .9 ) {
                    _direction = Direction.Right;
                } else if ( contact.Manifold.LocalNormal.X < -.9 ) {
                    _direction = Direction.Left;
                }
            }
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

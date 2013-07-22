using System.Linq;
using Enceladus.Weapon;
using Enceladus.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Enceladus.Entity.Enemy {

    public class PacingEnemy : AbstractEnemy, IGameEntity {

        static PacingEnemy() {
            Constants.Register(new Constant(EnemySpeed, 3f, Keys.E));
        }

        public PacingEnemy(Vector2 position, World world)
            : this(position, world, 1f, 1f) {
        }

        public PacingEnemy(Vector2 position, World world, float width, float height)
            : base(position, world, width, height) {
        }

        private static Texture2D DefaultImage;

        public static void LoadContent(ContentManager content) {
            DefaultImage = content.Load<Texture2D>("Enemy/enemy");
        }

        protected override Texture2D Image {
            get { return DefaultImage; }
            set { throw new System.NotImplementedException(); }
        }

        protected override void HitSolidObject(FarseerPhysics.Dynamics.Contacts.Contact contact, Fixture b) {
            if ( b.Body.GetUserData().IsPlayer || b.Body.GetUserData().IsTerrain || b.Body.GetUserData().IsDoor ) {
                if ( contact.Manifold.LocalNormal.X > .9 ) {
                    _direction = Direction.Right;
                } else if ( contact.Manifold.LocalNormal.X < -.9 ) {
                    _direction = Direction.Left;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                Vector2 position = _body.Position;
                position.X -= 1 / 2f;
                position.Y -= 1 / 2f;

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : SolidColorEffect.DisabledColor;
                spriteBatch.Draw(Image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width,
                                               Image.Height),
                                 null, color, 0f, new Vector2(),
                                 _direction == Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if ( IsStanding ) {
                if ( _direction == Direction.Left ) {
                    _body.LinearVelocity = new Vector2(-Constants.Get(EnemySpeed), 0);
                } else {
                    _body.LinearVelocity = new Vector2(Constants.Get(EnemySpeed), 0);
                }
            }
        }

        protected override Vector2 GetStandingLocation() {
            return _body.Position + new Vector2(0, _height/2);
        }
    }

}

using System.Linq;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Entity.Enemy {
    
    public class PacingEnemy : AbstractEnemy, IGameEntity {

        static PacingEnemy() {
            Constants.Register(new Constant(EnemySpeed, 3f, Keys.E));
        }

        public PacingEnemy(Vector2 position, World world) : this(position, world, 1f, 1f) {
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
        }

        public override float CharacterWidth {
            get { return 1f; }
        }

        public override float CharacterHeight {
            get { return 1f; }
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

    }
}

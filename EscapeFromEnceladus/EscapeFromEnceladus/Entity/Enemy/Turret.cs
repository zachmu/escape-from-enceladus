using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {
    public class Turret : GameEntityAdapter, IGameEntity {

        protected const int NumFrames = 4;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
        private static readonly int Barrel = 0;
        private static readonly int Cover = 1;
        private static readonly int WeakSpot = 2;
        private static readonly int Hatch = 3;
        private static readonly int ImageHeight = 64;
        private static readonly int ImageWidth = 64;

        private const float Height = 1f;
        private const float Width = .5f;
        private const float Radius = Width;

        private Body _body;
        private Direction _facingDirection;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Enemy/Turret/Turret{0:0000}", i));
            }           
        }

        public Turret(Vector2 position, World world, Direction facing) {
            _facingDirection = facing;
            
            _body = BodyFactory.CreateSolidArc(world, 1f, (float) Math.PI, 8, Radius, Vector2.Zero, Projectile.PiOverTwo);

            switch ( _facingDirection ) {
                case Direction.Left:
                    position += new Vector2(0, Height / 2);
                    break;
                case Direction.Right:
                    position += new Vector2(0, Height / 2);
                    _body.Rotation = (float) (-Math.PI);
                    break;
                case Direction.Up:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = Projectile.PiOverTwo;
                    break;
                case Direction.Down:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = -Projectile.PiOverTwo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _body.IsStatic = true;
            _body.IgnoreGravity = true;
            _body.Position = position;
            _body.CollisionCategories = EnceladusGame.EnemyCategory;
            _body.CollidesWith = Category.All;
        }

        public override void Dispose() {
            _body.Dispose();
        }

        public override bool Disposed {
            get { return _body.IsDisposed; }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 position = _body.Position;

            Vector2 origin = new Vector2(ImageWidth, ImageHeight / 2f);

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            Color color = SolidColorEffect.DisabledColor;

            float bodyRotation = -_body.Rotation;
            if ( _facingDirection == Direction.Up || _facingDirection == Direction.Down ) {
                bodyRotation = _body.Rotation;
            }
            spriteBatch.Draw(Animation[Barrel], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[WeakSpot], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Hatch], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Cover], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
        }

        public override Vector2 Position {
            get { return _body.Position; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {

    /// <summary>
    /// NPC character that you can talk to.
    /// </summary>
    public class NPC : Region, IGameEntity {

        private bool _disposed;

        public void Dispose() {
            _disposed = true;
        }

        private const int NumUnarmedWalkFrames = 27;
        private const float Height = 1.9f;

        private static Texture2D[] WalkAnimation;
        private static Texture2D Stand;
        private static Texture2D YButton { get; set; }
        private Texture2D Image { get; set; }

        private Body _body;
        private Direction _facingDirection = Direction.Left;
        private Color _color;

        public NPC(Color color, Vector2 topLeft, Vector2 bottomRight, World world) {
            _topLeft = topLeft;
            _bottomRight = bottomRight;
            _color = color;

            _body = BodyFactory.CreateRectangle(world, .6f, Height, 0f);
            _body.IsStatic = false;
            _body.CollisionCategories = Arena.NPCCategory;
            _body.CollidesWith = Arena.TerrainCategory;
            _body.Position = Position;

            Image = Stand;
        }

        public bool Disposed {
            get { return _disposed; }
        }

        public static void LoadContent(ContentManager content) {
            WalkAnimation = Player.Instance._unarmedWalkAnimation;
            Stand = Player.Instance._unarmedStandFrame;
            YButton = content.Load<Texture2D>("ButtonImages/xboxControllerButtonY");
        }

        public PolygonShape Shape {
            get { return null; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is character's feet
            Vector2 position = _body.Position;
            position.Y += Height / 2;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Color color = _color;
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, color, 0f, new Vector2(Image.Width / 2, Image.Height - 1),
                             _facingDirection == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        }

        public void Update(GameTime gameTime) {
            
        }
    }
}

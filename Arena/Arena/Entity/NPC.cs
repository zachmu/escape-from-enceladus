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
using Microsoft.Xna.Framework.Input;

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

        String[] conversation = new String[] {
                                                     "I hope this dialogue\nreferences a popular \nmeme.",
                                                     "There is no way I'm \nbuying this game \notherwise."
                                                 };

        private static Texture2D[] WalkAnimation;
        private static Texture2D Stand;
        private static Texture2D YButton { get; set; }
        private static SpriteFont Font;
        private Texture2D Image { get; set; }

        private Body _body;
        private Direction _facingDirection = Direction.Left;
        private Color _color;

        private int _contactCount = 0;
        private bool _playerNearby;
        private bool _inConversation;
        private int _conversationLineNum;

        public NPC(Color color, Vector2 topLeft, Vector2 bottomRight, World world) {
            _topLeft = topLeft;
            _bottomRight = bottomRight;
            _color = color;

            _body = BodyFactory.CreateRectangle(world, .6f, Height, 0f);
            _body.IsStatic = false;
            _body.CollisionCategories = Arena.NPCCategory;
            _body.CollidesWith = Arena.TerrainCategory;
            _body.Position = Position;

            Body proximitySensor = BodyFactory.CreateRectangle(world, Width, Height, 0);
            proximitySensor.Position = Position;
            proximitySensor.IsStatic = true;
            proximitySensor.IsSensor = true;
            proximitySensor.CollisionCategories = Arena.TerrainCategory;
            proximitySensor.CollidesWith = Arena.PlayerCategory;

            proximitySensor.OnCollision += (a, b, contact) => {
                _contactCount++;
                return true;
            };
            proximitySensor.OnSeparation += (a, b) => {
                _contactCount--;
            };

            Image = Stand;
        }

        public bool Disposed {
            get { return _disposed; }
        }

        public static void LoadContent(ContentManager content) {
            WalkAnimation = Player.Instance._unarmedWalkAnimation;
            Stand = Player.Instance._unarmedStandFrame;
            YButton = content.Load<Texture2D>("ButtonImages/xboxControllerButtonY");
            Font = content.Load<SpriteFont>("Fonts/November");
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

            if ( _playerNearby && !_inConversation ) {
                position = _body.Position;
                position.Y -= Height / 2 + .5f;

                displayPosition = ConvertUnits.ToDisplayUnits(position);

                spriteBatch.Draw(YButton,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, YButton.Width / 2,
                                               YButton.Height / 2),
                                 new Rectangle(0, 0, YButton.Width, YButton.Height), 
                                 SolidColorEffect.DisabledColor, 0f,
                                 new Vector2(YButton.Width / 2, YButton.Height / 2),
                                 _facingDirection == Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);
            }

            if ( _inConversation ) {
                position = _body.Position;
                position.Y -= Height / 2 + 1.5f;

                displayPosition = ConvertUnits.ToDisplayUnits(position);

                Vector2 stringSize = Font.MeasureString(conversation[_conversationLineNum]);
                displayPosition -= stringSize / 2;
                spriteBatch.DrawString(Font, conversation[_conversationLineNum], 
                                 displayPosition, _color);
            }
        }

        public void Update(GameTime gameTime) {
            if ( _contactCount > 0 ) {
                _playerNearby = Contains(Player.Instance.Position, 0);
            }

            if ( _inConversation ) {
                if (new Buttons[] { Buttons.A, Buttons.B, Buttons.Y, Buttons.X}.ToList().Any(
                    button => InputHelper.Instance.IsNewButtonPress(button))) {
                    _conversationLineNum++;
                    if (_conversationLineNum >= conversation.Length) {
                        StopConversation();
                    }
                }
            } else if ( _playerNearby ) {
                if ( InputHelper.Instance.IsNewButtonPress(Buttons.Y) ) {
                    StartConversation();
                }
            }
        }

        private void StartConversation() {
            Arena.Instance.StartConversation(this);
            _inConversation = true;
            _conversationLineNum = 0;
        }

        private void StopConversation() {
            Arena.Instance.EndConversation();
            _inConversation = false;
        }
    }
}

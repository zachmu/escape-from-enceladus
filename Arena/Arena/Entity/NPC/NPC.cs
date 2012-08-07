using System;
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

namespace Arena.Entity.NPC {

    /// <summary>
    /// NPC character that you can talk to.
    /// </summary>
    public class NPC : Region, IGameEntity {

        private bool _disposed;

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
        }

        private const int NumUnarmedWalkFrames = 27;
        private const float CharacterHeight = 1.9f;
        private const int HorizontalMargin = 40;

        String[] conversation = new String[] {
                                                     "I hope this dialogue references a popular meme.",
                                                     "There is no way I'm buying this game otherwise."
                                                 };

        protected static int MaxDialogWidth = 450;

        private static Texture2D[] WalkAnimation;
        private static Texture2D Stand;
        private static Texture2D YButton { get; set; }
        private static SpriteFont Font;
        private Texture2D Image { get; set; }

        private readonly Body _body;
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

            _body = BodyFactory.CreateRectangle(world, .6f, CharacterHeight, 0f);
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
            position.Y += CharacterHeight / 2;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Color color = _color;
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, color, 0f, new Vector2(Image.Width / 2, Image.Height - 1),
                             _facingDirection == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);

            if ( _playerNearby && !_inConversation ) {
                position = _body.Position;
                position.Y -= CharacterHeight / 2 + .5f;

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
                position.Y -= CharacterHeight / 2 + 1.5f;

                displayPosition = ConvertUnits.ToDisplayUnits(position);

                // split the string with newlines to get the width right
                StringBuilder sb = new StringBuilder();
                string text = conversation[_conversationLineNum];
                int left = 0, right = text.Length - 1;
                while ( left < right ) {
                    string substring = text.Substring(left, right - left + 1);
                    float width = Font.MeasureString(substring).X;
                    while ( width > MaxDialogWidth ) {
                        // look for a space to break the text up
                        for ( int i = right; i > left; i-- ) {
                            if ( text[i] == ' ' ) {
                                right = i - 1;
                                break;
                            }
                        }
                        substring = text.Substring(left, right - left + 1);
                        width = Font.MeasureString(substring).X;
                    }
                    sb.Append(substring).Append("\n");
                    left = right + 2;
                    right = text.Length - 1;
                }

                Vector2 stringSize = Font.MeasureString(sb);
                displayPosition -= stringSize / 2;

                // If we're still drawing off of the screen, nudge the draw boundary
                int screenWidth = spriteBatch.GraphicsDevice.Viewport.Bounds.Width;
                float cameraScreenCenter = ConvertUnits.ToDisplayUnits(camera.Position).X;
                float leftMargin = cameraScreenCenter - screenWidth / 2 + HorizontalMargin;
                float rightMargin = cameraScreenCenter + screenWidth / 2 - HorizontalMargin;

                if ( displayPosition.X < leftMargin ) {
                    displayPosition.X = leftMargin;
                } else if ( displayPosition.X + stringSize.X > rightMargin ) {
                    displayPosition.X = rightMargin - stringSize.X;
                }

                // Finally, draw the text shadowed. This involves drawing the text twice, darker then lighter.
                Color shadow = Color.Lerp(_color, Color.Black, .5f);
                spriteBatch.DrawString(Font, sb, displayPosition + new Vector2(3), shadow);
                spriteBatch.DrawString(Font, sb, displayPosition, _color);
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

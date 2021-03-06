﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Control;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Enceladus.Entity.NPC {

    /// <summary>
    /// NPC character that you can talk to.
    /// </summary>
    public abstract class NPC : Region, IGameEntity, IDialogEntity {

        private bool _disposed;

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
            NPCFactory.Destroy(this);
        }

        private const int NumUnarmedWalkFrames = 27;
        private const float CharacterHeight = 1.9f;
        private const int HorizontalMargin = 40;
        private const int VerticalMargin = 40;

        protected static int MaxDialogWidth = 450;

        private static Texture2D[] WalkAnimation;
        private static Texture2D Stand;

        private static Random _random = new Random(0);

        private int _animationFrame;
        private long _timeSinceLastAnimationUpdate;
        private Texture2D _image;
        private Texture2D Image {
            get { return _image; }
            set {
                if ( _image != value ) {
                    _timeSinceLastAnimationUpdate = 0;
                }
                _image = value;
            }
        }

        private readonly Body _body;
        private Direction _facingDirection = Direction.Left;
        
        private enum Mode {
            Walking,
            Standing,
        }
        private Mode _mode = Mode.Standing;

        private Color _color;

        private int _contactCount = 0;
        private bool _playerNearby;
        private bool _inConversation;
        private string _name;

        public string Name {
            get { return _name; }
        }

        public NPC(String name, Color color, Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth) : base(topLeft, bottomRight) {
            _name = name;
            _color = color;

            _body = BodyFactory.CreateRectangle(world, .6f, CharacterHeight, 0f);
            _body.IsStatic = false;
            _body.CollisionCategories = EnceladusGame.NPCCategory;
            _body.CollidesWith = EnceladusGame.TerrainCategory;
            _body.Position = Position + new Vector2(0, Height / 2 - CharacterHeight / 2);

            Fixture proximitySensor = FixtureFactory.AttachRectangle(sensorWidth, CharacterHeight * 2, 0, Vector2.Zero, _body);
            proximitySensor.IsSensor = true;
            proximitySensor.CollisionCategories = EnceladusGame.PlayerSensorCategory;
            proximitySensor.CollidesWith = EnceladusGame.PlayerCategory;

            proximitySensor.OnCollision += (a, b, contact) => {
                if ( b.Body == _body ) {
                    return false;
                }
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
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            DrawCharacter(spriteBatch);

            if ( _playerNearby && !_inConversation ) {
                DrawButtonPrompt(spriteBatch);
            }
        }

        private void DrawCharacter(SpriteBatch spriteBatch) {
            // Draw origin is character's feet
            Vector2 position = _body.Position;
            position.Y += CharacterHeight / 2;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Color color = _color;
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, color, 0f, new Vector2(Image.Width / 2f, Image.Height - 1),
                             _facingDirection == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        }

        private void DrawButtonPrompt(SpriteBatch spriteBatch) {
            Vector2 position = _body.Position;
            position.Y -= CharacterHeight / 2 + .5f;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Texture2D image = SharedGraphicalAssets.YButton;
            spriteBatch.Draw(image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, image.Width / 2,
                                           image.Height / 2),
                             new Rectangle(0, 0, image.Width, image.Height),
                             SolidColorEffect.DisabledColor, 0f,
                             new Vector2(image.Width / 2f, image.Height / 2f),
                             SpriteEffects.None, 0);
        }

        /// <summary>
        /// Draws the conversation text given in a text box above the NPC
        /// </summary>
        public virtual void DrawConversationText(SpriteBatch spriteBatch, Camera2D camera, string text) {
            Vector2 position = _body.Position;
            position.Y -= CharacterHeight / 2 + 1.5f;
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            DrawText(spriteBatch, _color, camera, text, displayPosition);
        }

        /// <summary>
        /// Draws the text given centered at the position given.
        /// </summary>
        internal static void DrawText(SpriteBatch spriteBatch, Color color, Camera2D camera, string text, Vector2 displayPosition) {
            // split the string with newlines to get the width right
            StringBuilder sb = new StringBuilder();
            int left = 0, right = text.Length - 1;
            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;
            while ( left < right ) {
                string substring = text.Substring(left, right - left + 1);
                float width = dialogFont.MeasureString(substring).X;
                while ( width > MaxDialogWidth ) {
                    // look for a space to break the text up
                    for ( int i = right; i > left; i-- ) {
                        if ( text[i] == ' ' ) {
                            right = i - 1;
                            break;
                        }
                    }
                    substring = text.Substring(left, right - left + 1);
                    width = dialogFont.MeasureString(substring).X;
                }
                sb.Append(substring).Append("\n");
                left = right + 2;
                right = text.Length - 1;
            }

            Vector2 stringSize = dialogFont.MeasureString(sb);
            displayPosition -= stringSize / 2;

            // If we're still drawing off of the screen, nudge the draw boundary
            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Bounds.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Bounds.Height;
            Vector2 cameraScreenCenter = ConvertUnits.ToDisplayUnits(camera.Position);
            float leftMargin = cameraScreenCenter.X - screenWidth / 2 + HorizontalMargin;
            float rightMargin = cameraScreenCenter.X + screenWidth / 2 - HorizontalMargin;
            float topMargin = cameraScreenCenter.Y - screenHeight / 2 + VerticalMargin;
            float bottomMargin = cameraScreenCenter.Y + screenHeight / 2 - VerticalMargin;

            if ( displayPosition.X < leftMargin ) {
                displayPosition.X = leftMargin;
            } else if ( displayPosition.X + stringSize.X > rightMargin ) {
                displayPosition.X = rightMargin - stringSize.X;
            }
            if ( displayPosition.Y < topMargin ) {
                displayPosition.Y = topMargin;
            } else if ( displayPosition.Y + stringSize.Y > bottomMargin ) {
                displayPosition.Y = bottomMargin - stringSize.Y;
            }

            // Draw a backdrop
            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop,
                             new Rectangle((int) displayPosition.X - 10, (int) displayPosition.Y + 10, (int) stringSize.X + 20,
                                           (int) stringSize.Y - 30), Color.Black * .65f);

            TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, color, sb.ToString(), displayPosition);
        }

        public void Update(GameTime gameTime) {
            _playerNearby = _contactCount > 0;

            if ( _mode == Mode.Walking ) {
                if ( !Contains(_body.Position) ) {
                    _facingDirection = _body.Position.X > BottomRight.X ? Direction.Left : Direction.Right;
                }

                if ( _facingDirection == Direction.Left ) {
                    _body.LinearVelocity = new Vector2(-2, 0);
                } else {
                    _body.LinearVelocity = new Vector2(2, 0);
                }

                if ( _random.NextDouble() > .997 ) {
                    _mode = Mode.Standing;
                    _body.LinearVelocity = Vector2.Zero;
                }

            } else {
                if ( _random.NextDouble() > .997 ) {
                    _mode = Mode.Walking;
                }

                if ( _random.NextDouble() > .995 ) {
                    if ( _facingDirection == Direction.Left ) {
                        _facingDirection = Direction.Right;
                    } else {
                        _facingDirection = Direction.Left;
                    }
                }
            }

            if ( _playerNearby ) {
                if ( PlayerControl.Control.IsNewAction() && !EnceladusGame.Instance.IsInConversation ) {
                    ConversationManager.Instance.StartConversation(this);
                }
            }

            UpdateImage(gameTime);
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(global::Enceladus.Mode mode) {
            return mode == global::Enceladus.Mode.NormalControl; 
        }

        private void UpdateImage(GameTime gameTime) {
            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;

            if (_mode == Mode.Walking) {
                float walkSpeed = Math.Abs(_body.LinearVelocity.X * .5f); 
                if ( _timeSinceLastAnimationUpdate > 1000f / NumUnarmedWalkFrames / walkSpeed) {
                    _animationFrame %= NumUnarmedWalkFrames;
                    Image = WalkAnimation[_animationFrame++];
                }
            } else {
                Image = Stand;
            }
        }

        /// <summary>
        /// Initiates conversation mode
        /// </summary>
        public void StartConversation() {
            _inConversation = true;
            _body.LinearVelocity = Vector2.Zero;
            _mode = Mode.Standing;
            _facingDirection = Player.Instance.Position.X < _body.Position.X ? Direction.Left : Direction.Right;
        }

        public void StopConversation() {
            _inConversation = false;
        }

        public Color Color { get { return _color; } }
    }
}

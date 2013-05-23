using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arena.Control;
using Arena.Entity.NPC;
using Arena.Event;
using Arena.Farseer;
using Arena.Map;
using Arena.Overlay;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.InteractiveObject {
    public class SaveStation : Region, IGameEntity {

        private const string SaveGame = "Save game";
        private const string Saving = "Saving...";
        private const string Saved = "Saved";

        private Body _body;
        private int _contactCount = 0;
        private string _id;

        public string Id {
            get { return _id; }
        }

        private bool _playerNearby = false;
        private bool _saving = false;
        private bool _saved = false;
        private ManualResetEvent _saveHandle;
        private double _timerMs;

        private const double _minSavingDisplayTimeMs = 1000;

        public SaveStation(World world, string name, Vector2 topLeft, Vector2 bottomRight)
            : base(topLeft, bottomRight) {

            _id = name; 

            _body = BodyFactory.CreateRectangle(world, Width, Height, 0f);
            _body.Position = Position;
            _body.IsStatic = true;
            _body.IsSensor = true;
            _body.CollisionCategories = Arena.PlayerSensorCategory;
            _body.CollidesWith = Arena.PlayerCategory;

            _body.OnCollision += (a, b, contact) => {
                _contactCount++;
                return true;
            };
            _body.OnSeparation += (a, b) => {
                _contactCount--;
            };
        }

        public void Dispose() {
            _body.Dispose();
        }

        public bool Disposed {
            get { return _body.IsDisposed; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( _playerNearby ) {
                DrawButtonPrompt(spriteBatch);
            }
        }

        private void DrawButtonPrompt(SpriteBatch spriteBatch) {
            Vector2 position = _body.Position;
            position.Y = TopLeft.Y - 1f;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);

            Texture2D button = SharedGraphicalAssets.YButton;

            string text;
            if ( _saved ) {
                text = Saved;
            } else if ( _saving ) {
                text = Saving;
            } else {
                text = SaveGame;
            }

            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;
            Vector2 stringSize = dialogFont.MeasureString(text);
            float imageWidth = button.Width / 2f;
            displayPosition -= stringSize / 2 - new Vector2(imageWidth / 2, 0);

            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop,
                             new Rectangle((int) (displayPosition.X - 20 - imageWidth), (int) displayPosition.Y + 10,
                                           (int) (stringSize.X + 30 + imageWidth),
                                           (int) stringSize.Y), Color.Black * .65f);

            Color shadow = Color.Lerp(Color.White, Color.Black, .5f);
            spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, text, displayPosition + new Vector2(3), shadow);
            spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, text, displayPosition, Color.White);

            spriteBatch.Draw(button,
                             new Rectangle((int) (displayPosition.X - imageWidth + 10),
                                           (int) (displayPosition.Y + stringSize.Y / 2 + 10),
                                           button.Width / 2,
                                           button.Height / 2),
                             new Rectangle(0, 0, button.Width, button.Height),
                             Color.White, 0f,
                             new Vector2(button.Width / 2f, button.Height / 2f),
                             SpriteEffects.None, 0);
        }

        public void Update(GameTime gameTime) {
            _playerNearby = _contactCount > 0;
            if ( _saving ) {
                _timerMs += gameTime.ElapsedGameTime.TotalMilliseconds;

                // It's a bad experience to let players think things didn't save just 
                // because it happens quickly, so fake a delay in that case.
                if ( _timerMs >= _minSavingDisplayTimeMs && _saveHandle.WaitOne(20) ) {
                    _saving = false;
                    _saveHandle = null;
                    _saved = true;
                    _timerMs = 0;
                    Arena.Instance.UnsetMode();
                }
            } else if ( _saved ) {
                _timerMs += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( _timerMs >= _minSavingDisplayTimeMs ) {
                    _saved = false;
                }
            } else if ( _playerNearby ) {
                if ( PlayerControl.Control.IsNewAction() && !Arena.Instance.IsInConversation ) {
                    Arena.Instance.SetMode(Mode.Saving);
                    _saving = true;
                    _timerMs = 0;
                    SaveState state = SaveState.Create(Id);
                    _saveHandle = state.Persist();
                }
            }
        }

        public bool DrawAsOverlay {
            get { return true; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl || mode == Mode.Conversation || mode == Mode.Saving;
        }
    }
}

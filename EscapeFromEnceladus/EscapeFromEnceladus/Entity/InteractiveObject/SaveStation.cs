using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Enceladus.Entity.NPC;
using Enceladus.Control;
using Enceladus.Event;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.InteractiveObject {
    public class SaveStation : Region, IGameEntity {

        private const string SaveGame = "Save game";
        private const string Saving = "Saving...";
        private const string Saved = "Saved";

        private readonly Body _body;
        private int _contactCount = 0;

        public string Id { get; private set; }

        private bool _playerNearby = false;
        private bool _saving = false;
        private bool _saved = false;
        private SaveWaiter _saveWaiter;
        private double _timerMs;

        private const double _minSavingDisplayTimeMs = 1000;

        public SaveStation(World world, string name, Vector2 topLeft, Vector2 bottomRight)
            : base(topLeft, bottomRight) {

            Id = name;

            _body = CreatePlayerSensor(world);

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

            TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, Color.White, text, displayPosition);

            spriteBatch.Draw(button,
                             new Rectangle((int) (displayPosition.X - imageWidth + 10),
                                           (int) (displayPosition.Y + stringSize.Y / 2 + 10),
                                           button.Width / 2,
                                           button.Height / 2),
                             new Rectangle(0, 0, button.Width, button.Height),
                             SolidColorEffect.DisabledColor, 0f,
                             new Vector2(button.Width / 2f, button.Height / 2f),
                             SpriteEffects.None, 0);
        }

        public void Update(GameTime gameTime) {
            _playerNearby = _contactCount > 0;
            if ( _saving ) {
                _timerMs += gameTime.ElapsedGameTime.TotalMilliseconds;

                // It's a bad experience to let players think things didn't save just 
                // because it happens quickly, so fake a delay in that case.
                if ( _timerMs >= _minSavingDisplayTimeMs && _saveWaiter.WaitHandle.WaitOne(20) ) {
                    _saving = false;
                    _saveWaiter = null;
                    _saved = true;
                    _timerMs = 0;
                    EnceladusGame.Instance.UnsetMode();
                }
            } else if ( _saved ) {
                _timerMs += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( _timerMs >= _minSavingDisplayTimeMs ) {
                    _saved = false;
                }
            } else if ( _playerNearby ) {
                if ( PlayerControl.Control.IsNewAction() && !EnceladusGame.Instance.IsInConversation ) {
                    EnceladusGame.Instance.SetMode(Mode.Saving);
                    _saving = true;
                    _timerMs = 0;
                    SaveState state = EnceladusGame.Instance.GetSaveState();
                    state.SaveStationLocation = _body.Position;
                    _saveWaiter = state.Persist();
                }
            }
        }

        public bool DrawAsOverlay {
            get { return true; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl || mode == Mode.Saving;
        }
    }
}

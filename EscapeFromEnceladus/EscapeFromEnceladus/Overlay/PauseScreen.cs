using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Enceladus.Control;
using Enceladus.Entity;
using Enceladus.Event;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Pause menu overlay
    /// </summary>
    public class PauseScreen : MenuScreen {

        private static readonly string[] MenuItems = new[] {
            "Return to Game",
            "Restart From Last Save Point",
            "Exit Game"
        };

        private static readonly string[] LoadingMenuItems = new[] {
            "",
            "Loading...",
            "",
        };

        private readonly Action[] MenuActions;

        private double _loadTimer = 0;
        private bool _loadingSavedGame = false;
        private SaveWaiter _saveWaiter;
        private bool _waitingForSaveGameToApply;
        private WaitHandle _applySaveStateWaitHandle;

        public PauseScreen() {
            MenuActions = new Action[] {
                ReturnToGame,
                LoadLastSave,
                ExitGame,
            };
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;

            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);

            StringBuilder sb = new StringBuilder();
            string[] menuItems = _loadingSavedGame || _waitingForSaveGameToApply ? LoadingMenuItems : MenuItems;
            foreach ( var s in menuItems) {
                sb.Append(s).Append("\n");
            }
            Vector2 stringSize = dialogFont.MeasureString(sb);
            Vector2 topLeft = screenCenter - new Vector2(stringSize.X / 2 + 20, stringSize.Y / 2) - new Vector2(0, dialogFont.LineSpacing * 2);
            Vector2 bottomRight = screenCenter + new Vector2(stringSize.X / 2 + 20, stringSize.Y / 2);

            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop,
                             new Rectangle((int) topLeft.X, (int) topLeft.Y,
                                           (int) (bottomRight.X - topLeft.X),
                                           (int) (bottomRight.Y - topLeft.Y)), Color.Black * .65f);

            Vector2 menuStringSize = dialogFont.MeasureString("Pause Menu");
            Vector2 displayPosition = screenCenter -
                                      new Vector2(menuStringSize.X / 2, stringSize.Y / 2 + dialogFont.LineSpacing * 2);
            TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, Color.White, "Pause Menu", displayPosition);
            
            for ( int i = 0; i < menuItems.Count(); i++ ) {
                Color color = Color.White;                     
                if ( i == _selectedIndex ) {
                    color = _flash ? Color.Crimson : Color.White;
                }
                displayPosition = screenCenter -
                                          new Vector2(stringSize.X / 2, stringSize.Y / 2 - dialogFont.LineSpacing * i);
                string text = menuItems[i];
                TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, color, text, displayPosition);
            }
        }

        public void Update(GameTime gameTime) {
            UpdateFlashTimer(gameTime);

            if ( _loadingSavedGame ) {
                _loadTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( _loadTimer >= 1000 && _saveWaiter.WaitHandle.WaitOne(0) ) {
                    _applySaveStateWaitHandle = EnceladusGame.Instance.ApplySaveState(_saveWaiter.SaveState);
                    _saveWaiter = null;
                    _loadingSavedGame = false;
                    _waitingForSaveGameToApply = true;
                }
                return;
            }

            if ( _waitingForSaveGameToApply ) {
                if ( _applySaveStateWaitHandle.WaitOne(0) ) {
                    _waitingForSaveGameToApply = false;
                    _applySaveStateWaitHandle = null;
                    EnceladusGame.Instance.UnsetMode();
                }
                return;
            }

            if ( PlayerControl.Control.IsNewPause() || PlayerControl.Control.IsNewCancelButton() ) {
                if ( EnceladusGame.Instance.Mode == Mode.Paused ) {
                    EnceladusGame.Instance.UnsetMode();
                }
                return;
            }

            if ( EnceladusGame.Instance.Mode != Mode.Paused ) {
                return;
            }

            HandleMovementControl();
        }

        protected override int NumMenuItems {
            get { return MenuItems.Count(); }
        }

        protected override void ApplyMenuSelection() {
            MenuActions[_selectedIndex]();
        }

        private void ReturnToGame() {
            EnceladusGame.Instance.UnsetMode();
        }

        private void LoadLastSave() {
            SaveState save = EnceladusGame.Instance.GetSaveState();
            _loadingSavedGame = true;
            _loadTimer = 0;
            _saveWaiter = save.Load();
        }

        private void ExitGame() {
            EnceladusGame.Instance.Exit();
        }
    }
}

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
    /// The menu screen displayed after dying
    /// </summary>
    public class DeathScreen : MenuScreen {
        private const string MenuTitle = "Mission Failed";

        private static readonly string[] MenuItems = new[] {
            "Restart From Last Save Point",
            "Return to Title Screen",
            "Exit Game",
        };

        private static readonly string[] LoadingMenuItems = new[] {
            "Loading...",
            "",
            "",
        };

        private readonly Action[] MenuActions;

        public DeathScreen() {
            MenuActions = new Action[] {
                LoadLastSave,
                ReturnToTitle,
                ExitGame
            };
        }

        private const double FadeTimeMs = 1500;
        private const double MenuDelayMs = 3000;
        private double _fadeTimer = 0;
        private double _loadTimer = 0;
        private bool _loadingSavedGame = false;
        private SaveWaiter _saveWaiter;
        private bool _waitingForSaveGameToApply;
        private WaitHandle _applySaveStateWaitHandle;

        /// <summary>
        /// Resets the death screen to its beginning values
        /// </summary>
        public void Reset() {
            _fadeTimer = 0;
        }

        /// <summary>
        /// Gets the appropriate alpha to draw the game scene (excluding the player)
        /// </summary>
        /// <returns></returns>
        public float GetScreenAlpha() {
            if ( _fadeTimer > FadeTimeMs )
                return 0;
            return (float) (1d - _fadeTimer / FadeTimeMs);
        }

        protected override int NumMenuItems {
            get { return MenuItems.Count(); }
        }

        private void ReturnToTitle() {
            EnceladusGame.Instance.GoToTitleScreen();
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

        protected override void ApplyMenuSelection() {
            MenuActions[_selectedIndex]();
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( _fadeTimer < MenuDelayMs )
                return;

            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;

            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth/2f, screenHeight/2f);

            StringBuilder sb = new StringBuilder();
            string[] menuItems = _loadingSavedGame || _waitingForSaveGameToApply ? LoadingMenuItems : MenuItems;
            foreach ( var s in menuItems ) {
                sb.Append(s).Append("\n");
            }
            Vector2 stringSize = dialogFont.MeasureString(sb);
            Vector2 topLeft = screenCenter - new Vector2(stringSize.X/2 + 20, stringSize.Y/2) -
                              new Vector2(0, dialogFont.LineSpacing*2);
            Vector2 bottomRight = screenCenter + new Vector2(stringSize.X/2 + 20, stringSize.Y/2);

            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop,
                             new Rectangle((int) topLeft.X, (int) topLeft.Y,
                                           (int) (bottomRight.X - topLeft.X),
                                           (int) (bottomRight.Y - topLeft.Y)), Color.Black*.65f);

            Vector2 menuStringSize = dialogFont.MeasureString(MenuTitle);
            Vector2 displayPosition = screenCenter -
                                      new Vector2(menuStringSize.X/2, stringSize.Y/2 + dialogFont.LineSpacing*2);
            TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, Color.White, MenuTitle, displayPosition);

            for ( int i = 0; i < menuItems.Count(); i++ ) {
                Color color = Color.White;
                if ( i == _selectedIndex ) {
                    color = _flash ? Color.Crimson : Color.White;
                }
                displayPosition = screenCenter -
                                  new Vector2(stringSize.X/2, stringSize.Y/2 - dialogFont.LineSpacing*i);
                string text = menuItems[i];
                TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, color, text, displayPosition);
            }
        }

        public void Update(GameTime gameTime) {
            _fadeTimer += gameTime.ElapsedGameTime.TotalMilliseconds;

            if ( _fadeTimer < FadeTimeMs ) {
                return;
            }

            if ( !Player.Instance.Disposed && !_waitingForSaveGameToApply ) {
                Player.Instance.Destroy();
            } else {
                EnceladusGame.Instance.StepWorld(gameTime);
            }

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

            if ( EnceladusGame.Instance.Mode != Mode.Death ) {
                return;
            }

            HandleMovementControl();
        }

    }
}

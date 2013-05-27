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
    public class PauseScreen {

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

        private int _selectedIndex = 0;
        private double _timer = 0;
        private double _loadTimer = 0;
        private bool _flash;
        private bool _loadingSavedGame = false;
        private SaveWaiter _saveWaiter;

        public PauseScreen() {
            MenuActions = new Action[] {
                ReturnToGame,
                LoadLastSave,
                ExitGame,
            };
        }

        private const double MsUntilColorChange = 250;

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;

            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);

            StringBuilder sb = new StringBuilder();
            string[] menuItems = _loadingSavedGame ? LoadingMenuItems : MenuItems;
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
            _timer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer >= MsUntilColorChange ) {
                _timer %= MsUntilColorChange;
                _flash = !_flash;
            }

            if ( _loadingSavedGame ) {
                _loadTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                if ( _loadTimer >= 1000 && _saveWaiter.WaitHandle.WaitOne(50) ) {                    
                    EnceladusGame.Instance.ApplySaveState(_saveWaiter.SaveState);
                    EnceladusGame.Instance.UnsetMode();
                    _saveWaiter = null;
                    _loadingSavedGame = false;
                }
                return;
            }

            if ( PlayerControl.Control.IsNewPause() ) {
                if ( EnceladusGame.Instance.Mode == Mode.Paused ) {
                    EnceladusGame.Instance.UnsetMode();
                } else if ( EnceladusGame.Instance.Mode == Mode.NormalControl ||
                            EnceladusGame.Instance.Mode == Mode.Conversation ) {
                    EnceladusGame.Instance.SetMode(Mode.Paused);
                }
            }

            if ( EnceladusGame.Instance.Mode != Mode.Paused ) {
                return;
            }

            if ( PlayerControl.Control.IsNewCancelButton() ) {
                EnceladusGame.Instance.UnsetMode();
                return;
            }

            Direction? direction;
            if ( PlayerControl.Control.IsNewDirection(out direction) ) {
                switch ( direction ) {
                    case Direction.Up:
                        _selectedIndex = (_selectedIndex - 1) % MenuItems.Count();
                        break;
                    case Direction.Down:
                        _selectedIndex = (_selectedIndex + 1) % MenuItems.Count();
                        break;
                    default:
                        break;
                }
            } else if ( PlayerControl.Control.IsNewConfirmButton() ) {
                ApplyMenuSelection();
            }
        }

        private void ApplyMenuSelection() {
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

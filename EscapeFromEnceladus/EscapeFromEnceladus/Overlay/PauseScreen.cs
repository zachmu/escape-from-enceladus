using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Control;
using Enceladus.Entity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Pause menu overlay
    /// </summary>
    public class PauseScreen {
        
        private static readonly string[] MenuItems = new[]  {             
            "Return to Game",
            "Restart From Last Save Point",
            "Exit Game"
        };

        private int _selectedIndex = 0;
        private double _timer = 0;
        private bool _flash;
        private const double MsUntilColorChange = 250;

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;

            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);

            StringBuilder sb = new StringBuilder();
            foreach ( var s in MenuItems) {
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
            DrawStringShadowed(spriteBatch, Color.White, "Pause Menu", displayPosition);
            
            for ( int i = 0; i < MenuItems.Count(); i++ ) {
                Color color = Color.White;                     
                if ( i == _selectedIndex ) {
                    color = _flash ? Color.Crimson : Color.White;
                }
                displayPosition = screenCenter -
                                          new Vector2(stringSize.X / 2, stringSize.Y / 2 - dialogFont.LineSpacing * i);
                string text = MenuItems[i];
                DrawStringShadowed(spriteBatch, color, text, displayPosition);
            }
        }

        private static void DrawStringShadowed(SpriteBatch spriteBatch, Color color, string text, Vector2 displayPosition) {
            Color shadow = Color.Lerp(color, Color.Black, .5f);
            spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, text, displayPosition + new Vector2(3), shadow);
            spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, text, displayPosition, color);
        }

        public void Update(GameTime gameTime) {
            _timer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer >= MsUntilColorChange ) {
                _timer %= MsUntilColorChange;
                _flash = !_flash;
            }

            if ( PlayerControl.Control.IsNewPause() ) {
                if ( EnceladusGame.Instance.Mode == Mode.Paused ) {
                    EnceladusGame.Instance.UnsetMode();
                } else if ( EnceladusGame.Instance.Mode == Mode.NormalControl ||
                            EnceladusGame.Instance.Mode == Mode.Conversation ) {
                    EnceladusGame.Instance.SetMode(Mode.Paused);
                }
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
            }
        }
    }
}

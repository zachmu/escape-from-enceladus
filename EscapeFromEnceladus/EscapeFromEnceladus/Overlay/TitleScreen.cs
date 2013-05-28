using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Control;
using Enceladus.Entity;
using Enceladus.Event;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// What you first see upon loading the game.
    /// </summary>
    public class TitleScreen {

        private int _selectedIndex = 0;
        private double _timer = 0;
        private double _loadTimer = 0;
        private bool _flash;
        private bool _loadingSavedGame;
        private SaveWaiter _saveWaiter;

        private const double ColorChangeFrequency = .003;
        private double _colorChangeTimer = 0;
        private Color _rainbowColor = Color.White;

        private int _frameNum = 0;
        private double _frameChangeTimer = 0;
        private const double FrameTimeMs = 70;

        private const double MsBetweenFlash = 250;
        private const string Title = "E S C A P E";
        private const string SubTitle = "F R O M   E N C E L A D U S";

        public void Draw(SpriteBatch spriteBatch) {

            spriteBatch.GraphicsDevice.Clear(Color.Black);

            SpriteFont dialogFont = SharedGraphicalAssets.DialogFont;
            SpriteFont titleFont = SharedGraphicalAssets.TitleFont;

            int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
            int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;
            Vector2 screenCenter = new Vector2(screenWidth / 2f, screenHeight / 2f);

            // Draw the player
            spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect);
            Texture2D[] animation = Player.Instance.RunAimStraightAnimation;
            spriteBatch.Draw(animation[_frameNum], screenCenter, null, _rainbowColor, 0f, new Vector2(animation[_frameNum].Width / 2, animation[_frameNum].Height / 2), 1f, SpriteEffects.None, 0f);
            spriteBatch.End();

            spriteBatch.Begin();

            Vector2 titleSize = titleFont.MeasureString(Title);
            Vector2 titlePos = screenCenter - titleSize / 2 - new Vector2(0, 250);
            TextDrawing.DrawStringShadowed(titleFont, spriteBatch, _rainbowColor, Title, titlePos);

            Vector2 subtitleSize = dialogFont.MeasureString(SubTitle);
            Vector2 subtitlePos = new Vector2(screenCenter.X - subtitleSize.X / 2, titlePos.Y + titleFont.LineSpacing - 30);
            TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, _rainbowColor, SubTitle, subtitlePos);

            StringBuilder sb = new StringBuilder();
            for ( int i = 1; i <= 3; i++) {
                sb.Append("Game " + i).Append("\n");
            }

            Vector2 stringSize = dialogFont.MeasureString(sb);

            for ( int i = 0; i < 3; i++ ) {
                Color color = Color.White;
                if ( i == _selectedIndex ) {
                    color = _flash ? Color.Crimson : Color.White;
                }
                Vector2 displayPosition = screenCenter - new Vector2(stringSize.X / 2, -250 + stringSize.Y / 2 - dialogFont.LineSpacing * i);
                string text = "Game " + (i + 1);
                TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, color, text, displayPosition);
            }

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            if ( EnceladusGame.Instance.Mode != Mode.TitleScreen ) {
                return;
            }

            _frameChangeTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _frameChangeTimer >= FrameTimeMs ) {
                _frameNum = (_frameNum + 1) % Player.Instance.RunAimStraightAnimation.Length;
                _frameChangeTimer -= FrameTimeMs;
            }

            _colorChangeTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            _rainbowColor = new Color((int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency)),
                (int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency + 2 * Math.PI / 3)),
                (int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency + 4 * Math.PI / 3)));

            _timer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer >= MsBetweenFlash ) {
                _timer %= MsBetweenFlash;
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

            if ( PlayerControl.Control.IsNewCancelButton() ) {
                EnceladusGame.Instance.UnsetMode();
                return;
            }

            Direction? direction;
            if ( PlayerControl.Control.IsNewDirection(out direction) ) {
                switch ( direction ) {
                    case Direction.Up:
                        _selectedIndex = (_selectedIndex - 1) % 3;
                        break;
                    case Direction.Down:
                        _selectedIndex = (_selectedIndex + 1) % 3;
                        break;
                    default:
                        break;
                }
            } else if ( PlayerControl.Control.IsNewConfirmButton() ) {
                ApplyMenuSelection();
            }
        }

        private void ApplyMenuSelection() {
            
        }

        private void LoadLastSave() {
            SaveState save = EnceladusGame.Instance.GetSaveState();
            _loadingSavedGame = true;
            _loadTimer = 0;
            _saveWaiter = save.Load();
        }
   
    }
}

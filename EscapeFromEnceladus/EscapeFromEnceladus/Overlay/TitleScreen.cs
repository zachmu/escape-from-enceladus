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
    /// What you first see upon loading the game.
    /// </summary>
    public class TitleScreen : MenuScreen {

        private bool _saveGamesInitialized;
        private bool _readingSavedGames;
        private bool _startingGame;
        private SaveWaiter[] _saveStates;
        private WaitHandle[] _waitHandles;

        private const double ColorChangeFrequency = .003;
        private double _colorChangeTimer = 0;
        private Color _rainbowColor = Color.White;

        private int _frameNum = 0;
        private double _frameChangeTimer = 0;
        private const double FrameTimeMs = 70;

        private const string Title = "E S C A P E";
        private const string SubTitle = "F R O M   E N C E L A D U S";
        private const string Loading = "Loading...";

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

            String[] games = new string[3];

            for ( int i = 0; i < 3; i++) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Game " + (i + 1));
                if ( _saveGamesInitialized && !_readingSavedGames ) {
                    sb.Append(": ").Append(SummarizeSaveState(_saveStates[i].SaveState));
                }
                games[i] = sb.ToString();
            }

            StringBuilder allText = new StringBuilder();
            for ( int i = 0; i < 3; i++ ) {
                allText.Append(games[0]).Append("\n");
            }

            Vector2 stringSize = dialogFont.MeasureString(allText);

            for ( int i = 0; i < 3; i++ ) {
                Color color = Color.White;
                if ( i == _selectedIndex ) {
                    color = _flash ? Color.Crimson : Color.White;
                }
                Vector2 displayPosition = screenCenter - new Vector2(stringSize.X / 2, -250 + stringSize.Y / 2 - dialogFont.LineSpacing * i);
                string text = games[i];
                TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, color, text, displayPosition);
            }

            if ( _readingSavedGames || _startingGame ) {
                Vector2 loadingSize = dialogFont.MeasureString(Loading);
                TextDrawing.DrawStringShadowed(dialogFont, spriteBatch, Color.White, Loading, screenCenter - loadingSize / 2);
            }

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            UpdateFlashTimer(gameTime);

            InitializeSaveStates();

            _frameChangeTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _frameChangeTimer >= FrameTimeMs ) {
                _frameNum = (_frameNum + 1) % Player.Instance.RunAimStraightAnimation.Length;
                _frameChangeTimer -= FrameTimeMs;
            }

            _colorChangeTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            _rainbowColor = new Color((int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency)),
                (int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency + 2 * Math.PI / 3)),
                (int) (127 + 128 * Math.Sin(_colorChangeTimer * ColorChangeFrequency + 4 * Math.PI / 3)));

            if ( _readingSavedGames ) {
                if ( WaitHandle.WaitAll(_waitHandles, 10) ) {
                    _readingSavedGames = false;
                }
                return;
            }

            if ( _startingGame ) {
                if ( EnceladusGame.Instance.Mode == Mode.TitleScreen ) {
                    EnceladusGame.Instance.UnsetMode();
                    _startingGame = false;
                }
                return;
            }

            HandleMovementControl();
        }

        private string SummarizeSaveState(SaveState state) {
            if ( state == null || state.SaveTime == null ) {
                return "New Game";
            } else {
                return state.SaveTime.ToString();
            }
        }

        public void Reset() {
            _saveGamesInitialized = false;
        }

        private void InitializeSaveStates() {
            if ( !_saveGamesInitialized ) {
                _saveStates = new SaveWaiter[3];
                _waitHandles = new WaitHandle[3];
                for ( int i = 0; i < 3; i++ ) {
                    SaveState save = new SaveState((PlayerIndex) i);
                    _saveStates[i] = save.Load();
                    _waitHandles[i] = _saveStates[i].WaitHandle;
                }
                _readingSavedGames = true;
                _saveGamesInitialized = true;
            }
        }

        protected override int NumMenuItems {
            get { return 3; }
        }

        protected override void ApplyMenuSelection() {
            if ( _saveStates[_selectedIndex].SaveState != null ) {
                EnceladusGame.Instance.ApplySaveState(_saveStates[_selectedIndex].SaveState);
            } else {
                EnceladusGame.Instance.NewGame((PlayerIndex) _selectedIndex);
            }
            _startingGame = true;
        }
    }
}

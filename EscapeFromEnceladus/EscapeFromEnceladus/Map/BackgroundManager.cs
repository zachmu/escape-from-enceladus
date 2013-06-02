using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Map {
    /// <summary>
    /// The background manager is responsible for drawing all background images.
    /// </summary>
    public class BackgroundManager : IGameEntity {
        private Texture2D _background;
        private float _backgroundAlpha;

        public float BackgroundAlpha {
            get { return _backgroundAlpha; }
        }

        private static BackgroundManager _instance;
        private readonly ContentManager _content;

        public static BackgroundManager Instance {
            get { return _instance; }
        }

        public BackgroundManager(ContentManager content) {
            _instance = this;
            _content = content;
            _backgroundAlpha = 1;
        }

        public void LoadRoom(Room room) {
            if ( room.BackgroundImage != null ) {
                _background = _content.Load<Texture2D>("Background/" + room.BackgroundImage);
            }
        }

        public void LoadContent() {
            _background = _content.Load<Texture2D>("Background/Microscheme_0_edited");
        }

        public void Dispose() {
        }

        public bool Disposed {
            get { return false; }
        }

        /// <summary>
        /// Unlike almost all game objects, the background starts its own sprite batch.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( BackgroundAlpha <= .06 )
                return;

            // Background image
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap,
                               DepthStencilState.None, RasterizerState.CullNone);
            Vector2 origin = camera.Position;
            origin = ConvertUnits.ToDisplayUnits(origin) / 4;
            spriteBatch.Draw(_background, Vector2.Zero,
                              new Rectangle((int) origin.X, (int) origin.Y, spriteBatch.GraphicsDevice.Viewport.Bounds.Width,
                                            spriteBatch.GraphicsDevice.Viewport.Bounds.Height), Color.White * _backgroundAlpha);
            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            if ( _background == null ) {
                LoadRoom(PlayerPositionMonitor.Instance.CurrentRoom);
            }
            if ( EnceladusGame.Instance.Mode == Mode.RoomTransition && _backgroundAlpha > 0 ) {
                _backgroundAlpha -= .05f;
                if ( _backgroundAlpha < .01f ) {
                    _backgroundAlpha = 0f;
                }
            } else if ( _backgroundAlpha < 1 ) {
                _backgroundAlpha += .05f;
            }
        }

        public Vector2 Position {
            get { return Vector2.Zero; }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return true;
        }
    }
}

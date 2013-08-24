using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity {

    /// <summary>
    /// An echo of the player when sprinting
    /// </summary>
    public class PlayerEcho : GameEntityAdapter {
        private const double TimeToLiveMs = 350;

        private readonly Texture2D _image;
        private readonly Vector2 _drawPos;
        private readonly Timer _timeToLive;
        private bool _flipHorizontally;

        public PlayerEcho(Texture2D image, Vector2 drawPos, bool flipHoriztonally) {
            _image = image;
            _drawPos = drawPos;
            _timeToLive = new Timer(TimeToLiveMs);
            _flipHorizontally = flipHoriztonally;
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            var displayPos = ConvertUnits.ToDisplayUnits(_drawPos);
            var origin = new Vector2(_image.Width / 2f, _image.Height);
            float percentLeft = (float) (_timeToLive.TimeLeft / TimeToLiveMs);
            float alpha = .75f * percentLeft;
            if ( alpha > .05 ) {
                Color color = Color.Lerp(Player.Instance.Color, Color.Black, .95f - percentLeft);
                spriteBatch.Draw(_image, displayPos, null, color * alpha, 0f, origin,
                                 1f, _flipHorizontally ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
        }

        public override void Update(GameTime gameTime) {
            _timeToLive.Update(gameTime);
            if ( _timeToLive.IsTimeUp() ) {
                Dispose();
            }
        }
    }
}

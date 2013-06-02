using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.InteractiveObject {

    /// <summary>
    /// A collectible item can be collected once.
    /// </summary>
    public class GenericCollectibleItem : Region, IGameEntity{

        private CollectibleItem _itemType;
        private Body _body;
        private double _rotationTimer;
        private const double RotationTimeMs = 2000;        

        public void Dispose() {
            _body.Dispose();
        }

        public bool Disposed {
            get { return _body.IsDisposed; }
        }

        /// <summary>
        /// Each item is drawn as a 64x64 texture.
        /// </summary>
        private static readonly Dictionary<CollectibleItem, Texture2D> _textures = new Dictionary<CollectibleItem, Texture2D>();

        public GenericCollectibleItem(CollectibleItem itemType, Vector2 topLeft, Vector2 bottomRight, World world)
            : base(AdjustToTileBoundary(topLeft), AdjustToTileBoundary(topLeft) + new Vector2(TileLevel.TileSize)) {

            _itemType = itemType;
            _body = CreatePlayerSensor(world);

            _body.OnCollision += (a, b, contact) => {
                Collect();
                return true;
            };
        }

        private void Collect() {
            Player.Instance.Equipment.Collected(_itemType);
            ItemCollectionState.Instance.Collected(TopLeft);
            EnceladusGame.Instance.Register(new WheelAnnouncement());
            EnceladusGame.Instance.SetMode(Mode.Conversation);
            Dispose();
        }

        public static void LoadContent(ContentManager cm) {
            _textures[CollectibleItem.Wheel] = cm.Load<Texture2D>("Pickups/Wheel0000");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Texture2D texture = _textures[_itemType];

            Vector2 position = _body.Position;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            bool reversed = _rotationTimer > RotationTimeMs / 4 && _rotationTimer < 3 * RotationTimeMs / 4;
            Vector2 scale = new Vector2((float) Math.Abs(Math.Cos(_rotationTimer / RotationTimeMs * Math.PI * 2)), 1f);
            spriteBatch.Draw(texture, displayPosition, null, Player.Instance.Color, 0f, origin, scale,
                 reversed ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        public void Update(GameTime gameTime) {
            _rotationTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _rotationTimer >= RotationTimeMs ) {
                _rotationTimer -= RotationTimeMs;
            }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return true;
        }
    }

    /// <summary>
    /// The set of all powerups that can be gotten in the game
    /// </summary>
    public enum CollectibleItem {
        BasicGun,
        Missile,
        Wheel,
        Bomb,
        Sonar,
    }

    class WheelAnnouncement : GameEntityAdapter, IGameEntity {

        private bool _disposed = false;
        private double _timer = 5000;

        public override void Dispose() {
            _disposed = true;
            EnceladusGame.Instance.UnsetMode();
        }

        public override bool Disposed {
            get { return _disposed; }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 position = camera.Position;
            Vector2 screenCenter = ConvertUnits.ToDisplayUnits(position);

            Texture2D crouchImage = Player.Instance.CrouchAnimation[Player.CrouchAimStraightFrame];
            Texture2D lTriggerImage = SharedGraphicalAssets.LTrigger;
            Texture2D scootImage = Player.Instance.ScooterAnimation[Player.ScootFrame];
            SpriteFont font = SharedGraphicalAssets.DialogFont;
            Vector2 plusSignSize = font.MeasureString("+");
            Vector2 equalsSize = font.MeasureString("=");

            const int margin = 20;
            const int spacing = 20;
            String title = "Propulsion Wheel";
            Vector2 titleSize = font.MeasureString(title);

            Vector2 backdropSize =
                new Vector2(Math.Max(2 * margin + titleSize.X,
                                     margin + crouchImage.Width + spacing + plusSignSize.X + spacing +
                                     lTriggerImage.Width / 2f + equalsSize.X + scootImage.Width + margin),
                            margin + font.LineSpacing + Math.Max(crouchImage.Height, lTriggerImage.Height / 2f) + margin);
            Vector2 topLeft = screenCenter - backdropSize / 2f;

            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop,
                             new Rectangle((int) (topLeft.X), (int) topLeft.Y,
                                           (int) (backdropSize.X),
                                           (int) backdropSize.Y), Color.Black * .85f);

            Vector2 titlePos = new Vector2(screenCenter.X - titleSize.X / 2, topLeft.Y + margin);
            TextDrawing.DrawStringShadowed(font, spriteBatch, Color.White, title, titlePos);

            Vector2 plusPos = new Vector2(topLeft.X + margin + crouchImage.Width + spacing,
                                          topLeft.Y + margin + font.LineSpacing +
                                          Math.Max(crouchImage.Height, lTriggerImage.Height / 2f) / 2);
            TextDrawing.DrawStringShadowed(font, spriteBatch, Color.White, "+", plusPos);

            Vector2 crouchPos = new Vector2(topLeft.X + margin, topLeft.Y + margin + font.LineSpacing);
            spriteBatch.Draw(crouchImage, crouchPos, Player.Instance.Color);

            Vector2 triggerPos =
                new Vector2(
                    topLeft.X + margin + crouchImage.Width + spacing + plusSignSize.X + spacing +
                    lTriggerImage.Width / 4f,
                    topLeft.Y + margin + font.LineSpacing + crouchImage.Height / 2f + 30);
            spriteBatch.Draw(lTriggerImage,
                             new Rectangle((int) (triggerPos.X),
                                           (int) (triggerPos.Y),
                                           lTriggerImage.Width / 2,
                                           lTriggerImage.Height / 2),
                             new Rectangle(0, 0, lTriggerImage.Width, lTriggerImage.Height),
                             SolidColorEffect.DisabledColor, 0f,
                             new Vector2(lTriggerImage.Width / 2f, lTriggerImage.Height / 2f),
                             SpriteEffects.None, 0);

            Vector2 equalsPos = new Vector2(triggerPos.X + lTriggerImage.Width / 4f + spacing,
                                            topLeft.Y + margin + font.LineSpacing +
                                            Math.Max(crouchImage.Height, lTriggerImage.Height / 2f) / 2);
            TextDrawing.DrawStringShadowed(font, spriteBatch, Color.White, "=", equalsPos);

            Vector2 scooterPos =
                new Vector2(
                    equalsPos.X + equalsSize.X + spacing,
                    topLeft.Y + margin + font.LineSpacing + crouchImage.Height / 2f);
            spriteBatch.Draw(scootImage,
                             scooterPos,
                             Player.Instance.Color);
        }

        public override void Update(GameTime gameTime) {
            _timer -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timer <= 0 ) {
                Dispose();
            }
        }

        public override bool UpdateInMode(Mode mode) {
            return mode == Mode.Conversation;
        }

        public override bool DrawAsOverlay { get { return true; } }
    }
}

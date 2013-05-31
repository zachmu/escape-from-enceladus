using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Map;
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
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Control;
using Enceladus.Entity;
using Enceladus.Entity.InteractiveObject;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// A weapon selector overlay
    /// </summary>
    public class WeaponSelector : IOverlayElement {

        private const int Margin = 10;
        private const int TopOffset = 20;

        private int _selectedWeapon = 0;
        private static readonly Dictionary<int, CollectibleItem> _itemsByIndex = new Dictionary<int, CollectibleItem>();

        static WeaponSelector() {
            _itemsByIndex[0] = CollectibleItem.Beam;
            _itemsByIndex[1] = CollectibleItem.Holocube;
            _itemsByIndex[2] = CollectibleItem.Springboard;
        }

        public void Draw(SpriteBatch spriteBatch) {
            int width = spriteBatch.GraphicsDevice.Viewport.Width;
            Vector2 topLeftPos = new Vector2(width / 2 - _outline.Width / 2 - Margin, TopOffset);

            DrawBackdrop(topLeftPos, spriteBatch);
            DrawItemSelection(topLeftPos, spriteBatch);
        }

        private void DrawBackdrop(Vector2 topLeftPos, SpriteBatch spriteBatch) {
            int width = 2 * Margin + _outline.Width;
            int height = 2 * Margin + _outline.Height;
            Rectangle src = new Rectangle(0, 0, width, height);
            Rectangle dest = new Rectangle((int) topLeftPos.X, (int) topLeftPos.Y, width, height);
            spriteBatch.Draw(SharedGraphicalAssets.BlackBackdrop, dest, src, Color.White * .65f);
        }

        private void DrawItemSelection(Vector2 topLeftPos, SpriteBatch spriteBatch) {
            Vector2 pos = topLeftPos + new Vector2(Margin);
            spriteBatch.Draw(_outline, pos, Color.White);
            spriteBatch.Draw(_images[_selectedWeapon], pos, Color.White * .65f);
        }

        public static void LoadContent(ContentManager cm) {
            _outline = cm.Load<Texture2D>("Overlay/ItemSelection/ItemSelection0000");
            _beam = cm.Load<Texture2D>("Overlay/ItemSelection/ItemSelection0001");
            _holocube = cm.Load<Texture2D>("Overlay/ItemSelection/ItemSelection0002");
            _springboard = cm.Load<Texture2D>("Overlay/ItemSelection/ItemSelection0003");
            _images = new Texture2D[] {
                _beam,
                _holocube,
                _springboard
            };
        }

        public bool Update(GameTime gameTime) {
            if ( PlayerControl.Control.IsNewLeftWeaponScroll() ) {
                _selectedWeapon--;
                if ( _selectedWeapon < 0 ) {
                    _selectedWeapon = Player.Instance.Equipment.NumSelectableTools - 1;
                }
                Player.Instance.SelectedItemChanged(_itemsByIndex[_selectedWeapon]);
                return true;
            } else if ( PlayerControl.Control.IsNewRightWeaponScroll() ) {
                _selectedWeapon = (_selectedWeapon + 1) % Player.Instance.Equipment.NumSelectableTools;
                Player.Instance.SelectedItemChanged(_itemsByIndex[_selectedWeapon]);
                return true;
            }

            return false;
        }

        private static Texture2D _outline;
        private static Texture2D _beam;
        private static Texture2D _holocube;
        private static Texture2D _springboard;
        private static Texture2D[] _images;
    }
}

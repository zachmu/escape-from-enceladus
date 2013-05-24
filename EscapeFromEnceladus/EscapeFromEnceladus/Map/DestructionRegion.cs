using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace Enceladus.Map {

    /// <summary>
    /// A region of destructible tiles.
    /// </summary>
    public class DestructionRegion : Region {

        // Bitwise or of weapon flags
        public int _weaponVulnerability;

        private Body _body;

        public DestructionRegion(World world, Vector2 topLeft, Vector2 bottomRight, int weaponVulnerability)
            : base(AdjustToTileBoundary(topLeft), AdjustToTileBoundary(bottomRight)) {
            _body = BodyFactory.CreateRectangle(world, Width, Height, 0);
            _body.IsStatic = true;
            _body.IsSensor = true;
            _body.Position = Position;
            _body.UserData = UserData.NewDestructionRegion(this);

            _weaponVulnerability = weaponVulnerability;
        }

        public bool Contains(Tile t) {
            return Contains(t.Position + new Vector2(TileLevel.TileSize / 2f));
        }

        public bool DestroyedBy(Projectile projectile) {
            return (projectile.DestructionFlags & _weaponVulnerability) != 0;
        }

        public void Dispose() {
            _body.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace Arena.Map {

    /// <summary>
    /// A region of destructible tiles.
    /// </summary>
    public class DestructionRegion : Region {

        // Bitwise or of weapon flags
        public int _weaponVulnerability;

        public DestructionRegion(World world, Vector2 topLeft, Vector2 bottomRight, int weaponVulnerability ) {
            _topLeft = AdjustToTileBoundary(topLeft);
            _bottomRight = AdjustToTileBoundary(bottomRight);
            Body rectangle = BodyFactory.CreateRectangle(world, Width, Height, 0);
            rectangle.IsStatic = true;
            rectangle.IsSensor = true;
            rectangle.Position = Position;
            rectangle.UserData = UserData.NewDestructionRegion(this);

            _weaponVulnerability = weaponVulnerability;
        }

        public bool Contains(Tile t) {
            return Contains(t.Position + new Vector2(TileLevel.TileSize / 2f));
        }

        public bool DestroyedBy(Projectile projectile) {
            return (projectile.DestructionFlags & _weaponVulnerability) != 0;
        }
    }
}

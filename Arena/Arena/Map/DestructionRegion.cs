﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace Arena.Map {

    /// <summary>
    /// A region of destructible tiles.
    /// </summary>
    public class DestructionRegion : Region {

        public string WeaponName { get; private set; }

        public DestructionRegion(World world, Vector2 topLeft, Vector2 bottomRight, string weaponName) {
            _topLeft = AdjustToTileBoundary(topLeft);
            _bottomRight = AdjustToTileBoundary(bottomRight);
            WeaponName = weaponName;
            Body rectangle = BodyFactory.CreateRectangle(world, Width, Height, 0);
            rectangle.IsStatic = true;
            rectangle.IsSensor = true;
            rectangle.Position = Position;
        }

        public bool Contains(Tile t) {
            return Contains(t.Position + new Vector2(TileLevel.TileSize / 2f));
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Arena.Map.Object;

namespace Arena.Entity.Enemy {
    public class EnemyFactory {

        /// <summary>
        /// Creates an appropriate enemy from the descriptor given.
        /// </summary>
        public static AbstractEnemy CreateEnemy(Object obj, World world) {
            Vector2 pos = ConvertUnits.ToSimUnits(obj.X, obj.Y);

            switch(obj.Name ?? "") {
                case "worm":
                    return new Worm(pos, world);
                    break;
                default:
                    return new PacingEnemy(pos, world);
            }
        } 
    }
}

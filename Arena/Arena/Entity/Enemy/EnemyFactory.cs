using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Arena.Map.Object;

namespace Arena.Entity.Enemy {
    public class EnemyFactory {

        /// <summary>
        /// Creates an appropriate enemy from the descriptor given.
        /// </summary>
        public static AbstractEnemy CreateEnemy(Object obj, World world) {
            // We use the lower-left corner for objects, aligned to the nearest tile boundary. 
            // this is to start enemies off with their feet on the ground.
            Vector2 pos = Region.AdjustToTileBoundary(ConvertUnits.ToSimUnits(obj.X, obj.Y + obj.Height));            

            switch(obj.Name ?? "") {
                case "worm":
                    return new Worm(pos, world);
                    break;
                case "beetle":
                    bool clockwise = obj.Properties.ContainsKey("clockwise");
                    return new Beetle(pos, world, clockwise);
                    break;
                default:
                    return new PacingEnemy(pos, world);
            }
        } 
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;
using Enceladus.Farseer;
using Enceladus.Map;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Enceladus.Map.Object;

namespace Enceladus.Entity.Enemy {
    public static class EnemyFactory {

        /// <summary>
        /// Creates an appropriate enemy from the descriptor given.
        /// </summary>
        public static IGameEntity CreateEnemy(Map.Object obj, World world) {
            
            // The default position is the lower-left corner for objects, aligned to the nearest tile boundary. 
            // This is to start enemies off with their feet on the ground.
            Vector2 pos = Region.AdjustToTileBoundary(ConvertUnits.ToSimUnits(obj.X, obj.Y + obj.Height));            
            
            // Some enemies (like bosses) are predicated on a particular milestone not being met yet           
            if ( obj.Properties.ContainsKey("milestoneNotMet") ) {
                string milestone = obj.Properties["milestoneNotMet"];
                GameMilestone m;
                if ( GameMilestone.TryParse(milestone, true, out m) ) {
                    if ( GameMilestones.Instance.HasMilestoneOccurred(m) ) {
                        return null;
                    }
                } else {
                    throw new ArgumentException(string.Format("no milestone called {0}", milestone));
                }
            }

            // The name field of the object determines its type
            switch(obj.Name ?? "") {
                case "worm":
                    return new Worm(pos, world);
                    break;
                case "beetle":
                    bool clockwise = obj.Properties.ContainsKey("clockwise");
                    return new Beetle(pos, world, clockwise);
                    break;
                case "turret":
                    Direction facing = Direction.Left;
                    if ( obj.Properties.ContainsKey("direction") ) {
                        switch ( obj.Properties["direction"] ) {
                            case "left":
                                facing = Direction.Left;
                                break;
                            case "right":
                                facing = Direction.Right;
                                break;
                            case "down":
                                facing = Direction.Down;
                                break;
                            case "up":
                                facing = Direction.Up;
                                break;
                        }
                    }
                    pos = Region.AdjustToTileBoundary(ConvertUnits.ToSimUnits(obj.X + obj.Width / 2, obj.Y + obj.Height / 2));
                    return new Turret(pos, world, facing);
                default:
                    return new PacingEnemy(pos, world);
            }
        }
    }
}

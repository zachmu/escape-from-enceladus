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

namespace Enceladus.Entity.InteractiveObject {

    /// <summary>
    /// Factory to create the appropriate kind of interactive object 
    /// </summary>
    public class InteractiveObjectFactory {
        public const string Save = "save";
        public const string Powerup = "powerup";
        private const string Event = "event";

        public static IGameEntity Create(World world, Map.Object obj) {
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(obj.X, obj.Y));
            var bottomRight =
                ConvertUnits.ToSimUnits(new Vector2(obj.X + obj.Width, obj.Y + obj.Height));

            switch ( obj.Type ) {
                case Save:
                    return new SaveStation(world, obj.Name, topLeft, bottomRight);
                case Powerup:
                    return CreatePowerup(topLeft, bottomRight, world, obj);
                case Event:
                    return CreateEvent(topLeft, bottomRight, world, obj);
                default:
                    throw new ArgumentException("Unexpected type of object: %s", obj.Type);
            }
        }

        /// <summary>
        /// Creates and returns an event-trigger region if necessary. Otherwise returns null.
        /// </summary>
        private static IGameEntity CreateEvent(Vector2 topLeft, Vector2 bottomRight, World world, Object o) {
            switch ( o.Name ) {
                case "RobotSentry":
                    if ( GameMilestones.Instance.HasMilestoneOccurred(GameMilestone.DefeatedRobotSentry) ) {
                        return null;
                    } else {
                        return new RobotSentry(topLeft, bottomRight, world);
                    }
                default:
                    throw new ArgumentException("Unexpected object name: %s", o.Name);
            }           
        }

        /// <summary>
        /// Creates and returns a powerup collection of the appropriate type 
        /// -- unless it has already been collected, in which case returns null.
        /// </summary>
        private static IGameEntity CreatePowerup(Vector2 topLeft, Vector2 bottomRight, World world, Object o) {

            // If this powerup has already been collected, don't create it
            if ( ItemCollectionState.Instance.IsItemCollected(Region.AdjustToTileBoundary(topLeft)) ) {
                return null;
            }

            switch ( o.Name ) {
                case "Wheel":
                    return new GenericCollectibleItem(CollectibleItem.Wheel, topLeft, bottomRight, world);
                default:
                    throw new ArgumentException("Unexpected object name: %s", o.Name);
            }
        }

        /// <summary>
        /// Returns the position of the region given (its center).
        /// </summary>
        public static Vector2 GetPosition(Map.Object obj) {
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(obj.X, obj.Y));
            var bottomRight =
                ConvertUnits.ToSimUnits(new Vector2(obj.X + obj.Width, obj.Y + obj.Height));
            return (topLeft + bottomRight) / 2f;
        }
    }
}

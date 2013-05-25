using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Enceladus.Map.Object;

namespace Enceladus.Entity.InteractiveObject {

    /// <summary>
    /// Factory to create the appropriate kind of interactive object 
    /// </summary>
    public class InteractiveObjectFactory {
        public const string Save = "save";

        public static IGameEntity Create(World world, Map.Object obj) {
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(obj.X, obj.Y));
            var bottomRight =
                ConvertUnits.ToSimUnits(new Vector2(obj.X + obj.Width, obj.Y + obj.Height));

            switch ( obj.Type ) {
                case Save:
                    return new SaveStation(world, obj.Name, topLeft, bottomRight);
                default:
                    throw new ArgumentException("Unexpected type of object: %s", obj.Type);
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

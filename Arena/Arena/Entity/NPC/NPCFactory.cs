using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Map;
using System;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Arena.Map.Object;

namespace Arena.Entity.NPC {
    public class NPCFactory {

        private const float DefaultSensorWidth = 6f;

        public static NPC Create(Object npc, World world) {
            Vector2 pos = ConvertUnits.ToSimUnits(npc.X, npc.Y);
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(npc.X, npc.Y));
            var bottomRight =
                ConvertUnits.ToSimUnits(new Vector2(npc.X + npc.Width, npc.Y + npc.Height));
            float sensorWidth = DefaultSensorWidth;
            if ( npc.Properties.ContainsKey("SensorWidth") ) {
                sensorWidth = float.Parse(npc.Properties["SensorWidth"]);
            }

            switch ( npc.Name ) {
                case "ProfessorIapetus":
                    return new ProfessorIapetus(topLeft, bottomRight, world, sensorWidth);
                case "EnsignForecastle":
                    return new EnsignForecastle(topLeft, bottomRight, world, sensorWidth);
                case "ChefHawser":
                    return new ChefHawser(topLeft, bottomRight, world, sensorWidth);
                case "EnsignClew":
                    return new EnsignClew(topLeft, bottomRight, world, sensorWidth);
                case "EnsignLeeward":
                    return new EnsignLeeward(topLeft, bottomRight, world, sensorWidth);
                case "CaptainPurchase":
                    return new CaptainPurchase(topLeft, bottomRight, world, sensorWidth);
                case "EnsignGibe":
                    return new EnsignGibe(topLeft, bottomRight, world, sensorWidth);
                case "EnsignTaffrail":
                    return new EnsignTaffrail(topLeft, bottomRight, world, sensorWidth);
                case "ChiefMizzen":
                    return new ChiefMizzen(topLeft, bottomRight, world, sensorWidth);
                default:
                    throw new ArgumentException("Unrecognized NPC name " + npc.Name);
            }

            return null;
        }
    }
}

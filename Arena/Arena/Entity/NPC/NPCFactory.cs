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

        private static Dictionary<String, NPC> _npcs = new Dictionary<string, NPC>(); 

        /// <summary>
        /// Creates a new instance of the NPC map object given, 
        /// storing that copy of the NPC for reference.
        /// </summary>
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
                    return _npcs["ProfessorIapetus"] = new ProfessorIapetus(topLeft, bottomRight, world, sensorWidth);
                case "EnsignForecastle":
                    return _npcs["EnsignForecastle"] = new EnsignForecastle(topLeft, bottomRight, world, sensorWidth);
                case "ChefHawser":
                    return _npcs["ChefHawser"] = new ChefHawser(topLeft, bottomRight, world, sensorWidth);
                case "EnsignClew":
                    return _npcs["EnsignClew"] = new EnsignClew(topLeft, bottomRight, world, sensorWidth);
                case "EnsignLeeward":
                    return _npcs["EnsignLeeward"] = new EnsignLeeward(topLeft, bottomRight, world, sensorWidth);
                case "CaptainPurchase":
                    return _npcs["CaptainPurchase"] = new CaptainPurchase(topLeft, bottomRight, world, sensorWidth);
                case "EnsignGibe":
                    return _npcs["EnsignGibe"] = new EnsignGibe(topLeft, bottomRight, world, sensorWidth);
                case "EnsignTaffrail":
                    return _npcs["EnsignTaffrail"] = new EnsignTaffrail(topLeft, bottomRight, world, sensorWidth);
                case "ChiefMizzen":
                    return _npcs["ChiefMizzen"] = new ChiefMizzen(topLeft, bottomRight, world, sensorWidth);
                default:
                    throw new ArgumentException("Unrecognized NPC name " + npc.Name);
            }
        }

        /// <summary>
        /// Returns the NPC with the name given.  The NPC must have previous been created.
        /// </summary>
        public static IDialogEntity Get(String npcName) {
            if ( npcName == "Roark" ) {
                return new PlayerDialog();
            }

            return _npcs[npcName];
        }
    }
}

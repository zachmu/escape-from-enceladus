using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;
using Enceladus.Map;
using System;
using Enceladus.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Object = Enceladus.Map.Object;

namespace Enceladus.Entity.NPC {

    /// <summary>
    /// NPCs should be created via Create when they become active to 
    /// the game world, such as upon room load, and destroyed when 
    /// they no longer need to be physically present in a scene.
    /// </summary>
    public class NPCFactory {

        private const float DefaultSensorWidth = 6f;

        public const string CharProfessorIapetus = "ProfessorIapetus";
        public const string CharLieutenantForecastle = "LieutenantForecastle";
        public const string CharChefHawser = "ChefHawser";
        public const string CharEnsignLeeward = "EnsignLeeward";
        public const string CharCaptainPurchase = "CaptainPurchase";
        public const string CharEnsignGibe = "EnsignGibe";
        public const string CharCommanderTaffrail = "CommanderTaffrail";
        public const string CharChiefMizzen = "ChiefMizzen";
        public const string Announcement = "Announcement";
        public const string CharRobotSentry = "RobotSentry";

        private static readonly Dictionary<string, NPC> _activeNpcs = new Dictionary<string, NPC>(); 

        /// <summary>
        /// Creates a new instance of the NPC map object given. If the given character isn't in the scene, returns null.
        /// </summary>
        public static NPC Create(Map.Object npc, World world) {
            Vector2 pos = ConvertUnits.ToSimUnits(npc.X, npc.Y);
            var topLeft = ConvertUnits.ToSimUnits(new Vector2(npc.X, npc.Y));
            var bottomRight =
                ConvertUnits.ToSimUnits(new Vector2(npc.X + npc.Width, npc.Y + npc.Height));
            float sensorWidth = DefaultSensorWidth;
            if ( npc.Properties.ContainsKey("SensorWidth") ) {
                sensorWidth = Single.Parse(npc.Properties["SensorWidth"]);
            }

            switch ( npc.Name ) {
                case CharProfessorIapetus:
                    return CreateProfessorIapetus(world, topLeft, bottomRight, sensorWidth);
                case CharLieutenantForecastle:
                    return _activeNpcs[CharLieutenantForecastle] = new EnsignForecastle(topLeft, bottomRight, world, sensorWidth);
                case CharChefHawser:
                    return _activeNpcs[CharChefHawser] = new ChefHawser(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignLeeward:
                    return _activeNpcs[CharEnsignLeeward] = new EnsignLeeward(topLeft, bottomRight, world, sensorWidth);
                case CharCaptainPurchase:
                    return _activeNpcs[CharCaptainPurchase] = new CaptainPurchase(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignGibe:
                    return CreateEnsignGibe(world, topLeft, bottomRight, sensorWidth);
                case CharCommanderTaffrail:
                    return _activeNpcs[CharCommanderTaffrail] = new EnsignTaffrail(topLeft, bottomRight, world, sensorWidth);
                case CharChiefMizzen:
                    return _activeNpcs[CharChiefMizzen] = new ChiefMizzen(topLeft, bottomRight, world, sensorWidth);
                default:
                    throw new ArgumentException("Unrecognized NPC name " + npc.Name);
            }
        }

        /*
         * Some NPCs aren't always present in the scene, so those methods return null in that case.
         */

        private static NPC CreateEnsignGibe(World world, Vector2 topLeft, Vector2 bottomRight, float sensorWidth) {
            if ( GameMilestones.Instance.HasMilestoneOccurred(GameMilestone.Embarked) ) {
                return null;
            }
            return _activeNpcs[CharEnsignGibe] = new EnsignGibe(topLeft, bottomRight, world, sensorWidth);
        }

        private static NPC CreateProfessorIapetus(World world, Vector2 topLeft, Vector2 bottomRight, float sensorWidth) {
            if ( GameMilestones.Instance.HasMilestoneOccurred(GameMilestone.Embarked) ) {
                return null;
            }
            return _activeNpcs[CharProfessorIapetus] = new ProfessorIapetus(topLeft, bottomRight, world, sensorWidth);
        }

        /// <summary>
        /// Destroys the NPC given
        /// </summary>
        /// <param name="npc"></param>
        public static void Destroy(NPC npc) {
            _activeNpcs.Remove(npc.Name);
        }


        /// <summary>
        /// Returns the NPC dialog entity for the npc given. If the npc is in the room and 
        /// active, the npc is its own dialog. Otherwise, it's a disembodied voice with that 
        /// npc's color.
        /// </summary>
        public static IDialogEntity GetDialogEntity(String npcName) {
            if ( npcName == "Roark" ) {
                return new PlayerDialog();
            }

            if ( !_activeNpcs.ContainsKey(npcName) ) {
                switch ( npcName ) {
                    case CharProfessorIapetus:
                        return new DisembodiedSpeaker(ProfessorIapetus.CharacterColor, CharProfessorIapetus);
                    case CharLieutenantForecastle:
                        return new DisembodiedSpeaker(EnsignForecastle.CharacterColor, CharLieutenantForecastle);
                    case CharChefHawser:
                        return new DisembodiedSpeaker(ChefHawser.CharacterColor, CharChefHawser);
                    case CharEnsignLeeward:
                        return new DisembodiedSpeaker(EnsignLeeward.CharacterColor, CharEnsignLeeward);
                    case CharCaptainPurchase:
                        return new DisembodiedSpeaker(CaptainPurchase.CharacterColor, CharCaptainPurchase);
                    case CharEnsignGibe:
                        return new DisembodiedSpeaker(EnsignGibe.CharacterColor, CharEnsignGibe);
                    case CharCommanderTaffrail:
                        return new DisembodiedSpeaker(EnsignTaffrail.CharacterColor, CharCommanderTaffrail);
                    case CharChiefMizzen:
                        return new DisembodiedSpeaker(ChiefMizzen.CharacterColor, CharChiefMizzen);
                    default:
                        throw new ArgumentException("Unrecognized NPC name " + npcName);
                }
            }

            return _activeNpcs[npcName];
        }

        /// <summary>
        /// Gets a disembodied speaker to stand in for the NPC with the name given.
        /// </summary>
        /// <param name="npcName"></param>
        public static IDialogEntity GetDisembodiedSpeaker(String npcName) {
            IDialogEntity speaker = GetDialogEntity(npcName);
            return new DisembodiedSpeaker(speaker.Color, npcName);
        }
    }
}

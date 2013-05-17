﻿using System.Collections.Generic;
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

        public const string CharProfessorIapetus = "ProfessorIapetus";
        public const string CharEnsignForecastle = "EnsignForecastle";
        public const string CharChefHawser = "ChefHawser";
        public const string CharEnsignClew = "EnsignClew";
        public const string CharEnsignLeeward = "EnsignLeeward";
        public const string CharCaptainPurchase = "CaptainPurchase";
        public const string CharEnsignGibe = "EnsignGibe";
        public const string CharEnsignTaffrail = "EnsignTaffrail";
        public const string CharChiefMizzen = "ChiefMizzen";
        public const string Announcement = "Announcement";

        private static readonly Dictionary<string, NPC> _activeNpcs = new Dictionary<string, NPC>(); 

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
                sensorWidth = Single.Parse(npc.Properties["SensorWidth"]);
            }

            switch ( npc.Name ) {
                case CharProfessorIapetus:
                    return _activeNpcs[CharProfessorIapetus] = new ProfessorIapetus(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignForecastle:
                    return _activeNpcs[CharEnsignForecastle] = new EnsignForecastle(topLeft, bottomRight, world, sensorWidth);
                case CharChefHawser:
                    return _activeNpcs[CharChefHawser] = new ChefHawser(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignClew:
                    return _activeNpcs[CharEnsignClew] = new EnsignClew(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignLeeward:
                    return _activeNpcs[CharEnsignLeeward] = new EnsignLeeward(topLeft, bottomRight, world, sensorWidth);
                case CharCaptainPurchase:
                    return _activeNpcs[CharCaptainPurchase] = new CaptainPurchase(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignGibe:
                    return _activeNpcs[CharEnsignGibe] = new EnsignGibe(topLeft, bottomRight, world, sensorWidth);
                case CharEnsignTaffrail:
                    return _activeNpcs[CharEnsignTaffrail] = new EnsignTaffrail(topLeft, bottomRight, world, sensorWidth);
                case CharChiefMizzen:
                    return _activeNpcs[CharChiefMizzen] = new ChiefMizzen(topLeft, bottomRight, world, sensorWidth);
                default:
                    throw new ArgumentException("Unrecognized NPC name " + npc.Name);
            }
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
                        return new DisembodiedSpeaker(ProfessorIapetus.CharacterColor);
                    case CharEnsignForecastle:
                        return new DisembodiedSpeaker(EnsignForecastle.CharacterColor);
                    case CharChefHawser:
                        return new DisembodiedSpeaker(ChefHawser.CharacterColor);
                    case CharEnsignClew:
                        return new DisembodiedSpeaker(EnsignClew.CharacterColor);
                    case CharEnsignLeeward:
                        return new DisembodiedSpeaker(EnsignLeeward.CharacterColor);
                    case CharCaptainPurchase:
                        return new DisembodiedSpeaker(CaptainPurchase.CharacterColor);
                    case CharEnsignGibe:
                        return new DisembodiedSpeaker(EnsignGibe.CharacterColor);
                    case CharEnsignTaffrail:
                        return new DisembodiedSpeaker(EnsignTaffrail.CharacterColor);
                    case CharChiefMizzen:
                        return new DisembodiedSpeaker(ChiefMizzen.CharacterColor);
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
            return new DisembodiedSpeaker(speaker.Color);
        }
    }
}

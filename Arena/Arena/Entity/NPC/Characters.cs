using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Arena.Entity.NPC {
    internal class ProfessorIapetus : NPC {
        public static readonly Color CharacterColor = Color.Tomato;
        public ProfessorIapetus(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharProfessorIapetus, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignForecastle : NPC {
        public static readonly Color CharacterColor = Color.Plum;
        public EnsignForecastle(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharSergeantForecastle, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class ChefHawser : NPC {
        public static readonly Color CharacterColor = Color.Wheat;
        public ChefHawser(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharChefHawser, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignLeeward : NPC {
        public static readonly Color CharacterColor = Color.DarkOrange;
        public EnsignLeeward(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharEnsignLeeward, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignTaffrail : NPC {
        public static readonly Color CharacterColor = Color.DarkSalmon;

        public EnsignTaffrail(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharEnsignTaffrail, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class CaptainPurchase : NPC {
        public static readonly Color CharacterColor = Color.SpringGreen;

        public CaptainPurchase(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharCaptainPurchase, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignGibe : NPC {
        public static readonly Color CharacterColor = Color.DarkRed;

        public EnsignGibe(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharEnsignGibe, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class ChiefMizzen : NPC {
        public static readonly Color CharacterColor = Color.Aqua;

        public ChiefMizzen(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(NPCFactory.CharChiefMizzen, CharacterColor, topLeft, bottomRight, world, sensorWidth) {
        }
    }

}

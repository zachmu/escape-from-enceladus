using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Arena.Entity.NPC {
    internal class ProfessorIapetus : NPC {
        public ProfessorIapetus(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("ProfessorIapetus", Color.Tomato, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignForecastle : NPC {
        public EnsignForecastle(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("EnsignForecastle", Color.Plum, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class ChefHawser : NPC {
        public ChefHawser(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("ChefHawser", Color.Wheat, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignClew : NPC {
        public EnsignClew(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("EnsignClew", Color.Bisque, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignLeeward : NPC {
        public EnsignLeeward(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("EnsignLeeward", Color.DarkOrange, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignTaffrail : NPC {
        public EnsignTaffrail(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("EnsignTaffrail", Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class CaptainPurchase : NPC {
        public CaptainPurchase(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("CaptainPurchase", Color.SpringGreen, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class EnsignGibe : NPC {
        public EnsignGibe(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("EnsignGibe", Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }
    }

    internal class ChiefMizzen : NPC {
        public ChiefMizzen(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base("ChiefMizzen", Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }
    }

}

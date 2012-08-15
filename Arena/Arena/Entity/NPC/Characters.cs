using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Arena.Entity.NPC {
    internal class ProfessorIapetus : NPC {
        public ProfessorIapetus(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.Tomato, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "I hope you're enjoying your little jaunt, Lieutenant.",
                             "You fools have no idea who you're trifling with.",
                             "I do sincerely hope we don't experience any engine difficulty on the way.",
                             "That would provide me with an irresistible opportunity!",
                         };
        }
    }

    internal class EnsignForecastle : NPC {
        public EnsignForecastle(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.Plum, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "Professor Iapetus gives my the jibblies, Lieutenant.",
                             "He's always making veiled threats about us veering off course,",
                             "or hitting an asteroid, or getting space madness!",
                             "At the academy they told us space madness was a myth!",
                             "And the other guys are saying he's responsible for killing more people than Mecha-Hitler.",
                             "But that's ridiculous. That record is untouchable.",
                             "Why are we even taking him to Enceladus, any way?",
                             "If it were my call, he'd take a long walk down a short air lock.",
                         };
        }
    }

    internal class ChefHawser : NPC {
        public ChefHawser(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.Wheat, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "Ah, Lieutenant Roark!",
                             "I hope you've worked up an appetite with your security details.",
                             "We're having some nice, juicy venison replica tonight, fresh out of the vat!",
                         };
        }

    }

    internal class EnsignClew : NPC {
        public EnsignClew(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.Bisque, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "I told them not to assign me the top bunk.",
                             "I get vertigo!",
                             "<think calming thoughts, ensign!>",
                         };
        }

    }

    internal class EnsignLeeward : NPC {
        public EnsignLeeward(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.DarkOrange, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "What do you even do all day, with Professor Iapetus locked up behind his laser bars?",
                             "This must be a really nice gig for you!",
                             "Ensign Taffrail says you spend a lot of time \"in your bunk\", if you catch my drift."
                         };
        }

    }

    internal class EnsignTaffrail : NPC {
        public EnsignTaffrail(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "Don't pay any mind to Ensign Leeward. He's been in a terrible mood all day.",
                             "He got a hypergram from his ex-wife this morning.",
                             "I guess she's getting the moon resort in the divorce.  Poor guy.",
                             "He really loved that place.",
                         };
        }
    }

    internal class CaptainPurchase : NPC {
        public CaptainPurchase(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.SpringGreen, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "At ease, Lieutenant.",
                             "...just right now, I mean. In general, keep your eyes open and your ears peeled!",
                             "Iapetus is not to be trusted. He's managed to escape the Planetary Union too many times!",
                             "But not this time! And you're in charge of making sure of that!",
                         };
        }

    }

    internal class EnsignGibe : NPC {
        public EnsignGibe(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "Oh hey, Lieutenant Roark. I guess you found me.",
                             "I come here sometimes to just get away from the bustle of ship life, you know?",
                             "Look, don't tell Captain Purchase I'm here, OK?  I'll owe you one!",
                         };
        }

    }

    internal class ChiefMizzen : NPC {
        public ChiefMizzen(Vector2 topLeft, Vector2 bottomRight, World world, float sensorWidth)
            : base(Color.DarkSalmon, topLeft, bottomRight, world, sensorWidth) {
        }

        protected override string[] GetConversation() {
            return new[] {
                             "No time to talk, I have my hands full keeping this bird in the air.",
                             "Well, not air, I guess. Vaccuum.",
                             "What? How do the engines work?",
                             "<sigh>  You know I had to go to school for like 10 years to do this job, right?",
                             "I can't just distill all that knowledge down to a folksy metaphor on demand!",
                             "Anyway, shouldn't you be getting back to making sure that geriatric doesn't escape?",
                         };
        }
    }

}

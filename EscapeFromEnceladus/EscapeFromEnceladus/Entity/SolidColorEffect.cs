using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {
    
    /// <summary>
    /// Simple container for the solid-color shader effect
    /// </summary>
    public class SolidColorEffect {
        public static Effect Effect { get; private set; }
        public static Color DisabledColor { get; private set; }

        public static void LoadContent(ContentManager cm) {
            Effect = cm.Load<Effect>("Effects/SolidColor");
            DisabledColor = Color.Black;
        }
    }
}

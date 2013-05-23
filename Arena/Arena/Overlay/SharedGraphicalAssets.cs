﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Overlay {

    /// <summary>
    /// Static class to hold shared control graphical assets (buttons, etc)
    /// </summary>
    public class SharedGraphicalAssets {
        public static Texture2D YButton { get; private set; }
        public static SpriteFont DialogFont { get; private set; }
        public static Texture2D BlackBackdrop { get; private set; }

        public static void LoadContent(ContentManager cm) {
            YButton = cm.Load<Texture2D>("ButtonImages/xboxControllerButtonY");
            DialogFont = cm.Load<SpriteFont>("Fonts/November");
            DialogFont.LineSpacing -= 10;
            BlackBackdrop = cm.Load<Texture2D>("BlackBackdrop");
        }
    }
}
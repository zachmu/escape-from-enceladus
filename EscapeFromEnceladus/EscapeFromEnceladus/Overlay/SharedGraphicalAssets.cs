using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Overlay {

    /// <summary>
    /// Static class to hold shared control graphical assets (buttons, etc)
    /// </summary>
    public class SharedGraphicalAssets {
        public static Texture2D YButton { get; private set; }
        public static Texture2D LTrigger { get; private set; }
        public static SpriteFont DialogFont { get; private set; }
        public static Texture2D BlackBackdrop { get; private set; }
        public static SpriteFont TitleFont { get; private set; }
        public static Texture2D[] Projectiles { get; private set; }
        public const int ProjectileSphere16 = 0;
        public const int ProjectilePlayerBasic = 1;

        public static void LoadContent(ContentManager cm) {
            YButton = cm.Load<Texture2D>("ButtonImages/xboxControllerButtonY");
            LTrigger = cm.Load<Texture2D>("ButtonImages/xboxControllerLeftTrigger");
            DialogFont = cm.Load<SpriteFont>("Fonts/November");
            DialogFont.LineSpacing -= 10;
            TitleFont = cm.Load<SpriteFont>("Fonts/November128");
            BlackBackdrop = cm.Load<Texture2D>("BlackBackdrop");

            Projectiles = new Texture2D[2];
            for ( int i = 0; i < Projectiles.Length; i++ ) {
                Projectiles[i] = cm.Load<Texture2D>(String.Format("Projectile/Projectile{0:0000}", i));
            }
        }


    }
}

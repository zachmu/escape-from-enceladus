using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

/**
 * Camera class is responsible for knowing the bounds of the view
 */
namespace Test {

    class Camera {
        public Rectangle Viewport { get; private set; }

        public Camera(Rectangle viewport) {
            this.Viewport = viewport;
        }

        public void Pan(Vector2 delta) {
            Rectangle r = Viewport;
            r.X += (int)delta.X;
            r.Y += (int)delta.Y;
            Viewport = r;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena {
    class Shot {

        private readonly Body _body;

        public static Texture2D Image { get; set; }

        public Vector2 Position {
            get { return _body.Position; }
        }

        public Shot(Vector2 position, World world) {
            _body = BodyFactory.CreateRectangle(world, 3, 3, 100f);
            _body.IsStatic = false;
            _body.Restitution = .1f;
            _body.Friction = 1f;
            _body.Position = position;
            _body.FixedRotation = true;
            _body.IgnoreGravity = true;

            _body.OnCollision += (a, b, contact) => _body.IgnoreGravity = false;
        }

    }
}

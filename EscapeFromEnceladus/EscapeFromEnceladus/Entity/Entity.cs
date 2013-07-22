using System;
using Enceladus.Farseer;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity {
    public abstract class Entity {

        protected World _world;
        protected Body _body;

        public Vector2 Position { get { return _body.Position; } }

        protected readonly FlashAnimation _flashAnimation = new FlashAnimation();
        protected readonly StandingMonitor _standingMonitor = new StandingMonitor();

        // Returns where this entity is standing
        protected abstract Vector2 GetStandingLocation();

        protected void UpdateFlash(GameTime gameTime) {
            _flashAnimation.UpdateFlash(gameTime);
        }
    }
}
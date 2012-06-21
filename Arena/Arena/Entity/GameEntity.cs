using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {
    
    /// <summary>
    /// There's also a LoadContent implied, but for most classes this 
    /// should be a static method, so we can't define it here.
    /// </summary>
    public interface IGameEntity : IDisposable {
        bool Disposed { get; }
        Vector2 Position { get; }
        PolygonShape Shape { get; }

        void Draw(SpriteBatch spriteBatch, Camera2D camera);
        void Update(GameTime gameTime);
    }
}

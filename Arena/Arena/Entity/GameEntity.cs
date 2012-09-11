using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision;
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
        void Draw(SpriteBatch spriteBatch, Camera2D camera);
        void Update(GameTime gameTime);
        Vector2 Position { get; }
    }

    /// <summary>
    /// An IGameEntity that can collide with other game elements, so 
    /// that we need to track its boundaries.
    /// </summary>
    public interface ICollidableEntity : IGameEntity {
        AABB Aabb { get; }
    }
}

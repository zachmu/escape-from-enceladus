using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity {
    
    /// <summary>
    /// There's also a LoadContent implied, but for most classes this 
    /// should be a static method, so we can't define it here.
    /// </summary>
    public interface IGameEntity : IDisposable {
        bool Disposed { get; }
        void Draw(SpriteBatch spriteBatch, Camera2D camera);
        void Update(GameTime gameTime);
        Vector2 Position { get; }
        bool DrawAsOverlay { get; }
        bool UpdateInMode(Mode mode);
    }

    /// <summary>
    /// Basic adapter class for uncomplicated entities, 
    /// such as static announcements on a timer.
    /// </summary>
    public abstract class GameEntityAdapter : IGameEntity {
        public abstract void Dispose();
        public abstract bool Disposed { get; }
        
        public virtual void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            
        }

        public virtual void Update(GameTime gameTime) {
            
        }

        public virtual Vector2 Position {
            get { return Vector2.Zero; }
        }

        public virtual bool DrawAsOverlay {
            get { return false; }
        }

        public virtual bool UpdateInMode(Mode mode) {
            return true;
        }
    }

    /// <summary>
    /// An IGameEntity that can collide with other game elements, so 
    /// that we need to track its boundaries.
    /// </summary>
    public interface ICollidableEntity : IGameEntity {
        AABB Aabb { get; }
    }
}

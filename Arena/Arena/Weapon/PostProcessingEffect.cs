using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Weapon {

    /// <summary>
    /// A post-processing effect drawn over the entire screen.
    /// </summary>
    public abstract class PostProcessingEffect {

        public abstract bool Disposed { get; }
        public abstract Effect Effect { get; }

        /// <summary>
        /// Sets effect parameters for this pass.
        /// This enables a single Effect to be shared by multiple instances.
        /// </summary>
        public abstract void SetEffectParameters();
    }
}

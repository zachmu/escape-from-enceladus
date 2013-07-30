using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Weapon;

namespace Enceladus.Entity.Enemy {
    public interface IEnemy : IGameEntity {

        /// <summary>
        /// Returns how much damage getting hit by this enemy does
        /// </summary>
        int BaseDamage { get; }

        /// <summary>
        /// Handles being hit by the weapon given, 
        /// returning whether it did any damage.
        /// </summary>
        bool HitBy(IWeapon projectile);

        void DoDamage(float damage);
    }
}

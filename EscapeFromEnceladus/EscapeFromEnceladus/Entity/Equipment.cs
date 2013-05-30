using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Event;

namespace Enceladus.Entity {
    
    /// <summary>
    /// All the player's current equipment
    /// </summary>
    public class Equipment : ISaveable {

        private readonly HashSet<Powerup> _powerups = new HashSet<Powerup>();

        public Equipment() {
            _powerups.Add(Powerup.BasicGun);
        }

        /// <summary>
        /// Returns whether the player has collected the given powerup
        /// </summary>
        public bool HasPowerup(Powerup powerup) {
            return _powerups.Contains(powerup);
        }

        /// <summary>
        /// Add the given powerup to the set the player has collected. 
        /// Some powerups can be collected more than once.
        /// </summary>
        public void Collected(Powerup powerup) {
            _powerups.Add(powerup);
        }

        public void Save(SaveState save) {
            save.Equipment = this;
        }

        public void LoadFromSave(SaveState save) {
            _powerups.Clear();
            _powerups.UnionWith(save.Equipment._powerups);
        }
    }

    /// <summary>
    /// The set of all powerups that can be gotten in the game
    /// </summary>
    public enum Powerup {
        BasicGun,
        Missile,
        Wheel,
        Bomb,
        Sonar,
    }
}

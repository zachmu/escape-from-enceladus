using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enceladus.Entity.Enemy {
    
    /// <summary>
    /// Responsible for monitoring which enemies are active at any given time.
    /// </summary>
    public class EnemyMonitor {

        public event EnemyAdded OnEnemyAdded;
        public event EnemyRemoved OnEnemyRemoved;

        private readonly static EnemyMonitor _instance = new EnemyMonitor();
        public static EnemyMonitor Instance {
            get { return _instance; }
        }

        public void EnemyAdded(IEnemy enemy) {
            OnEnemyAdded(enemy);
        }

        public void EnemyRemoved(IEnemy enemy) {
            OnEnemyRemoved(enemy);
        }
    }

    public delegate void EnemyAdded(IEnemy enemy);
    public delegate void EnemyRemoved(IEnemy enemy);
}

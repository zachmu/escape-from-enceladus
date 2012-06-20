using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arena {

    /// <summary>
    /// Place to store data about bodies in the game world
    /// </summary>
    public class UserData {

        public bool IsTerrain { get; private set; }
        public bool IsEnemy { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool IsProjectile { get; private set; }

        public Enemy Enemy { get; private set; }
        public Shot Projectile { get; private set; }

        public static UserData NewTerrain() {
            return new UserData() { IsTerrain = true };
        }

        public static UserData NewEnemy(Enemy enemy) {
            return new UserData() { IsEnemy = true, Enemy = enemy };
        }

        public static UserData NewProjectile(Shot shot) {
            return new UserData() { IsProjectile = true, Projectile = shot };
        }

        public static UserData NewPlayer() {
            return new UserData() { IsPlayer = true };
        }
    }
}

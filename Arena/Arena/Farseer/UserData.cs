using Arena.Entity;
using Arena.Map;

namespace Arena.Farseer {

    /// <summary>
    /// Place to store data about bodies in the game world
    /// </summary>
    public class UserData {

        public bool IsTerrain { get; private set; }
        public bool IsEnemy { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool IsProjectile { get; private set; }
        public bool IsDoor { get; private set; }
        public bool IsDestructibleRegion { get; private set; }

        public Enemy Enemy { get; private set; }
        public Shot Projectile { get; private set; }
        public Door Door { get; private set; }
        public DestructionRegion Destruction { get; private set; }

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

        public static UserData NewDoor(Door door) {
            return new UserData() { IsDoor = true, Door = door };
        }

        public static UserData NewDestructionRegion(DestructionRegion destructionRegion) {
            return new UserData() { IsDestructibleRegion = true, Destruction = destructionRegion};
        }
    }
}

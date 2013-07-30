namespace Enceladus.Weapon {
    /// <summary>
    /// A basic weapon, capable of doing damage to things.
    /// </summary>
    public interface IWeapon {
        /// <summary>
        /// Returns the destruction flags for this projectile.
        /// </summary>
        int DestructionFlags { get; }

        /// <summary>
        /// Returns the base damage for this projectile.
        /// </summary>
        float BaseDamage { get; }
    }
}
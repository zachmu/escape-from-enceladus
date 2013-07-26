using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {
    
    /// <summary>
    /// Bare bones enemy projectile. Goes in a straight line until it hits something, then dies.
    /// </summary>
    public class EnemyProjectile : BodyEntityAdapter, IEnemy {

        public event ProjectileDisposed ProjectileDisposed;

        protected Texture2D _image;
        private double _timeToLiveMs = 5000;
        private World _world;

        public EnemyProjectile(Texture2D image, World world, Vector2 location, Vector2 velocity, float angle, float radius) {
            _world = world;

            _body = BodyFactory.CreateCircle(world, radius, 10, location);
            _body.Rotation = angle;
            _body.IsStatic = false;
            _body.IgnoreGravity = true;
            _body.UserData = UserData.NewEnemy(this);

            _body.CollisionCategories = EnceladusGame.EnemyCategory;
            _body.CollidesWith = EnceladusGame.PlayerCategory | EnceladusGame.PlayerProjectileCategory | EnceladusGame.TerrainCategory;

            _body.LinearVelocity = velocity;

            _body.OnCollision += (a, b, contact) => {
                if ( b.GetUserData().IsPlayer )
                    Player.Instance.HitBy(this);
                Dispose();
                return true;
            };
            _image = image;
        }

        public int BaseDamage {
            get { return 8; }
        }

        public bool HitBy(Projectile projectile) {
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, _image, SolidColorEffect.DisabledColor, null, _body.Position, 3, 3));
            Dispose();
            SoundEffectManager.Instance.PlaySoundEffect("enemyExplode");
            return true;
        }

        public override void Update(GameTime gameTime) {
            _timeToLiveMs -= gameTime.ElapsedGameTime.TotalMilliseconds;
            if ( _timeToLiveMs <= 0 ) {
                Dispose();
            }
        }

        public override void Dispose() {
            if ( !Disposed ) {
                ProjectileDisposed(this);
                base.Dispose();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_body.IsDisposed ) {
                Vector2 position = _body.Position;
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                spriteBatch.Draw(_image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, _image.Width, _image.Height),
                                 null, SolidColorEffect.DisabledColor, _body.Rotation,
                                 new Vector2(_image.Width / 2, _image.Height / 2),
                                 SpriteEffects.None, 0);
            }
        }
    }

    public delegate void ProjectileDisposed(EnemyProjectile projectile);

}

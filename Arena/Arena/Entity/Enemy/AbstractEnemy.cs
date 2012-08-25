using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.Enemy {

    public abstract class AbstractEnemy : Entity, IGameEntity {

        protected const string EnemySpeed = "Enemy speed (m/s)";

        protected int _hitPoints;
        protected abstract Texture2D Image { get; set; }
        private bool _disposed;
        protected Direction _direction;

        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position { get { return _body.Position; } }

        public PolygonShape Shape {
            get {
                AABB aabb = new AABB();
                foreach ( Fixture f in _body.FixtureList ) {
                    for ( int i = 0; i < f.ProxyCount; i++ ) {
                        AABB fab;
                        f.GetAABB(out fab, i);
                        aabb.Combine(ref fab);
                    }
                }
                return new PolygonShape(new Vertices(aabb.GetVertices()), 0);
            }
        }

        public AbstractEnemy(Vector2 position, World world, float width, float height) {
            CreateBody(position, world, width, height);
            ConfigureBody(position, height);

            _hitPoints = 5;
            _direction = Direction.Left;

            _body.OnCollision += (a, b, contact) => {

                HitSolidObject(contact, b);

                if ( b.Body.GetUserData().IsTerrain ) {
                    HitByTerrain();
                }

                if ( b.Body.GetUserData().IsPlayer ) {
                    Player.Instance.HitBy(this);
                }

                return true;
            };

            _body.OnSeparation += (a, b) => UpdateStanding();

            _world = world;
        }

        protected virtual void HitSolidObject(Contact contact, Fixture b) {
        }

        protected void HitByTerrain() {
            UpdateStanding();
        }

        protected virtual void CreateBody(Vector2 position, World world, float width, float height) {
            _body = BodyFactory.CreateRectangle(world, width, height, 10f);
        }

        protected virtual void ConfigureBody(Vector2 position, float height) {
            _body.IsStatic = false;
            _body.FixedRotation = true;
            _body.SleepingAllowed = false;
            _body.Friction = 0;
            // the position provided by the factor is the lower-left corner of the map area, tile-aligned
            _body.Position = position - new Vector2(0, height / 2);
            _body.CollidesWith = Arena.PlayerCategory | Arena.PlayerProjectileCategory | Arena.TerrainCategory;
            _body.CollisionCategories = Arena.EnemyCategory;
            _body.UserData = UserData.NewEnemy(this);
        }

        public abstract void Draw(SpriteBatch spriteBatch, Camera2D camera);

        public virtual void Update(GameTime gameTime) {
            if ( _hitPoints <= 0 ) {
                Destroyed();
                return;
            }

            UpdateFlash(gameTime);
        }

        public void HitBy(Projectile shot) {
            _hitPoints -= shot.BaseDamage;
            _flashTime = 150;
        }

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
        }

        private void Destroyed() {
            Arena.Instance.Register(new HealthPickup(_body.Position, _world));
            Arena.Instance.Register(new ShatterAnimation(_world, Image, null, _body.Position, 8));
            Dispose();
        }
    }
}

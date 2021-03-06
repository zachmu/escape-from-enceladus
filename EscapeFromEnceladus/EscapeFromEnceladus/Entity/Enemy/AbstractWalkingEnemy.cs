﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {

    public abstract class AbstractWalkingEnemy : IEnemy {

        protected const string EnemySpeed = "Enemy speed (m/s)";

        protected float _hitPoints;
        protected abstract Texture2D Image { get; set; }
        private bool _disposed;
        protected Direction _direction;
        protected float _height;
        protected World _world;
        protected Body _body;
        protected readonly FlashAnimation _flashAnimation = new FlashAnimation();
        protected readonly StandingMonitor _standingMonitor = new StandingMonitor();

        public bool Disposed {
            get { return _disposed; }
        }

        protected AbstractWalkingEnemy(Vector2 position, World world, float width, float height) {
            _height = height;
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
            // the position provided by the factory is the lower-left corner of the map area, tile-aligned
            _body.Position = position - new Vector2(0, height / 2);
            _body.CollidesWith = EnceladusGame.PlayerCategory | EnceladusGame.PlayerProjectileCategory | EnceladusGame.TerrainCategory;
            _body.CollisionCategories = EnceladusGame.EnemyCategory;
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

        public bool DrawAsOverlay {
            get { return false; }
        }

        public Vector2 Position { get { return _body.Position; } }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        public bool HitBy(IWeapon shot) {
            DoDamage(shot.BaseDamage);
            return true;
        }

        private void DamageTaken() {
            _flashAnimation.SetFlashTime(150);
            SoundEffectManager.Instance.PlaySoundEffect("enemyHit");
        }

        public void DoDamage(float damage) {
            DamageTaken();
            _hitPoints -= damage;
        }

        public virtual void SpringboardLaunch() {
            _body.LinearVelocity = new Vector2(_body.LinearVelocity.X, -20);
        }

        public void Dispose() {
            _disposed = true;
            _body.Dispose();
        }

        protected virtual void Destroyed() {
            Dispose();
            // TODO: move this responsibility into pickup generator
            EnceladusGame.Instance.Register(new HealthPickup(_body.Position, _world));
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, Image, null, _body.Position, 8));
            SoundEffectManager.Instance.PlaySoundEffect("enemyExplode");
        }

        /// <summary>
        /// Updates the standing and ceiling status using the body's current contacts.
        /// </summary>
        protected void UpdateStanding() {
            _standingMonitor.UpdateStanding(_body, _world, GetStandingLocation(), 0f);
        }

        // Returns where the enemy is standing
        protected abstract Vector2 GetStandingLocation();

        protected void UpdateFlash(GameTime gameTime) {
            _flashAnimation.UpdateFlash(gameTime);
        }

        public abstract int BaseDamage { get; }
    }
}

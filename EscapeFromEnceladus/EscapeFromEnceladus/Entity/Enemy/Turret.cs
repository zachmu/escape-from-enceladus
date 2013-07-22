﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {
    public class Turret : GameEntityAdapter, IGameEntity {

        protected const int NumFrames = 4;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
        private const int Barrel = 0;
        private const int Cover = 1;
        private const int WeakSpot = 2;
        private const int Hatch = 3;

        private const int ImageHeight = 64;
        private const int ImageWidth = 64;
        private const float Height = 1f;
        private const float Width = .5f;
        private const float Radius = Width;
        private const float TurretSpeedRps = .20f;

        private readonly Body _body;
        private readonly Direction _facingDirection;
        private float _barrelTargetRadians;
        private float _barrelAimRadians;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Enemy/Turret/Turret{0:0000}", i));
            }
        }

        public Turret(Vector2 position, World world, Direction facing) {
            _facingDirection = facing;
            
            _body = BodyFactory.CreateSolidArc(world, 1f, Projectile.Pi, 8, Radius, Vector2.Zero, Projectile.PiOverTwo);

            switch ( _facingDirection ) {
                case Direction.Left:
                    position += new Vector2(0, Height / 2);
                    break;
                case Direction.Right:
                    position += new Vector2(0, Height / 2);
                    _body.Rotation = -Projectile.Pi;
                    break;
                case Direction.Up:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = Projectile.PiOverTwo;
                    break;
                case Direction.Down:
                    position += new Vector2(Height / 2, 0);
                    _body.Rotation = -Projectile.PiOverTwo;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _body.IsStatic = true;
            _body.IgnoreGravity = true;
            _body.Position = position;
            _body.CollisionCategories = EnceladusGame.EnemyCategory;
            _body.CollidesWith = Category.All;
        }

        public override void Dispose() {
            _body.Dispose();
        }

        public override bool Disposed {
            get { return _body.IsDisposed; }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 position = _body.Position;

            Vector2 origin = new Vector2(ImageWidth, ImageHeight / 2f);

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            Color color = SolidColorEffect.DisabledColor;

            float bodyRotation = -_body.Rotation;
            if ( _facingDirection == Direction.Up || _facingDirection == Direction.Down ) {
                bodyRotation = _body.Rotation;
            }

            float barrelRotation = (float) (Projectile.Pi - _barrelAimRadians);

            spriteBatch.Draw(Animation[Barrel], displayPosition, null, color, barrelRotation, origin, 1f,
                             SpriteEffects.None, 0);

            spriteBatch.Draw(Animation[WeakSpot], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Hatch], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
            spriteBatch.Draw(Animation[Cover], displayPosition, null, color, bodyRotation, origin, 1f,
                             SpriteEffects.None, 0);
        }

        public override void Update(GameTime gameTime) {
            DetermineTargetAngle();
            UpdateBarrelAngle(gameTime);
        }

        private void UpdateBarrelAngle(GameTime gameTime) {
            float target;
            float current;
            float maxMovement = (float) (TurretSpeedRps * gameTime.ElapsedGameTime.TotalSeconds * Projectile.Pi * 2);

            switch ( _facingDirection ) {
                case Direction.Left: // angle > pi/2 || angle < -pi/2
                    target = NormalizeAngle(_barrelTargetRadians);
                    current = NormalizeAngle(_barrelAimRadians);
                    break;
                case Direction.Right: // angle < pi/2 && angle > -pi/2
                case Direction.Up: // angle > 0 && angle < pi/2
                    target = _barrelTargetRadians;
                    current = _barrelAimRadians;
                    break;
                case Direction.Down: // angle < 0 && angle > -pi/2
                    target = _barrelTargetRadians;
                    // this orientation, uniquely, has to worry about rotating in 
                    // the opposite direction when pointing to the left.
                    if ( target == Projectile.Pi ) {
                        target = -Projectile.Pi;
                    }
                    current = _barrelAimRadians;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            float diff = target - current;
            if ( diff >= 0 ) {
                diff = Math.Min(diff, maxMovement);
            } else {
                diff = Math.Max(diff, -maxMovement);
            }
            _barrelAimRadians = DenormalizeAngle(current + diff);

        }

        /// <summary>
        /// Returns an angle in the range [0,2pi)
        /// </summary>
        private float NormalizeAngle(float angle) {
            if ( angle < 0 ) {
                return (float) (2 * Projectile.Pi + angle);
            } else {
                return angle;
            }
        }

        /// <summary>
        /// Returns an angle in the range (-pi,pi]
        /// </summary>
        private float DenormalizeAngle(float angle) {
            if ( angle <= Projectile.Pi ) {
                return angle;
            } else {
                return (float) (angle - Projectile.Pi * 2);
            }
        }

        /// <summary>
        /// Determines where the aim target should be, constrained to turret's range
        /// </summary>
        private void DetermineTargetAngle() {
            Vector2 diff = Player.Instance.Position - Position;
            float angle = (float) Math.Atan2(-diff.Y, diff.X);

            // Depending on our facing direction, we need to determine if the player
            // is in our 180 degree range and adjust the target accordingly.
            switch ( _facingDirection ) {
                case Direction.Left:
                    if ( angle >= Projectile.PiOverTwo || angle <= -Projectile.PiOverTwo ) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle > 0 ? Projectile.PiOverTwo : -Projectile.PiOverTwo;
                    }
                    break;
                case Direction.Right:
                    if ( angle >= -Projectile.PiOverTwo && angle <= Projectile.PiOverTwo ) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle > 0 ? Projectile.PiOverTwo : -Projectile.PiOverTwo;
                    }
                    break;
                case Direction.Up:
                    if ( angle >= 0 ) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle > -Projectile.PiOverTwo ? 0 : Projectile.Pi;
                    }
                    break;
                case Direction.Down:
                    if ( angle <= 0 ) {
                        _barrelTargetRadians = angle;
                    } else {
                        _barrelTargetRadians = angle < Projectile.PiOverTwo ? 0 : Projectile.Pi;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override Vector2 Position {
            get { return _body.Position; }
        }
    }
}

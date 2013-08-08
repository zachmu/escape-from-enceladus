using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Overlay;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// The beam weapon, which burns a continuous path through enemies.
    /// </summary>
    public class Beam : GameEntityAdapter, IWeapon {

        private static Texture2D _image;
        private Direction _direction;
        private Vector2 _start;
        private Vector2 _end;
        private float _angle;
        private World _world;

        private const int ImageHeight = 16;
        private const int ImageWidth = 16;
        private const int MaxRange = 30;
        private const float DamagePerSecond = 50;

        public static void LoadContent(ContentManager cm) {
            _image = cm.Load<Texture2D>("Projectile/Projectile0002");
        }

        public Beam(World world, Vector2 start, Direction direction) {
            Update(world, start, direction);
        }

        public void Update(World world, Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
            _world = world;
            DetermineLength();
        }

        private void DetermineLength() {

            // Don't forget to invert the y coordinate because of the differing y axes
            Vector2 end = _start + new Vector2((float) Math.Cos(_angle) * MaxRange,
                                               (float) -Math.Sin(_angle) * MaxRange);

            float closestFraction = 1;
            Vector2 closestPoint = end;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( ((fixture.GetUserData().IsDoor && !fixture.GetUserData().Door.IsOpen())
                      || fixture.GetUserData().IsTerrain) && fraction < closestFraction ) {
                    closestFraction = fraction;
                    closestPoint = point;
                }
                return 1;
            }, _start, end);

            _end = closestPoint;
        }

        private static readonly Constants Constants = Constants.Instance;
        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {

            Vector2 drawStart = _start;
            Vector2 drawEnd = _end;

            if ( Constants.Get(Sonar.WeaponDrawDebug) >= 1 ) {
                drawStart +=
                    new Vector2(ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetX]),
                                ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetY]));
                drawEnd +=
                    new Vector2(ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetX]),
                                ConvertUnits.ToSimUnits(Constants[Projectile.ProjectileOffsetY]));

                Vector2 debugLocation = ConvertUnits.ToDisplayUnits(drawEnd);
                spriteBatch.Draw(SharedGraphicalAssets.DebugMarker, debugLocation, SolidColorEffect.DisabledColor);
            }

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(drawStart);
            Vector2 origin = new Vector2(0, ImageHeight / 2);

            float unitLegth = ConvertUnits.ToSimUnits(ImageWidth);
            float lengthRatio = (drawEnd - drawStart).Length() / unitLegth;

            spriteBatch.Draw(_image, displayPosition, null, SolidColorEffect.DisabledColor,
                             Projectile.GetSpriteRotation(_direction), origin, new Vector2(lengthRatio, 1.0f),
                             SpriteEffects.None, 1.0f);
        }

        public override void Update(GameTime gameTime) {
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( (fixture.GetUserData().IsDoor) ) {
                    fixture.GetUserData().Door.HitBy(this);
                } else if ( fixture.GetUserData().IsEnemy ) {
                    fixture.GetUserData().Enemy.DoDamage((float) (gameTime.ElapsedGameTime.TotalSeconds * DamagePerSecond));
                }
                return -1;
            }, _start, _end);
        }

        public int DestructionFlags {
            get { return EnceladusGame.BeamDestructionFlag; }
        }

        public float BaseDamage {
            get { return 1; }
        }
    }

}

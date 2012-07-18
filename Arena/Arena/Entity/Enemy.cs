using System;
using System.Linq;
using Arena.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Entity {
    
    public class Enemy : Entity, IGameEntity {

        private int _hitPoints;

        private static Texture2D _image;
        private bool _disposed;

        private const float CharacterWidth = 1;
        private const float CharacterHeight = 1;

        private Direction _direction;

        private readonly Fixture _floorSensor;
        private int _floorSensorContactCount = 0;
        
        private const string EnemySpeed = "Enemy speed (m/s)";

        static Enemy() {
            Constants.Register(new Constant(EnemySpeed, 3f, Keys.E));
        }

        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position { get { return _body.Position; } }

        public PolygonShape Shape {
            get { return (PolygonShape) _body.FixtureList.First().Shape; }
        }

        public Enemy(Vector2 position, World world) {
            _body = BodyFactory.CreateRectangle(world, 1f, 1f, 10f);
            _body.IsStatic = false;
            _body.FixedRotation = true;
            _body.SleepingAllowed = false;
            _body.Friction = 0;
            _body.Position = position;
            _body.CollidesWith = Arena.PlayerCategory | Arena.PlayerProjectileCategory | Arena.TerrainCategory;
            _body.CollisionCategories = Arena.EnemyCategory;
            _body.UserData = UserData.NewEnemy(this);

            _hitPoints = 5;
            _direction = Direction.Left;

            _body.OnCollision += (a, b, contact) => {

                if ( b.Body.GetUserData().IsPlayer || b.Body.GetUserData().IsTerrain || b.Body.GetUserData().IsDoor ) {
                    if ( contact.Manifold.LocalNormal.X > .9 ) {
                        _direction = Direction.Right;
                    } else if ( contact.Manifold.LocalNormal.X < -.9 ) {
                        _direction = Direction.Left;
                    }
                }

                if (b.Body.GetUserData().IsTerrain) {
                    UpdateStanding();
                }

                if ( b.Body.GetUserData().IsPlayer ) {
                    Player.Instance.HitBy(this);
                }

                return true;
            };

            _body.OnSeparation += (a, b) => UpdateStanding();

            _world = world;
        }

        public static void LoadContent(ContentManager content) {
            _image = content.Load<Texture2D>("enemy");
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !_disposed ) {
                Vector2 position = _body.Position;
                position.X -= CharacterWidth / 2f;
                position.Y -= CharacterHeight / 2f;

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                spriteBatch.Draw(_image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, _image.Width,
                                               _image.Height),
                                 null, Color.White, 0f, new Vector2(),
                                 _direction == Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);
            }
        }

        public void Update(GameTime gameTime) {
            if (_hitPoints <= 0) {
                Dispose();
                return;
            }

            if ( IsStanding ) {
                if ( _direction == Direction.Left ) {
                    _body.LinearVelocity = new Vector2(-Constants.Get(EnemySpeed), 0);
                } else {
                    _body.LinearVelocity = new Vector2(Constants.Get(EnemySpeed), 0);
                }
            }
        }

        public void HitBy(Projectile shot) {
            _hitPoints--;
        }

        public void Dispose() {
            _disposed = true;
            Arena.Instance.Register(new HealthPickup(_body.Position, _world));
            _body.Dispose();
        }
    }
}

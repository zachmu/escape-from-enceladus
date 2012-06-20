using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {
    
    public class Enemy : IDisposable {

        private readonly Body _body;

        private int _hitPoints;

        private Texture2D _image;
        private bool _disposed;

        private const float CharacterWidth = 1;
        private const float CharacterHeight = 1;

        private Player.Direction _direction;

        private readonly Fixture _floorSensor;
        private int _floorSensorContactCount = 0;
        
        private const string EnemySpeed = "Enemy speed (m/s)";

        static Enemy() {
            Constants.Register(new Constant(EnemySpeed, 3f, Keys.E));
        }

        public Vector2 Position { get { return _body.Position; } }

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
            _direction = Player.Direction.Left;

            _body.OnCollision += (a, b, contact) => {

                if ( ((UserData) b.Body.UserData).IsPlayer || ((UserData) b.Body.UserData).IsTerrain ) {
                    if ( contact.Manifold.LocalNormal.X > .9 ) {
                        _direction = Player.Direction.Right;
                    } else if ( contact.Manifold.LocalNormal.X < -.9 ) {
                        _direction = Player.Direction.Left;
                    }
                }

                if (((UserData)b.Body.UserData).IsPlayer) {
                    Player.Instance.HitBy(this);
                }

                return true;
            };

            var rectangle = PolygonTools.CreateRectangle(CharacterWidth / 2f - .1f, .05f);
            Vertices translated = new Vertices(rectangle.Count);
            translated.AddRange(rectangle.Select(v => v + new Vector2(0, CharacterHeight / 2f)));

            Shape footSensorShape = new PolygonShape(translated, 0f);
            _floorSensor = _body.CreateFixture(footSensorShape);
            _floorSensor.IsSensor = true;
            _floorSensor.UserData = "floor";
            _floorSensor.OnCollision += (a, b, contact) => {
                _floorSensorContactCount++;
                Console.WriteLine("Hit floor");
                return true;
            };
            _floorSensor.OnSeparation += (a, b) => {
                // For some reason we sometimes get "duplicate" leaving events due to body / fixture destruction
                if ( _floorSensorContactCount > 0 ) {
                    _floorSensorContactCount--;
                    Console.WriteLine("Left floor");
                }
            };

        }

        public void LoadContent(ContentManager content) {
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
                                 _direction == Player.Direction.Right
                                     ? SpriteEffects.None
                                     : SpriteEffects.FlipHorizontally, 0);
            }
        }

        public void Update(GameTime gameTime) {
            if (_hitPoints <= 0) {
                Dispose();
                return;
            }

            if ( IsStanding() ) {
                if ( _direction == Player.Direction.Left ) {
                    _body.LinearVelocity = new Vector2(-Constants.Get(EnemySpeed), 0);
                } else {
                    _body.LinearVelocity = new Vector2(Constants.Get(EnemySpeed), 0);
                }
            }
        }

        private bool IsStanding() {
            return _floorSensorContactCount > 0;
        }

        public void HitBy(Shot shot) {
            _hitPoints--;
        }

        public void Dispose() {
            _body.Dispose();
            _disposed = true;
        }
    }
}

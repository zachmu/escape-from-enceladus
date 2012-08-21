using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity.Enemy {

    public class Beetle : AbstractEnemy {

        private Texture2D _image;
        private int _animationFrame = 0;
        private int _timeSinceLastUpdate;

        private const float Height = .5f;
        private const float Width = 1;

        private const float turnTimeMs = 250;

        private bool _clockwise = false;
        private bool _turning = false;
        private bool _concaveTurn = false;
        private Vector2 _worldTurningJoint;
        private int _ignoreTurnsMs = 200;

        private Direction _direction;

        protected override Texture2D Image {
            get { return _image; }
            set {
                _timeSinceLastUpdate = 0;
                _image = value;
            }
        }

        public Beetle(Vector2 position, World world, bool clockwise)
            : base(position, world, Width, Height) {
            _image = Animation[0];
            _clockwise = clockwise;
            if ( _clockwise ) {
                _direction = Direction.Right;
            } else {
                _direction = Direction.Left;
            }
            _ignoreTurnsMs = 200;
        }

        protected override void CreateBody(Vector2 position, World world, float width, float height) {
            base.CreateBody(position, world, width, height);
            _body.IgnoreGravity = true;
        }

        protected override void HitSolidObject(FarseerPhysics.Dynamics.Contacts.Contact contact, Fixture b) {
            if ( !_turning && (b.GetUserData().IsTerrain || b.GetUserData().IsDoor) && _ignoreTurnsMs <= 0 ) {
                Console.WriteLine("HIT");
                _turning = true;
                _concaveTurn = true;
                if ( _clockwise ) {
                    _worldTurningJoint = _body.GetWorldPoint(new Vector2(Width / 2, Height / 2));
                } else {
                    _worldTurningJoint = _body.GetWorldPoint(new Vector2(-Width / 2, Height / 2));
                }
            } else if ( b.GetUserData().IsPlayer ) {
                _clockwise = !_clockwise;
                switch ( _direction ) {
                    case Direction.Left:
                        _direction = Direction.Right;
                        break;
                    case Direction.Right:
                        _direction = Direction.Left;
                        break;
                    case Direction.Up:
                        _direction = Direction.Down;
                        break;
                    case Direction.Down:
                        _direction = Direction.Up;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // draw position is character's center
                Vector2 position = _body.Position;

                //Vector2 scale = new Vector2(_width / BaseWidth, _height / BaseHeight);
                Vector2 origin = new Vector2(Image.Width / 2f, Image.Height / 2f);

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _drawSolidColor ? _flashColor : SolidColorEffect.DisabledColor;

                spriteBatch.Draw(Image, displayPosition, null, color, _body.Rotation, origin, 1f,
                                 _clockwise ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        private const int NumFrames = 5;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
       
        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Enemy/Beetle/Beetle{0:0000}", i));
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            _ignoreTurnsMs -= (int) gameTime.ElapsedGameTime.TotalMilliseconds;

            if ( _turning ) {
                HandleTurn(gameTime);
            } else {
                HandleMove();
            }

            UpdateAnimation(gameTime);
        }

        private void HandleMove() {
            switch ( _direction ) {
                case Direction.Left:
                    _body.LinearVelocity = new Vector2(-2, 0);
                    break;
                case Direction.Right:
                    _body.LinearVelocity = new Vector2(2, 0);
                    break;
                case Direction.Up:
                    _body.LinearVelocity = new Vector2(0, -2);
                    break;
                case Direction.Down:
                    _body.LinearVelocity = new Vector2(0, 2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if ( _ignoreTurnsMs <= 0 ) {
                Vector2 frontEdge;
                Vector2 rayDest;

                switch ( _direction ) {
                    case Direction.Left:
                        if ( _clockwise ) {
                            frontEdge = _body.Position +
                                        new Vector2(0, -Height / 2f);
                            rayDest = frontEdge + new Vector2(0, -Height / 2f);
                        } else {
                            frontEdge = _body.Position +
                                        new Vector2(0, Height / 2f);
                            rayDest = frontEdge + new Vector2(0, Height / 2f);
                        }
                        break;
                    case Direction.Right:
                        if ( _clockwise ) {
                            frontEdge = _body.Position +
                                        new Vector2(0, Height / 2f);
                            rayDest = frontEdge + new Vector2(0, Height / 2f);
                        } else {
                            frontEdge = _body.Position +
                                        new Vector2(0, -Height / 2f);
                            rayDest = frontEdge + new Vector2(0, -Height / 2f);
                        }
                        break;
                    case Direction.Up:
                        if ( _clockwise ) {
                            frontEdge = _body.Position +
                                        new Vector2(Height / 2f, 0);
                            rayDest = frontEdge + new Vector2(Height / 2f, 0);
                        } else {
                            frontEdge = _body.Position +
                                        new Vector2(-Height / 2f, 0);
                            rayDest = frontEdge + new Vector2(-Height / 2f, 0);
                        }
                        break;
                    case Direction.Down:
                        if ( _clockwise ) {
                            frontEdge = _body.Position +
                                        new Vector2(-Height / 2f, 0);
                            rayDest = frontEdge + new Vector2(-Height / 2f, 0);
                        } else {
                            frontEdge = _body.Position +
                                        new Vector2(+Height / 2f, 0);
                            rayDest = frontEdge + new Vector2(+Height / 2f, 0);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                bool cliffSensed = true;

                _world.RayCast((fixture, point, normal, fraction) => {
                    if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor ) {
                        cliffSensed = false;
                        return 0;
                    }
                    return -1;
                }, frontEdge, rayDest);

                if ( cliffSensed ) {
                    _turning = true;
                    _concaveTurn = false;
                    _worldTurningJoint = _body.GetWorldPoint(new Vector2(0, Height / 2));
                }
            }
        }

        private void HandleTurn(GameTime gameTime) {
            _body.LinearVelocity = Vector2.Zero;
            float rotationDelta =
                (float) (Projectile.PiOverTwo * gameTime.ElapsedGameTime.TotalMilliseconds / turnTimeMs);

            // We turn the opposite direction if we aren't going clockwise, 
            // or if this is a concave turn
            if ( !_clockwise ) {
                rotationDelta *= -1;
            }
            if ( _concaveTurn ) {
                rotationDelta *= -1;
            }

            _body.Rotation += rotationDelta;
            //Console.WriteLine("Rotation: {0}, maxrot: {1}", _body.Rotation, maxRotation);

            if (_concaveTurn) {
                Vector2 revolutionPoint = Vector2.Zero;
                if (_clockwise) {
                    revolutionPoint = _body.GetWorldPoint(new Vector2(Width / 2, Height / 2));
                } else {
                    revolutionPoint = _body.GetWorldPoint(new Vector2(-Width / 2, Height / 2));
                }
                _body.Position = new Vector2(_body.Position.X + _worldTurningJoint.X - revolutionPoint.X, _body.Position.Y);
            } else {
                Vector2 revolutionPoint = _body.GetWorldPoint(new Vector2(0, Height / 2));
                _body.Position += _worldTurningJoint - revolutionPoint;
            }

            float currRotation = NormalizeAngle(_body.Rotation);

            if ( _clockwise ) {
                if ( _concaveTurn ) {
                    switch ( _direction ) {
                        case Direction.Left:
                            if ( currRotation <= Projectile.PiOverTwo ) {
                                EndRotation(Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Right:
                            if ( currRotation <= -Projectile.PiOverTwo ) {
                                EndRotation(-Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Up:
                            if ( currRotation < Math.PI ) {
                                EndRotation((float) Math.PI);
                            }
                            break;
                        case Direction.Down:
                            if ( currRotation <= 0) {
                                EndRotation(0);
                            }
                            break;
                    }
                } else {
                    switch ( _direction ) {
                        case Direction.Left:
                            if ( currRotation >= 3 * Projectile.PiOverTwo ) {
                                EndRotation(3 * Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Right:
                            if ( currRotation >= Projectile.PiOverTwo ) {
                                EndRotation(Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Up:
                            // we'll wrap around to 0
                            if ( currRotation < Projectile.PiOverEight ) {
                                EndRotation(0f);
                            }
                            break;
                        case Direction.Down:
                            if ( currRotation >= Math.PI ) {
                                EndRotation((float) Math.PI);
                            }
                            break;
                    }
                }
            } else {
                if ( _concaveTurn ) {
                    switch ( _direction ) {
                        case Direction.Right:
                            if ( currRotation > -Projectile.PiOverTwo ) {
                                EndRotation(-Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Left:
                            if ( currRotation > Projectile.PiOverTwo ) {
                                EndRotation(Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Up:
                            if ( currRotation > -Projectile.PiOverTwo ) {
                                EndRotation(-Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Down:
                            if ( currRotation >= 0 ) {
                                EndRotation(0);
                            }
                            break;
                    }
                } else {
                    switch ( _direction ) {
                        case Direction.Right:
                            if ( currRotation < -3 * Projectile.PiOverTwo ) {
                                EndRotation(-3 * Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Left:
                            if ( currRotation < -Projectile.PiOverTwo ) {
                                EndRotation(-Projectile.PiOverTwo);
                            }
                            break;
                        case Direction.Up:
                            // we'll wrap around to 0
                            if ( currRotation > -Projectile.PiOverEight ) {
                                EndRotation(0f);
                            }
                            break;
                        case Direction.Down:
                            if ( currRotation < -Math.PI ) {
                                EndRotation(-(float) Math.PI);
                            }
                            break;
                    }
                }
            }
        }

        private void EndRotation(float rotation) {
            _body.Rotation = rotation;
            _turning = false;
            _ignoreTurnsMs = 200;
            SetNextDirection();
        }

        private float NormalizeAngle(float angle) {
            if ( angle >= 0 ) {
                while ( angle >= 2 * Math.PI ) {
                    angle -= 2 * (float) Math.PI;
                }
            } else {
                while ( angle <= -2 * Math.PI ) {
                    angle += 2 * (float) Math.PI;
                }                
            }
            return angle;
        }

        // Sets the new direction after completing a turn
        private void SetNextDirection() {
            if ( _concaveTurn ) {
                switch ( _direction ) {
                    case Direction.Left:
                        _direction = _clockwise ? Direction.Down : Direction.Up;
                        break;
                    case Direction.Right:
                        _direction = _clockwise ? Direction.Up : Direction.Down;
                        break;
                    case Direction.Up:
                        _direction = _clockwise ? Direction.Left : Direction.Right;
                        break;
                    case Direction.Down:
                        _direction = _clockwise ? Direction.Right : Direction.Left;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }
            } else {
                switch ( _direction ) {
                    case Direction.Left:
                        _direction = _clockwise ? Direction.Up : Direction.Down;
                        break;
                    case Direction.Right:
                        _direction = _clockwise ? Direction.Down : Direction.Up;
                        break;
                    case Direction.Up:
                        _direction = _clockwise ? Direction.Right : Direction.Left;
                        break;
                    case Direction.Down:
                        _direction = _clockwise ? Direction.Left : Direction.Right;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }                
            }
        }

        private void UpdateAnimation(GameTime gameTime) {
            _timeSinceLastUpdate += (int) gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_timeSinceLastUpdate > 100) {
                _animationFrame = (_animationFrame + 1) % NumFrames;
                Image = Animation[_animationFrame];
            }
        }
    }
}

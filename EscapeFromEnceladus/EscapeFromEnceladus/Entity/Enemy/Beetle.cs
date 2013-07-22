using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity.Enemy {

    public class Beetle : AbstractWalkingEnemy {

        private Texture2D _image;
        private int _animationFrame = 0;
        private int _timeSinceLastUpdate;

        private const float Height = .5f;
        private const float Width = 1;
        private const float Radius = Width / 2;
        private static readonly float LegHeight = ConvertUnits.ToSimUnits(4f);

        private const float TurnTimeMs = 250;
        private const float RotationDeltaPerMs = Projectile.PiOverTwo / TurnTimeMs;

        private bool _clockwise = false;
        private bool _concaveTurn = false;
        private float _targetAngle;
        private Vector2 _worldTurningJoint;

        private const int IgnoreTurnTimeout = 50;
        private int _ignoreTurnsMs = IgnoreTurnTimeout;

        private enum Mode {
            Walking,
            Turning,
            Falling,
        }

        private Mode _mode = Mode.Walking;
        
        protected override Texture2D Image {
            get { return _image; }
            set {
                _timeSinceLastUpdate = 0;
                _image = value;
            }
        }

        private const int NumFrames = 5;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
        private int LinearVelocity = 2;

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Enemy/Beetle/Beetle{0:0000}", i));
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
            _ignoreTurnsMs = IgnoreTurnTimeout;
        }

        protected override void CreateBody(Vector2 position, World world, float width, float height) {
            _body = BodyFactory.CreateSolidArc(world, 1f, (float) Math.PI, 12, Radius, new Vector2(0, -LegHeight), (float) Math.PI);
            FixtureFactory.AttachRectangle(Width - .05f, LegHeight, 1f, new Vector2(0, -LegHeight / 2), _body);
        }

        protected override void ConfigureBody(Vector2 position, float height) {
            base.ConfigureBody(position, height);
            _body.IgnoreGravity = true;
            _body.Friction = .5f;
            _body.Position = position;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            _ignoreTurnsMs -= (int) gameTime.ElapsedGameTime.TotalMilliseconds;

            switch ( _mode ) {
                case Mode.Walking:
                    HandleWalk();
                    break;
                case Mode.Turning:
                    HandleTurn(gameTime);
                    break;
                case Mode.Falling:
                    HandleRecovery(gameTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateAnimation(gameTime);
        }

        private void HandleRecovery(GameTime gameTime) {
            if ( _body.LinearVelocity.Length() < .001 ) {
                ContactEdge contactEdge = _body.ContactList;
                while ( contactEdge != null ) {
                    if ( contactEdge.Contact.GetPlayerNormal(_body).Y < -.8f ) {
                        if ( _body.Rotation > 2 * Math.PI - .01 || _body.Rotation < .01 ) {
                            _mode = Mode.Walking;
                            _direction = _clockwise ? Direction.Right : Direction.Left;
                            _body.IgnoreGravity = true;
                            _body.FixedRotation = true;
                        }
                    }
                    contactEdge = contactEdge.Next;
                }
            }
        }

        private void UpdateAnimation(GameTime gameTime) {
            _timeSinceLastUpdate += (int) gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_timeSinceLastUpdate > 20) {
                _animationFrame = (_animationFrame + 1) % NumFrames;
                Image = Animation[_animationFrame];
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( !Disposed ) {
                // draw position is character's feet
                Vector2 position = _body.Position;

                //Vector2 origin = new Vector2();
                Vector2 origin = new Vector2(Image.Width / 2f, Image.Height);

                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
                Color color = _flashAnimation.IsActive ? _flashAnimation.FlashColor : SolidColorEffect.DisabledColor;

                spriteBatch.Draw(Image, displayPosition, null, color, _body.Rotation, origin, 1f,
                                 _clockwise ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
            }
        }

        /// <summary>
        /// Event handler for when the beetle runs into a solid object.  We only care about the player interaction
        /// </summary>
        protected override void HitSolidObject(Contact contact, Fixture b) {
            if ( b.GetUserData().IsPlayer ) {
                bool reverseDirection = false;
                switch ( _direction ) {
                    case Direction.Left:
                        if ( Player.Instance.Position.X < _body.Position.X ) {
                            reverseDirection = true;
                            _direction = Direction.Right;
                        }
                        break;
                    case Direction.Right:
                        if ( Player.Instance.Position.X > _body.Position.X ) {
                            reverseDirection = true;
                            _direction = Direction.Left;
                        }
                        break;
                    case Direction.Up:
                        if ( Player.Instance.Position.Y < _body.Position.Y ) {
                            reverseDirection = true;
                            _direction = Direction.Down;
                        }
                        break;
                    case Direction.Down:
                        if ( Player.Instance.Position.Y > _body.Position.Y ) {
                            reverseDirection = true;
                            _direction = Direction.Up;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (reverseDirection) {
                    _clockwise = !_clockwise;
                    if (_mode == Mode.Turning) {
                        _targetAngle =
                            NormalizeAngle(_targetAngle + (_clockwise ? Projectile.PiOverTwo : -Projectile.PiOverTwo));
                    }
                }
            }
        }

        /// <summary>
        /// Begins a concave turn
        /// </summary>
        private void InitiateConcaveTurn() {
            _mode = Mode.Turning;
            _concaveTurn = true;
            if ( _clockwise ) {
                _worldTurningJoint = _body.GetWorldPoint(new Vector2(Radius, 0));
            } else {
                _worldTurningJoint = _body.GetWorldPoint(new Vector2(-Radius, 0));
            }
            _targetAngle =
                NormalizeAngle(_clockwise
                                   ? _body.Rotation - Projectile.PiOverTwo
                                   : _body.Rotation + Projectile.PiOverTwo);
        }

        /// <summary>
        /// Begins a convex turn
        /// </summary>
        private void InitiateConvexTurn() {
            _mode = Mode.Turning;
            _concaveTurn = false;
            _worldTurningJoint = _body.GetWorldPoint(new Vector2(0, 0));
            _targetAngle =
                NormalizeAngle(_clockwise
                                   ? _body.Rotation + Projectile.PiOverTwo
                                   : _body.Rotation - Projectile.PiOverTwo);
        }

        /// <summary>
        /// Handles the movement (not turning) mode of movement
        /// </summary>
        private void HandleWalk() {
            switch ( _direction ) {
                case Direction.Left:
                    _body.LinearVelocity = new Vector2(-LinearVelocity, 0);
                    break;
                case Direction.Right:
                    _body.LinearVelocity = new Vector2(LinearVelocity, 0);
                    break;
                case Direction.Up:
                    _body.LinearVelocity = new Vector2(0, -LinearVelocity);
                    break;
                case Direction.Down:
                    _body.LinearVelocity = new Vector2(0, LinearVelocity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Vector2 frontBumper = _body.GetWorldPoint(new Vector2(_clockwise ? Radius - .05f: -Radius + .05f, -.1f));
            Vector2 inFront = _body.GetWorldVector(new Vector2(_clockwise ? .075f : -.075f, 0));

            bool wallSensed = false;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor ) {
                    wallSensed = true;
                    return 0;
                }
                return -1;
            }, frontBumper, frontBumper + inFront);

            if ( wallSensed ) {
                InitiateConcaveTurn();
                return;
            }

            // We don't want to start a new convex turn right after this one
            if ( _ignoreTurnsMs <= 0 ) {
                Vector2 underneath = _body.GetWorldVector(new Vector2(0, Height / 2));

                var cliffSensed = CliffSensed(underneath);
                if ( cliffSensed ) {
                    // If there's nothing under our center, check to see if there's terrain anywhere.
                    // If not, we fall down.
                    var terrainSensed = TerrainUnderneath(underneath);

                    if ( terrainSensed ) {
                        InitiateConvexTurn();
                    } else {
                        Fall();
                    }
                }
            }
        }

        /// <summary>
        /// Returns whether there is a cliff directly underneath the body.
        /// </summary>
        private bool CliffSensed(Vector2 underneath) {
            Vector2 cliffSensor = _body.GetWorldPoint(new Vector2(0, -.1f));
            bool cliffSensed = true;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor && !fixture.IsSensor ) {
                    cliffSensed = false;
                    return 0;
                }
                return -1;
            }, cliffSensor, cliffSensor + underneath);
            return cliffSensed;
        }

        /// <summary>
        /// Returns whether there is terrain under the front or back edge of the body, to a specified depth.
        /// </summary>
        private bool TerrainUnderneath(Vector2 underneath) {
            var backEdge = _body.GetWorldPoint(new Vector2(-Width, -.1f));
            var frontEdge = _body.GetWorldPoint(new Vector2(Width, -.1f));

            bool terrainSensed = false;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor && !fixture.IsSensor ) {
                    terrainSensed = true;
                    return 0;
                }
                return -1;
            }, frontEdge, frontEdge + underneath);

            if ( !terrainSensed ) {
                _world.RayCast((fixture, point, normal, fraction) => {
                    if ( fixture.GetUserData().IsTerrain || fixture.GetUserData().IsDoor ) {
                        terrainSensed = true;
                        return 0;
                    }
                    return -1;
                }, backEdge, backEdge + underneath);
            }
            return terrainSensed;
        }

        private void Fall() {
            _mode = Mode.Falling;
            _body.IgnoreGravity = false;
            _body.FixedRotation = false;
        }

        /// <summary>
        /// Handles the turning (not moving) mode of movement
        /// </summary>
        private void HandleTurn(GameTime gameTime) {
            _body.LinearVelocity = Vector2.Zero;
            float rotationDelta = (float) (RotationDeltaPerMs * gameTime.ElapsedGameTime.TotalMilliseconds);

            float currRotation = NormalizeAngle(_body.Rotation);
            bool rotatingClockwise = IsClockwiseRotation(currRotation, _targetAngle);
            if ( _concaveTurn ) {
                rotationDelta *= 2f / 3f;
            }

            // Make sure we don't overshoot our adjustment
            if ( Math.Abs(currRotation - _targetAngle) < rotationDelta ) {
                _body.Rotation = _targetAngle;
            } else {
                _body.Rotation += rotatingClockwise ? rotationDelta : -rotationDelta;
            }
            //Console.WriteLine("Rotation: {0}, maxrot: {1}", _body.Rotation, maxRotation);

            Vector2 underneath = _body.GetWorldVector(new Vector2(0, Height * 3f / 2f));
            if ( CliffSensed(underneath) && !TerrainUnderneath(underneath) ) {
                Fall();
                return;
            }

            // Adjust our position to keep a certain point on the ground
            AdjustTurningPosition();

            if ( Math.Abs(NormalizeAngle(_body.Rotation) - _targetAngle) <= rotationDelta / 2 ) {
                EndRotation(_targetAngle);
            }
        }

        /// <summary>
        /// Moves the body to keep certain points in contact with the ground, 
        /// depending on whether it's a concave or convex turn
        /// </summary>
        private void AdjustTurningPosition() {
            if ( _concaveTurn ) {
                Vector2 revolutionPoint = Vector2.Zero;
                if ( _clockwise ) {
                    revolutionPoint = _body.GetWorldPoint(new Vector2(Radius, 0));
                } else {
                    revolutionPoint = _body.GetWorldPoint(new Vector2(-Radius, 0));
                }
                switch ( _direction ) {
                    case Direction.Left:
                    case Direction.Right:
                        _body.Position = new Vector2(_body.Position.X + _worldTurningJoint.X - revolutionPoint.X,
                                                     _body.Position.Y);
                        break;
                    case Direction.Up:
                    case Direction.Down:
                        _body.Position = new Vector2(_body.Position.X,
                                                     _body.Position.Y + _worldTurningJoint.Y - revolutionPoint.Y);
                        break;
                }
            } else {
                Vector2 revolutionPoint = _body.GetWorldPoint(new Vector2(0, 0));
                _body.Position += _worldTurningJoint - revolutionPoint;
            }
        }

        /// <summary>
        /// Returns whether a rotation between the two angles given is in the clockwise direction.
        /// Angles increase in the clockwise direction.
        /// Only works for targets in units of PI/2.
        /// </summary>
        private bool IsClockwiseRotation(float origin, float target) {
            if (target < .01f || target > 2 * Math.PI - .01f) {
                return origin >= Math.PI;
            } else if (target <= Projectile.PiOverTwo + .01f) {
                return origin <= Projectile.PiOverTwo || origin >= 3 * Projectile.PiOverTwo;
            } else if (target <= Math.PI) {
                return origin <= Math.PI;
            } else if (target <= 3 * Projectile.PiOverTwo + .01f) {
                return origin <= 3 * Projectile.PiOverTwo && origin >= Projectile.PiOverTwo;
            } else {
                // Better buggy than nothing
                return true;
                //throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Ends the rotation mode, setting the new rotation to be the angle given
        /// </summary>
        private void EndRotation(float rotation) {
            _body.Rotation = rotation;
            AdjustTurningPosition();
            _mode = Mode.Walking;
            _ignoreTurnsMs = IgnoreTurnTimeout;
            SetNextDirection();
        }

        /// <summary>
        /// Returns an angle in the range [0..2PI]
        /// </summary>
        private float NormalizeAngle(float angle) {
            if ( angle >= 0 ) {
                while ( angle >= 2 * Math.PI ) {
                    angle -= 2 * (float) Math.PI;
                }
            } else {
                while ( angle < 0 ) {
                    angle += 2 * (float) Math.PI;
                }                
            }
            return angle;
        }

        // Sets the new direction after completing a turn
        private void SetNextDirection() {
            float rotation = NormalizeAngle(_body.Rotation);
            if ( rotation < Projectile.PiOverEight || rotation > Math.PI * 2 - Projectile.PiOverEight ) {
                _direction = _clockwise ? Direction.Right : Direction.Left;
            } else if ( rotation < Projectile.PiOverTwo + .05f ) {
                _direction = _clockwise ? Direction.Down : Direction.Up;
            } else if ( rotation < Math.PI + .05f ) {
                _direction = _clockwise ? Direction.Left : Direction.Right;
            } else {
                _direction = _clockwise ? Direction.Up : Direction.Down;
            }
        }

        protected override Vector2 GetStandingLocation() {
            return _body.GetWorldPoint(new Vector2(0, LegHeight));
        }
    }
}

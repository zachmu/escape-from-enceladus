/*
 * Stolen from Farseer Physics Samples.
 */

using System;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {
    public class Camera2D {
        private const float MinZoom = 0.02f;
        private const float MaxZoom = 20f;
        private const string RoomTransitionSpeed = "Room Transition speed";
        private static GraphicsDevice _graphics;

        private Vector2 _currentPosition;
        private float _currentRotation;
        private float _currentZoom;

        private Vector2 _minPosition;
        private Vector2 _maxPosition;
        private Room _currentRoom;
        private bool _isConstrainedToRoom;
        private float _minRotation;
        private float _maxRotation;

        private bool _positionTracking;
        private bool _rotationTracking;
        private Body _trackingBody;

        private Vector2 _targetPosition;
        private float _targetRotation;
        private readonly Vector2 _translateCenter;

        public Matrix DisplayView { get; private set; }
        public Matrix SimView { get; private set; }
        public Matrix SimProjection { get; private set; }

        static Camera2D() {
            Constants.Register(new Constant(RoomTransitionSpeed, 200f, Keys.R));
        }

        /// <summary>
        /// The constructor for the Camera2D class.
        /// </summary>
        /// <param name="graphics"></param>
        public Camera2D(GraphicsDevice graphics) {
            _graphics = graphics;
            SimProjection = Matrix.CreateOrthographicOffCenter(0f, ConvertUnits.ToSimUnits(_graphics.Viewport.Width),
                                                               ConvertUnits.ToSimUnits(_graphics.Viewport.Height), 0f,
                                                               0f,
                                                               1f);
            SimView = Matrix.Identity;
            DisplayView = Matrix.Identity;

            _translateCenter = new Vector2(ConvertUnits.ToSimUnits(_graphics.Viewport.Width / 2f),
                                           ConvertUnits.ToSimUnits(_graphics.Viewport.Height / 2f));

            ResetCamera();
        }

        /// <summary>
        /// The current position of the camera.
        /// </summary>
        public Vector2 Position {
            get { return _currentPosition; }
            set {
                _targetPosition = value;
                if ( IsConstrainPosition ) {
                    Vector2.Clamp(ref _targetPosition, ref _minPosition, ref _maxPosition, out _targetPosition);
                }
            }
        }

        /// <summary>
        /// Returns whether the camera is at its target destination.
        /// </summary>
        /// <returns></returns>
        public bool IsAtTarget() {
            return (_targetPosition - _currentPosition).Length() < .1;
        }

        /// <summary>
        /// Constrain the camera to the rectangular region given.
        /// </summary>
        public void ConstrainToRegion(Vector2 minPosition, Vector2 maxPosition) {
            _minPosition = minPosition;
            _maxPosition = maxPosition;
            IsConstrainPosition = true;
            _isConstrainedToRoom = false;
        }

        public void ConstrainToRoom(Room room) {
            _currentRoom = room;
            IsConstrainPosition = true;
            _isConstrainedToRoom = true;
        }

        /// <summary>
        /// Whether or not to constrain the position of the camera 
        /// to a region or room
        /// </summary>
        public bool IsConstrainPosition { get; set; }  

        /// <summary>
        /// Immediately moves the camera from its current position in the amount specified, 
        /// updating the target position there as well.
        /// </summary>
        public void MoveCamera(Vector2 amount) {
            _currentPosition += amount;
            if ( IsConstrainPosition ) {
                if ( _isConstrainedToRoom ) {
                    _currentRoom.ClosestAreaOfConstraint(_targetPosition, out _minPosition, out _maxPosition);
                }
                Vector2.Clamp(ref _currentPosition, ref _minPosition, ref _maxPosition, out _currentPosition);
            }
            _targetPosition = _currentPosition;
            _positionTracking = false;
            _rotationTracking = false;
        }

        /// <summary>
        /// Sets the target position to be the current position plus a specified delta
        /// </summary>
        public void MoveTarget(Vector2 delta) {
            _targetPosition = _currentPosition + delta;
            if ( IsConstrainPosition ) {
                if ( _isConstrainedToRoom ) {
                    _currentRoom.ClosestAreaOfConstraint(_targetPosition, out _minPosition, out _maxPosition);
                }
                Vector2.Clamp(ref _targetPosition, ref _minPosition, ref _maxPosition, out _targetPosition);
            }
        }

        /// <summary>
        /// Resets the camera to default values.
        /// </summary>
        public void ResetCamera() {
            _currentPosition = Vector2.Zero;
            _targetPosition = Vector2.Zero;
            _minPosition = Vector2.Zero;
            _maxPosition = Vector2.Zero;
            IsConstrainPosition = false;

            _currentRotation = 0f;
            _targetRotation = 0f;
            _minRotation = -MathHelper.Pi;
            _maxRotation = MathHelper.Pi;

            _positionTracking = false;
            _rotationTracking = false;

            _currentZoom = 1f;

            SetView();
        }

        public void Jump2Target() {
            _currentPosition = _targetPosition;
            _currentRotation = _targetRotation;

            SetView();
        }

        private void SetView() {
            Matrix matRotation = Matrix.CreateRotationZ(_currentRotation);
            Matrix matZoom = Matrix.CreateScale(_currentZoom);
            Vector3 translateCenter = new Vector3(_translateCenter, 0f);
            Vector3 translateBody = new Vector3(-_currentPosition, 0f);

            SimView = Matrix.CreateTranslation(translateBody) *
                    matRotation *
                    matZoom *
                    Matrix.CreateTranslation(translateCenter);

            translateCenter = ConvertUnits.ToDisplayUnits(translateCenter);
            translateBody = ConvertUnits.ToDisplayUnits(translateBody);

            DisplayView = Matrix.CreateTranslation(translateBody) *
                         matRotation *
                         matZoom *
                         Matrix.CreateTranslation(translateCenter);
        }

        /// <summary>
        /// Moves the camera forward one timestep.
        /// </summary>
        public void Update(GameTime gameTime) {
            if ( _trackingBody != null ) {
                if ( _positionTracking ) {
                    _targetPosition = _trackingBody.Position;
                    if ( IsConstrainPosition) {
                        Vector2.Clamp(ref _targetPosition, ref _minPosition, ref _maxPosition, out _targetPosition);
                    }
                }
                if ( _rotationTracking ) {
                    _targetRotation = -_trackingBody.Rotation % MathHelper.TwoPi;
                    if ( _minRotation != _maxRotation ) {
                        _targetRotation = MathHelper.Clamp(_targetRotation, _minRotation, _maxRotation);
                    }
                }
            }

            // Move toward our target position
            Vector2 delta = _targetPosition - _currentPosition;
            float distance = delta.Length();
            if ( distance > 0f ) {
                delta /= distance;

                float inertia;
                if ( distance < 10f ) {
                    inertia = (float) Math.Pow(distance / 10.0, 2.0);
                } else {
                    inertia = 1f;
                }

                Vector2 adjustmentSpeed = 100f * delta * inertia;

                // clamp the movement to a max and min speed
                float minSpeed = 4f;
                float maxSpeed = 30f;
                if ( adjustmentSpeed.Length() > maxSpeed ) {
                    adjustmentSpeed.Normalize();
                    adjustmentSpeed *= maxSpeed;
                } else if ( adjustmentSpeed.Length() < minSpeed ) {
                    adjustmentSpeed.Normalize();
                    adjustmentSpeed *= minSpeed;
                }

                // If the adjustment distance is under a min threshold, 
                // or we're about to overshoot, we're done
                float minAdjustment = .001f;
                Vector2 adjustment = adjustmentSpeed * (float) gameTime.ElapsedGameTime.TotalSeconds;
                if ( adjustment.Length() < minAdjustment || adjustment.Length() > distance ) {
                    _currentPosition = _targetPosition;
                } else {
                    if ( IsConstrainPosition ) {
                        _currentPosition += adjustment;
                        //Vector2.Clamp(ref _currentPosition, ref _minPosition, ref _maxPosition, out _currentPosition);
                    } else {
                        _currentPosition += adjustment;
                    }
                }
            }

            SetView();
        }

        public Vector2 ConvertScreenToWorld(Vector2 location) {
            Vector3 t = new Vector3(location, 0);

            t = _graphics.Viewport.Unproject(t, SimProjection, SimView, Matrix.Identity);

            return new Vector2(t.X, t.Y);
        }

        public Vector2 ConvertWorldToScreen(Vector2 location) {
            Vector3 t = new Vector3(location, 0);

            t = _graphics.Viewport.Project(t, SimProjection, SimView, Matrix.Identity);

            return new Vector2(t.X, t.Y);
        }

        public void DebugDraw(SpriteBatch batch, Texture2D debugMarker) {
            batch.Draw(debugMarker, ConvertUnits.ToDisplayUnits(_targetPosition), null, Color.White, 0, new Vector2(debugMarker.Width / 2f, debugMarker.Height / 2f), 1f, SpriteEffects.None, 1f);
        }

        #region unused

        /// <summary>
        /// Gets or sets the minimum rotation in radians.
        /// </summary>
        /// <value>The min rotation.</value>
        public float MinRotation {
            get { return _minRotation; }
            set { _minRotation = MathHelper.Clamp(value, -MathHelper.Pi, 0f); }
        }

        /// <summary>
        /// Gets or sets the maximum rotation in radians.
        /// </summary>
        /// <value>The max rotation.</value>
        public float MaxRotation {
            get { return _maxRotation; }
            set { _maxRotation = MathHelper.Clamp(value, 0f, MathHelper.Pi); }
        }

        /// <summary>
        /// The current rotation of the camera in radians.
        /// </summary>
        public float Zoom {
            get { return _currentZoom; }
            set {
                _currentZoom = value;
                _currentZoom = MathHelper.Clamp(_currentZoom, MinZoom, MaxZoom);
            }
        }

        /// <summary>
        /// the body that this camera is currently tracking. 
        /// Null if not tracking any.
        /// </summary>
        public Body TrackingBody {
            get { return _trackingBody; }
            set {
                _trackingBody = value;
                if ( _trackingBody != null ) {
                    _positionTracking = true;
                }
            }
        }

        public void RotateCamera(float amount) {
            _currentRotation += amount;
            if ( _minRotation != _maxRotation ) {
                _currentRotation = MathHelper.Clamp(_currentRotation, _minRotation, _maxRotation);
            }
            _positionTracking = false;
            _rotationTracking = false;
        }

        /// <summary>
        /// The current rotation of the camera in radians.
        /// </summary>
        public float Rotation {
            get { return _currentRotation; }
            set {
                _targetRotation = value % MathHelper.TwoPi;
                if ( _minRotation != _maxRotation ) {
                    _targetRotation = MathHelper.Clamp(_targetRotation, _minRotation, _maxRotation);
                }
            }
        }

        public bool EnablePositionTracking {
            get { return _positionTracking; }
            set {
                if ( value && _trackingBody != null ) {
                    _positionTracking = true;
                } else {
                    _positionTracking = false;
                }
            }
        }

        public bool EnableRotationTracking {
            get { return _rotationTracking; }
            set {
                if ( value && _trackingBody != null ) {
                    _rotationTracking = true;
                } else {
                    _rotationTracking = false;
                }
            }
        }

        public bool EnableTracking {
            set {
                EnablePositionTracking = value;
                EnableRotationTracking = value;
            }
        }


        #endregion
    }
}
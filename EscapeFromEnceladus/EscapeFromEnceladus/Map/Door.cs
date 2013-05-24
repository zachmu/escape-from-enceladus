using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Weapon;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Enceladus.Map {
    
    /// <summary>
    /// A door that can be opened by some weapon. Doors live the life of the simulation,
    /// but they only physically exist when the player is around. A door's life will be
    /// a series of call to Create() and Destroy().
    /// </summary>
    public class Door : Region, IGameEntity {
        public static Texture2D Image { get; private set; }

        private const string DoorOpenTime = "Door open time (s)";
        private const string DoorStayOpenTime = "Door stays open (s)";

        private readonly World _world;
        private readonly Orientation _orientation;
        private readonly string _name;
        private Body _body;
        private bool _locked;

        static Door() {
            Constants.Register(new Constant(DoorOpenTime, .2f, Keys.V));
            Constants.Register(new Constant(DoorStayOpenTime, 5f, Keys.F));
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("door");
        }

        public Door(World world, Vector2 topLeft, Vector2 bottomRight, Object mapObject)
            : base(AdjustToTileBoundary(topLeft), AdjustToTileBoundary(bottomRight)) {
            _world = world;
            if ( mapObject.Name != null ) {
                _name = mapObject.Name;
            }

            if ( Width > Height ) {
                _orientation = Orientation.Horizontal;
            } else {
                _orientation = Orientation.Vertical;
            }
        }

        /// <summary>
        /// The unique name of this door. Most doors don't have one.
        /// </summary>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// A disposed door doesn't physically exist, but its 
        /// metadata (like locked state) might still be relevant.
        /// </summary>
        public void Dispose() {
            _body.Dispose();
        }

        public bool Disposed {
            get { return _body == null || _body.IsDisposed; }
        }

        /// <summary>
        /// All doors can be locked, but only named ones can persist this information
        /// </summary>
        public void Lock() {
            _locked = true;
            DoorState.Instance.DoorLocked(Name);
        }

        /// <summary>
        /// All doors can be unlocked, but only named ones can persist this information
        /// </summary>
        public void Unlock() {
            _locked = false;
            DoorState.Instance.DoorUnlocked(Name);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if ( _orientation == Orientation.Vertical ) {
                DrawVertical(spriteBatch);
            } else {
                DrawHorizontal(spriteBatch);
            }
        }

        private void DrawVertical(SpriteBatch spriteBatch) {
            int fullDisplayHeight = (int) (TileLevel.TileDisplaySize * Height);
            Rectangle rect = new Rectangle(0, 0, TileLevel.TileDisplaySize, fullDisplayHeight);

            float offset = 0;
            switch ( _state ) {
                case State.Closed:
                    break;
                case State.Opening:
                    float percentOpen = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    offset = Height * percentOpen;
                    rect.Height = (int) (fullDisplayHeight * (1 - percentOpen));
                    break;
                case State.Open:
                    return;
                case State.Closing:
                    float percentClosed = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    offset = Height * (1 - percentClosed);
                    rect.Height = (int) (fullDisplayHeight * percentClosed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for ( int i = 0; i < Width / TileLevel.TileSize; i++ ) {
                Vector2 topLeft = TopLeft + new Vector2(TileLevel.TileSize * i, offset);
                Vector2 displayPos = ConvertUnits.ToDisplayUnits(topLeft);
                spriteBatch.Draw(Image, displayPos, rect, SolidColorEffect.DisabledColor);
            }
        }

        private void DrawHorizontal(SpriteBatch spriteBatch) {
            int fullDisplayWidth = (int) (TileLevel.TileDisplaySize * Width);
            Rectangle rect = new Rectangle(0, 0, TileLevel.TileDisplaySize, fullDisplayWidth);

            switch ( _state ) {
                case State.Closed:
                    break;
                case State.Opening:
                    float percentOpen = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    rect.Y = (int) (fullDisplayWidth * percentOpen);
                    rect.Height = (int) (fullDisplayWidth * (1 - percentOpen));
                    break;
                case State.Open:
                    return;
                case State.Closing:
                    float percentClosed = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    rect.Y = (int) (fullDisplayWidth * (1 - percentClosed));
                    rect.Height = (int) (fullDisplayWidth * percentClosed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for ( int i = 0; i < Height / TileLevel.TileSize; i++ ) {
                Vector2 topLeft = TopLeft;
                Vector2 displayPos = ConvertUnits.ToDisplayUnits(topLeft) +
                                     new Vector2(ConvertUnits.ToDisplayUnits(Width / 2),
                                                 TileLevel.TileDisplaySize * i + Image.Width / 2f);
                spriteBatch.Draw(Image, displayPos, rect, SolidColorEffect.DisabledColor, -(float) Math.PI / 2,
                                 new Vector2(Image.Width / 2, Image.Height / 2), 1.0f, SpriteEffects.None, 1f);
            }
        }

        private enum State {
            Closed,
            Opening,
            Open,
            Closing,
        }

        private enum Orientation {
            Vertical,
            Horizontal
        }

        private State _state = State.Closed;
        private int _msSinceLastStateChange;

        public void Update(GameTime gameTime) {
            _msSinceLastStateChange += gameTime.ElapsedGameTime.Milliseconds;
            switch ( _state ) {
                case State.Closed:
                    break;
                case State.Open:
                    if ( _msSinceLastStateChange >= Constants.Get(DoorStayOpenTime) * 1000 
                        && !EnceladusGame.EntitiesOverlapping(Aabb) ) {
                        CloseDoor();
                    }
                    break;
                case State.Opening:
                    if ( _msSinceLastStateChange >= Constants.Get(DoorOpenTime) * 1000 ) {
                        OpenDoorFully();
                    }
                    break;
                case State.Closing:
                    if ( _msSinceLastStateChange >= Constants.Get(DoorOpenTime) * 1000 ) {
                        CloseDoorFully();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        private void OpenDoorFully() {
            MakeDoorPassable();
            _state = State.Open;
            _msSinceLastStateChange = 0;
        }

        private void OpenDoor() {
            MakeDoorPassable();
            switch ( _state ) {
                case State.Closed:
                    _state = State.Opening;
                    _msSinceLastStateChange = 0;
                    break;
                case State.Closing:
                    _state = State.Opening;
                    _msSinceLastStateChange = (int) (Constants.Get(DoorOpenTime) * 1000 - _msSinceLastStateChange);
                    break;
            }
        }

        private void CloseDoor() {
            _state = State.Closing;
            _msSinceLastStateChange = 0;
        }

        private void CloseDoorFully() {
            MakeDoorSolid();
            _state = State.Closed;
            _msSinceLastStateChange = 0;
        }

        private void CreateBody() {
            _body = BodyFactory.CreateRectangle(_world, Width, Height, 0);
            _body.IsStatic = true;
            _body.Position = _topLeft + new Vector2(Width / 2, Height / 2);
            _body.UserData = UserData.NewDoor(this);
            _body.CollidesWith = Category.All;
            _body.CollisionCategories = EnceladusGame.TerrainCategory;
            _body.OnSeparation += (a, b) => {
                if ( b.GetUserData().IsPlayer ) {
                    if ( _state == State.Open ) {
                        CloseDoor();
                    }
                }
            };
        }

        private void MakeDoorPassable() {
            _body.IsSensor = true;
        }

        private void MakeDoorSolid() {
            _body.IsSensor = false;
        }

        public void HitBy(Projectile shot) {
            if ( !_locked ) {
                OpenDoor();
            }
        }

        /// <summary>
        /// Called to instantiate this door in the game world as an interactive object.
        /// Dispose gets rid of it.
        /// </summary>
        public void Create() {
            CreateBody();
            CloseDoorFully();
        }
    }
}

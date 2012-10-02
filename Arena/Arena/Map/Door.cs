using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
using Arena.Weapon;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena.Map {
    
    /// <summary>
    /// A door that can be opened by some weapon.
    /// </summary>
    public class Door : Region, IGameEntity {
        public static Texture2D Image { get; private set; }

        private const string DoorOpenTime = "Door open time (s)";
        private const string DoorStayOpenTime = "Door stays open (s)";

        private Body _body;
        private World _world;
        private Orientation _orientation;

        static Door() {
            Constants.Register(new Constant(DoorOpenTime, .2f, Keys.V));
            Constants.Register(new Constant(DoorStayOpenTime, 5f, Keys.F));
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("door");
        }

        public Door(World world, Vector2 topLeft, Vector2 bottomRight)
            : base(AdjustToTileBoundary(topLeft), AdjustToTileBoundary(bottomRight)) {
            _world = world;

            if (Width > Height) {
                _orientation = Orientation.Horizontal;
            } else {
                _orientation = Orientation.Vertical;
            }

            CreateBody();
        }

        public void Dispose() {
            _body.Dispose();
        }

        public bool Disposed {
            get { return _body.IsDisposed; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            if (_orientation == Orientation.Vertical) {
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
                spriteBatch.Draw(Image, displayPos, rect, Color.White);
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
                spriteBatch.Draw(Image, displayPos, rect, Color.White, -(float) Math.PI / 2,
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
                        && !Arena.EntitiesOverlapping(Aabb) ) {
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
                        _state = State.Closed;
                        _msSinceLastStateChange = 0;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OpenDoorFully() {
            MakeDoorPassable();
            _state = State.Open;
            _msSinceLastStateChange = 0;
        }

        private void CloseDoor() {
            MakeDoorSolid();
            _state = State.Closing;
            _msSinceLastStateChange = 0;
        }

        private void CreateBody() {
            _body = BodyFactory.CreateRectangle(_world, Width, Height, 0);
            _body.IsStatic = true;
            _body.Position = _topLeft + new Vector2(Width / 2, Height / 2);
            _body.UserData = UserData.NewDoor(this);
            _body.CollidesWith = Category.All;
            _body.CollisionCategories = Arena.TerrainCategory;
            _body.OnSeparation += (a, b) => {
                if ( b.GetUserData().IsPlayer ) {
                    if ( _state == State.Open ) {
                        CloseDoor();
                        Console.WriteLine("Closing door");
                    }
                }
            };
        }

        private void MakeDoorPassable() {
            _body.IsSensor = true;
         //   _body.CollidesWith = Category.None;
        }

        private void MakeDoorSolid() {
            _body.IsSensor = false;
           // _body.CollidesWith = Category.All;
        }

        public void HitBy(Projectile shot) {
            switch (_state) {
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
    }
}

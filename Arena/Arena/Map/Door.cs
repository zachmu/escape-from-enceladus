﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Entity;
using Arena.Farseer;
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

        static Door() {
            Constants.Register(new Constant(DoorOpenTime, .3f, Keys.V));
            Constants.Register(new Constant(DoorStayOpenTime, 2.5f, Keys.F));
        }

        public static void LoadContent(ContentManager content) {
            Image = content.Load<Texture2D>("door");
        }

        public Door(World world, Vector2 topLeft, Vector2 bottomRight) {
            _topLeft = AdjustToTileBoundary(topLeft);
            _bottomRight = AdjustToTileBoundary(bottomRight);
            _world = world;

            CreateBody();
        }

        public void Dispose() {
            _body.Dispose();
        }

        public bool Disposed {
            get { return _body.IsDisposed; }
        }

        public PolygonShape Shape {
            get { return new PolygonShape(PolygonTools.CreateRectangle(Width / 2, Height / 2), 0f); }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // TODO: assumes door height of 3
            int fullDisplayHeight = TileLevel.TileDisplaySize * 3;
            Rectangle rect = new Rectangle(0, 0, TileLevel.TileDisplaySize, fullDisplayHeight);

            float offset = 0;
            switch (_state) {
                case State.Closed:
                    break;
                case State.Opening:
                    float percentOpen = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    offset = 3f * percentOpen;
                    rect.Height = (int) (fullDisplayHeight * (1 - percentOpen));
                    break;
                case State.Open:
                    return;
                case State.Closing:
                    float percentClosed = _msSinceLastStateChange / Constants.Get(DoorOpenTime) / 1000;
                    offset = 3f * (1 - percentClosed);
                    rect.Height = (int) (fullDisplayHeight * percentClosed);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for ( int i = 0; i < Width / TileLevel.TileSize; i++ ) {
                Vector2 topLeft = TopLeft + new Vector2(TileLevel.TileSize * i, offset);
                Vector2 displayPos = new Vector2();
                ConvertUnits.ToDisplayUnits(ref topLeft, out displayPos);
                spriteBatch.Draw(Image, displayPos, rect, Color.White);
            }
        }

        private enum State {
            Closed,
            Opening,
            Open,
            Closing,
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
                        && !Arena.EntitiesOverlapping(Shape, Position) ) {
                        CreateBody();
                        _state = State.Closing;
                        _msSinceLastStateChange = 0;
                    }
                    break;
                case State.Opening:
                    if ( _msSinceLastStateChange >= Constants.Get(DoorOpenTime) * 1000 ) {
                        DestroyBody();
                        _state = State.Open;
                        _msSinceLastStateChange = 0;
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

        private void CreateBody() {
            _body = BodyFactory.CreateRectangle(_world, Width, Height, 0);
            _body.IsStatic = true;
            _body.Position = _topLeft + new Vector2(Width / 2, Height / 2);
            _body.UserData = UserData.NewDoor(this);
            _body.CollidesWith = Category.All;
            _body.CollisionCategories = Arena.TerrainCategory;
        }

        private void DestroyBody() {
            _body.Dispose();
        }

        public void HitBy(Shot shot) {
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
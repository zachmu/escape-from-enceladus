using System;
using System.Collections.Generic;
using Arena.Entity;
using Arena.Farseer;
using Arena.Map;
using FarseerPhysics;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Path = System.IO.Path;

namespace Arena {

    enum Mode {
        NormalControl,
        RoomTransition,
    }

    public class Arena : Game {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch _spriteBatch;
        private TileLevel _tileLevel;
        private Camera2D _camera;
        private World _world;
        private DebugViewXNA _debugView;
        private Player _player;
        private Mode _mode;

        private readonly List<IGameEntity> _entities = new List<IGameEntity>();

        private bool _manualCamera = false;

        private static Arena _instance;

        public static Arena Instance {
            get { return _instance; }
        }

        private const string Gravity = "World gravity (m/s/s)";

        public const Category PlayerCategory = Category.Cat1;
        public const Category TerrainCategory = Category.Cat2;
        public const Category PlayerProjectileCategory = Category.Cat3;
        public const Category EnemyCategory = Category.Cat4;

        public static Boolean Debug = true;
    
        public Arena() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //            graphics.PreferMultiSampling = true;
#if WINDOWS || XBOX
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(64f);
            IsFixedTimeStep = true;
#elif WINDOWS_PHONE
            ConvertUnits.SetDisplayUnitToSimUnitRatio(16f);
            IsFixedTimeStep = false;
#endif
#if WINDOWS
            graphics.IsFullScreen = false;
#elif XBOX || WINDOWS_PHONE
            graphics.IsFullScreen = true;
#endif
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            _instance = this;

            Constants.Register(new Constant(Gravity, 25f, Keys.G));

            _world = new World(new Vector2(0, Constants.Get(Gravity)));
            _camera = new Camera2D(graphics.GraphicsDevice);
            _player = new Player(new Vector2(5,5), _world);

            _mode = Mode.NormalControl;

            base.Initialize();
        }

        /// <summary>
        /// Registers an entity to receive update and draw calls.
        /// Entity will be removed when it is disposed.
        /// </summary>
        public void Register(params IGameEntity[] entity) {
            _entities.AddRange(entity);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _tileLevel = new TileLevel(Content, Path.Combine(Content.RootDirectory, "Maps", "test.tmx"), _world,
                                       _player.Position);
            _player.LoadContent(Content);

            Enemy.LoadContent(Content);
            Shot.LoadContent(Content);
            Constants.font = Content.Load<SpriteFont>("DebugFont");

            if ( _debugView == null ) {
                _debugView = new DebugViewXNA(_world);
                _debugView.RemoveFlags(DebugViewFlags.Shape);
                _debugView.RemoveFlags(DebugViewFlags.Joint);
                _debugView.DefaultShapeColor = Color.White;
                _debugView.SleepingShapeColor = Color.LightGray;
                _debugView.LoadContent(GraphicsDevice, Content);
            }

            ClampCameraToRoom();
        }

        /// <summary>
        /// Constrains the camera position to the current room in the level.
        /// </summary>
        private void ClampCameraToRoom() {
            Vector2 viewportCenter = ConvertUnits.ToSimUnits(GraphicsDevice.Viewport.Width / 2f,
                                                             GraphicsDevice.Viewport.Height / 2f);
            _camera.MinPosition = TileLevel.CurrentRoom.TopLeft + viewportCenter;
            _camera.MaxPosition = TileLevel.CurrentRoom.BottomRight - viewportCenter;
        }

        private void UnclampCamera() {
            _camera.MinPosition = _camera.MaxPosition;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        private Vector2 _spriteSpeed = new Vector2(1.0f);

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            
            KeyboardState keyboardState = Keyboard.GetState();
            if ( GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape) )
                this.Exit();

            InputHelper helper = InputHelper.Instance;
            helper.Update(gameTime);

            if ( _mode == Mode.NormalControl ) {
                foreach ( var pressedKey in helper.GetNewKeyPresses() ) {
                    switch ( pressedKey ) {
                        case Keys.F1:
                            EnableOrDisableFlag(DebugViewFlags.Shape);
                            break;
                        case Keys.F2:
                            EnableOrDisableFlag(DebugViewFlags.DebugPanel);
                            EnableOrDisableFlag(DebugViewFlags.PerformanceGraph);
                            break;
                        case Keys.F3:
                            EnableOrDisableFlag(DebugViewFlags.Joint);
                            break;
                        case Keys.F4:
                            EnableOrDisableFlag(DebugViewFlags.ContactPoints);
                            EnableOrDisableFlag(DebugViewFlags.ContactNormals);
                            break;
                        case Keys.F5:
                            EnableOrDisableFlag(DebugViewFlags.PolygonPoints);
                            break;
                        case Keys.F6:
                            EnableOrDisableFlag(DebugViewFlags.Controllers);
                            break;
                        case Keys.F7:
                            EnableOrDisableFlag(DebugViewFlags.CenterOfMass);
                            break;
                        case Keys.F8:
                            EnableOrDisableFlag(DebugViewFlags.AABB);
                            break;
                    }
                }
                foreach ( var pressedKey in helper.KeyboardState.GetPressedKeys() ) {
                    switch ( pressedKey ) {
                        case Keys.Up:
                            _manualCamera = true;
                            _camera.MoveCamera(new Vector2(0, -1));
                            break;
                        case Keys.Down:
                            _manualCamera = true;
                            _camera.MoveCamera(new Vector2(0, 1));
                            break;
                        case Keys.Left:
                            _manualCamera = true;
                            _camera.MoveCamera(new Vector2(-1, 0));
                            break;
                        case Keys.Right:
                            _manualCamera = true;
                            _camera.MoveCamera(new Vector2(1, 0));
                            break;
                        case Keys.LeftAlt:
                            _manualCamera = false;
                            break;
                    }
                }

                _tileLevel.Update(gameTime);

                _world.Gravity = new Vector2(0, Constants.Get(Gravity));

                _world.Step(Math.Min((float) gameTime.ElapsedGameTime.TotalSeconds, (1f / 30f)));

                _player.Update(gameTime);

                foreach ( IGameEntity ent in _entities ) {
                    ent.Update(gameTime);
                }

                Constants.Update(helper);

                TrackPlayer();
            } 

            _camera.Update(gameTime);

            if ( _mode == Mode.RoomTransition ) {
                if (_camera.IsAtTarget()) {
                    _mode = Mode.NormalControl;
                    ClampCameraToRoom();
                }
            }

            _entities.RemoveAll(entity => entity.Disposed);
            base.Update(gameTime);
        }

        /// <summary>
        /// Moves the camera to follow the player around the screen.
        /// </summary>
        private void TrackPlayer() {
            if ( !_manualCamera ) {
                // If the sprite goes outside a margin area, move the camera
                const int margin = 250;
                Rectangle viewportMargin = new Rectangle(graphics.GraphicsDevice.Viewport.X + margin,
                                                         graphics.GraphicsDevice.Viewport.Y + margin,
                                                         graphics.GraphicsDevice.Viewport.Width - 2 * margin,
                                                         graphics.GraphicsDevice.Viewport.Height - 2 * margin);
                Vector2 spriteScreenPosition = _camera.ConvertWorldToScreen(_player.Position);

                int maxx =
                    viewportMargin.Right - Player.ImageWidth;
                int minx = viewportMargin.Left;
                int maxy = viewportMargin.Bottom - Player.ImageHeight;
                int miny = viewportMargin.Top;

                // Move the camera just enough to position the sprite at the edge of the margin
                if ( spriteScreenPosition.X > maxx ) {
                    float delta = spriteScreenPosition.X - maxx;
                    _camera.MoveCamera(ConvertUnits.ToSimUnits(delta, 0));
                } else if ( spriteScreenPosition.X < minx ) {
                    float delta = spriteScreenPosition.X - minx;
                    _camera.MoveCamera(ConvertUnits.ToSimUnits(delta, 0));
                }

                if ( spriteScreenPosition.Y > maxy ) {
                    float delta = spriteScreenPosition.Y - maxy;
                    _camera.MoveCamera(ConvertUnits.ToSimUnits(0, delta));
                } else if ( spriteScreenPosition.Y < miny ) {
                    float delta = spriteScreenPosition.Y - miny;
                    _camera.MoveCamera(ConvertUnits.ToSimUnits(0, delta));
                }
            }

            Room currentRoom = TileLevel.CurrentRoom;
            if ( !currentRoom.Contains(_player.Position) ) {
                Direction directionOfTravel = currentRoom.GetRelativeDirection(_player.Position);

                _mode = Mode.RoomTransition;
                UnclampCamera();

                _tileLevel.DetermineCurrentRoom(_player.Position);
                currentRoom = TileLevel.CurrentRoom;
                switch ( directionOfTravel ) {
                    case Direction.Left:
                        _camera.Position =
                            new Vector2(
                                currentRoom.BottomRight.X -
                                ConvertUnits.ToSimUnits(graphics.GraphicsDevice.Viewport.Width / 2f), _camera.Position.Y);

                        break;
                    case Direction.Right:
                        _camera.Position =
                            new Vector2(
                                currentRoom.TopLeft.X +
                                ConvertUnits.ToSimUnits(graphics.GraphicsDevice.Viewport.Width / 2f), _camera.Position.Y);
                        break;
                    case Direction.Up:
                        _camera.Position =
                            new Vector2(
                                _camera.Position.X, currentRoom.TopLeft.Y -
                                ConvertUnits.ToSimUnits(graphics.GraphicsDevice.Viewport.Height / 2f));
                        break;
                    case Direction.Down:
                        _camera.Position =
                            new Vector2(
                                _camera.Position.X, currentRoom.TopLeft.Y +
                                ConvertUnits.ToSimUnits(graphics.GraphicsDevice.Viewport.Height / 2f));
                        break;
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            graphics.GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
            _tileLevel.Draw(_spriteBatch, _camera, graphics.GraphicsDevice.Viewport.Bounds );
            _player.Draw(_spriteBatch, _camera);

            foreach ( IGameEntity ent in _entities ) {
                ent.Draw(_spriteBatch, _camera);
            }

            _spriteBatch.End();

            _spriteBatch.Begin();
            Constants.Draw(_spriteBatch);
            _spriteBatch.End();

            Matrix projection = _camera.SimProjection;
            Matrix view = _camera.SimView;

            _debugView.RenderDebugData(ref projection, ref view);

            base.Draw(gameTime);
        }

        private void EnableOrDisableFlag(DebugViewFlags flag) {
            if ( (_debugView.Flags & flag) == flag ) {
                _debugView.RemoveFlags(flag);
            } else {
                _debugView.AppendFlags(flag);
            }
        }

        /// <summary>
        /// The identity transform (no rotation) at the location given.
        /// </summary>
        /// <returns></returns>
        public static Transform IdentityTransform(Vector2 centerOfMass) {
            Mat22 m = new Mat22();
            m.SetIdentity();
            Vector2 position = centerOfMass;
            Transform transform = new Transform(ref position, ref m);
            return transform;
        }

        /// <summary>
        /// Determines if any _entities are overlapping the region identified by the shape and transform given.
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static bool EntitiesOverlapping(PolygonShape shape, Transform transform) {
            Transform playerTransform = Player.Instance.Transform;
            PolygonShape playerShape = Player.Instance.Shape;
            Manifold manifold = new Manifold();
            Collision.CollidePolygons(ref manifold, shape, ref transform, playerShape, ref playerTransform);

            return manifold.PointCount > 0;
        }
    }
}


using System;
using System.IO;
using FarseerPhysics;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Arena {
    public class Arena : Microsoft.Xna.Framework.Game {
        private GraphicsDeviceManager graphics;
        private SpriteBatch _spriteBatch;
        private TileLevel _tileLevel;
        private Camera2D _camera;
        private World _world;
        private DebugViewXNA _debugView;
        private Player _player;
        private bool _manualCamera = false;

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
            _world = new World(new Vector2(0, 20f));
            _camera = new Camera2D(graphics.GraphicsDevice);

            _player = new Player(new Vector2(5, 5), _world);
            _player.Image = Content.Load<Texture2D>("samus");

            Shot.Image = Content.Load<Texture2D>("star");

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //_level = new Level(Content, Content.Load<Texture2D>("levelTest"), _world);
            _tileLevel = new TileLevel(Content, Path.Combine(Content.RootDirectory, "Maps", "test_simple.tmx"), _world);

            if ( _debugView == null ) {
                _debugView = new DebugViewXNA(_world);
                _debugView.RemoveFlags(DebugViewFlags.Shape);
                _debugView.RemoveFlags(DebugViewFlags.Joint);
                _debugView.DefaultShapeColor = Color.White;
                _debugView.SleepingShapeColor = Color.LightGray;
                _debugView.LoadContent(GraphicsDevice, Content);
            }
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
                }
            }

            _player.Update();

            if ( !_manualCamera ) {
                // If the sprite goes outside a margin area, move the camera
                const int margin = 150;
                Rectangle viewportMargin = new Rectangle(graphics.GraphicsDevice.Viewport.X + margin,
                                                         graphics.GraphicsDevice.Viewport.Y + margin,
                                                         graphics.GraphicsDevice.Viewport.Width - 2 * margin,
                                                         graphics.GraphicsDevice.Viewport.Height - 2 * margin);
                Vector2 spriteScreenPosition = _camera.ConvertWorldToScreen(_player.Position);
                int maxx =
                    viewportMargin.Right - _player.Image.Width;
                int minx = viewportMargin.Left;
                int maxy =
                    viewportMargin.Bottom - _player.Image.Height;
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

            _tileLevel.Update(gameTime);

            _world.Step(Math.Min((float) gameTime.ElapsedGameTime.TotalSeconds, (1f / 30f)));

            _camera.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            graphics.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
            _tileLevel.Draw(_spriteBatch, _camera, graphics.GraphicsDevice.Viewport.Bounds );
            _player.Draw(_spriteBatch, _camera);
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
    }
}


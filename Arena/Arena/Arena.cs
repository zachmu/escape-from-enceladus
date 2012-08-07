using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Entity;
using Arena.Entity.NPC;
using Arena.Farseer;
using Arena.Map;
using Arena.Overlay;
using Arena.Weapon;
using Arena.Xbox;
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
        Conversation,
    }

    public class Arena : Game {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private TileLevel _tileLevel;
        private Texture2D _background;
        private Camera2D _camera;
        private World _world;
        private DebugViewXNA _debugView;
        private Player _player;
        private Mode _mode;
        private readonly Stack<Mode> _modeStack = new Stack<Mode>(); 
        private InputHelper _inputHelper;

        private HealthStatus _healthStatus;

        private readonly List<IGameEntity> _entities = new List<IGameEntity>();
        private readonly List<IGameEntity> _entitiesToAdd = new List<IGameEntity>();
        private NPC _conversationNPC;

        private bool _manualCamera = false;

        private static Arena _instance;

        public static SpriteFont DebugFont;

        public static Arena Instance {
            get { return _instance; }
        }

        private const string Gravity = "World gravity (m/s/s)";
        private const string ShaderVar1 = "Shader Var 1";
        private const String ShaderVar2 = "Shader Var 2";
        private const String ShaderVar3 = "Shader Var 3";

        public const Category PlayerCategory = Category.Cat1;
        public const Category TerrainCategory = Category.Cat2;
        public const Category PlayerProjectileCategory = Category.Cat3;
        public const Category EnemyCategory = Category.Cat4;
        public const Category NPCCategory = Category.Cat5;

        public static Boolean Debug = true;

        public Arena() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //            graphics.PreferMultiSampling = true;
#if WINDOWS || XBOX
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(64f);
            IsFixedTimeStep = true;
#elif WINDOWS_PHONE
            ConvertUnits.SetDisplayUnitToSimUnitRatio(16f);
            IsFixedTimeStep = false;
#endif
#if WINDOWS
            _graphics.IsFullScreen = false;
#elif XBOX || WINDOWS_PHONE
            _graphics.IsFullScreen = true;
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
            Constants.Register(new Constant(ShaderVar1, .5f, Keys.D1));
            Constants.Register(new Constant(ShaderVar2, .5f, Keys.D2));
            Constants.Register(new Constant(ShaderVar3, .6f, Keys.D3));

            _world = new World(new Vector2(0, Constants.Get(Gravity)));
            _camera = new Camera2D(_graphics.GraphicsDevice);
            _player = new Player(new Vector2(73, 28), _world);
            _healthStatus = new HealthStatus();

            _mode = Mode.NormalControl;
            _inputHelper = new InputHelper();

            base.Initialize();
        }

        /// <summary>
        /// Registers an entity to receive update and draw calls.
        /// Entity will be removed when it is disposed.
        /// </summary>
        public void Register(params IGameEntity[] entity) {
            _entitiesToAdd.AddRange(entity);
        }

        private readonly List<PostProcessingEffect> _postProcessorEffects = new List<PostProcessingEffect>();

        /// <summary>
        /// Registers a post processor effect and draws it every frame.
        /// </summary>
        public void Register(PostProcessingEffect postProcessorEffect) {
            _postProcessorEffects.Add(postProcessorEffect);
        }

        /// <summary>
        /// Registers this NPC as having a conversation, which freezes the action
        /// </summary>
        public void StartConversation(NPC actor) {
            _modeStack.Push(_mode);
            _mode = Mode.Conversation;
            _conversationNPC = actor;
        }

        /// <summary>
        /// End this conversation
        /// </summary>
        public void EndConversation() {
            _mode = _modeStack.Pop();
            _conversationNPC = null;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // There's a dependency here between the player's assets and the NPC's 
            // -- this should go away once they stop sharing animations.
            _player.LoadContent(Content);

            LoadStaticContent();
            _tileLevel = new TileLevel(Content, Path.Combine(Content.RootDirectory, Path.Combine("Maps", "Ship.tmx")), _world,
                                       _player.Position);
            _healthStatus.LoadContent(Content);
            _background = Content.Load<Texture2D>("Background/rock02");

            if ( _debugView == null ) {
                _debugView = new DebugViewXNA(_world);
                _debugView.RemoveFlags(DebugViewFlags.Shape);
                _debugView.RemoveFlags(DebugViewFlags.Joint);
                _debugView.DefaultShapeColor = Color.White;
                _debugView.SleepingShapeColor = Color.LightGray;
                _debugView.LoadContent(GraphicsDevice, Content);
            }

            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            ClampCameraToRoom();

            EnableOrDisableFlag(DebugViewFlags.DebugPanel);
            EnableOrDisableFlag(DebugViewFlags.PerformanceGraph);
        }

        private void LoadStaticContent() {
            Enemy.LoadContent(Content);
            Shot.LoadContent(Content);
            Missile.LoadContent(Content);
            HealthPickup.LoadContent(Content);
            Bomb.LoadContent(Content);
            Sonar.LoadContent(Content);
            SolidColorEffect.LoadContent(Content);
            NPC.LoadContent(Content);
            Constants.font = Content.Load<SpriteFont>("DebugFont");
            DebugFont = Content.Load<SpriteFont>("DebugFont");
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

        private bool _stepSimMode = false;
        private bool _nextSimStep = false;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {

            _entities.AddRange(_entitiesToAdd);
            _entitiesToAdd.Clear();
            
            KeyboardState keyboardState = Keyboard.GetState();
            if ( GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape) )
                this.Exit();

            _inputHelper.Update(gameTime);
            if ( _mode == Mode.NormalControl ) {
                foreach ( var pressedKey in _inputHelper.GetNewKeyPresses() ) {
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
                        case Keys.F10:
                            _stepSimMode = !_stepSimMode;
                            break;
                        case Keys.Enter:
                            _nextSimStep = true;
                            break;
                    }
                }

                foreach ( var pressedKey in _inputHelper.KeyboardState.GetPressedKeys() ) {
                    switch ( pressedKey ) {
                        case Keys.Up:
                            SetManualCamera();
                            _camera.MoveCamera(new Vector2(0, -1));
                            break;
                        case Keys.Down:
                            SetManualCamera();
                            _camera.MoveCamera(new Vector2(0, 1));
                            break;
                        case Keys.Left:
                            SetManualCamera();
                            _camera.MoveCamera(new Vector2(-1, 0));
                            break;
                        case Keys.Right:
                            SetManualCamera();
                            _camera.MoveCamera(new Vector2(1, 0));
                            break;
                        case Keys.LeftAlt:
                            _manualCamera = false;
                            ClampCameraToRoom();
                            break;
                    }
                }

                Constants.Update(_inputHelper);

                _world.Gravity = new Vector2(0, Constants.Get(Gravity));

                // Step every simulation element
                if ( !_stepSimMode || _nextSimStep ) {
                    InputHelper.Instance.Update(gameTime);

                    float step = 0f;
                    while ( step < Math.Min((float) gameTime.ElapsedGameTime.TotalSeconds, (1f / 30f)) ) {
                        _world.Step((1f / 60f));
                        step += (1f / 59f);
                    }

                    foreach ( IGameEntity ent in _entities ) {
                        ent.Update(gameTime);
                    }

                    // Make sure our game mode hasn't change before giving the player a chance to respond
                    if ( _mode == Mode.NormalControl ) {
                        _player.Update(gameTime);
                    }

                    _tileLevel.Update(gameTime);
                    _nextSimStep = false;
                }

                TrackPlayer();
            } else if (_mode == Mode.Conversation) {
                InputHelper.Instance.Update(gameTime);
                _conversationNPC.Update(gameTime);
            }

            _camera.Update(gameTime);

            if ( _mode == Mode.RoomTransition ) {
                if (_camera.IsAtTarget()) {
                    _mode = Mode.NormalControl;
                    ClampCameraToRoom();
                }
            }

            _entities.RemoveAll(entity => entity.Disposed);
            _postProcessorEffects.RemoveAll(effect => effect.Disposed);

            base.Update(gameTime);
        }

        private void SetManualCamera() {
            _manualCamera = true;
            UnclampCamera();
        }

        /// <summary>
        /// Moves the camera to follow the player around the screen.
        /// </summary>
        private void TrackPlayer() {
            if ( !_manualCamera ) {
                // If the sprite goes outside a margin area, move the camera
                const int margin = 250;
                Rectangle viewportMargin = new Rectangle(_graphics.GraphicsDevice.Viewport.X + margin,
                                                         _graphics.GraphicsDevice.Viewport.Y + margin,
                                                         _graphics.GraphicsDevice.Viewport.Width - 2 * margin,
                                                         _graphics.GraphicsDevice.Viewport.Height - 2 * margin);
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

                currentRoom = _tileLevel.SetCurrentRoom(_player.Position);
                foreach ( IGameEntity gameEntity in _entities.Where(entity => !currentRoom.Contains(entity.Position)) ) {
                    gameEntity.Dispose();
                }
                switch ( directionOfTravel ) {
                    case Direction.Left:
                        _camera.Position =
                            new Vector2(
                                currentRoom.BottomRight.X -
                                ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Width / 2f), _camera.Position.Y);

                        break;
                    case Direction.Right:
                        _camera.Position =
                            new Vector2(
                                currentRoom.TopLeft.X +
                                ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Width / 2f), _camera.Position.Y);
                        break;
                    case Direction.Up:
                        _camera.Position =
                            new Vector2(
                                _camera.Position.X, currentRoom.BottomRight.Y -
                                ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Height / 2f));
                        break;
                    case Direction.Down:
                        _camera.Position =
                            new Vector2(
                                _camera.Position.X, currentRoom.TopLeft.Y +
                                ConvertUnits.ToSimUnits(_graphics.GraphicsDevice.Viewport.Height / 2f));
                        break;
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {

            using (
                // Render first to a new back buffer
                RenderTarget2D renderTarget = new RenderTarget2D(_graphics.GraphicsDevice,
                                                                 _graphics.PreferredBackBufferWidth,
                                                                 _graphics.PreferredBackBufferHeight) ) {

                _graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                _graphics.GraphicsDevice.Clear(Color.Black);

                // Background image
                _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.Opaque, SamplerState.LinearWrap,
                                   DepthStencilState.None, RasterizerState.CullNone);
                Vector2 origin = _camera.Position;
                origin = ConvertUnits.ToDisplayUnits(origin) / 4;
                _spriteBatch.Draw(_background, Vector2.Zero,
                                  new Rectangle((int) origin.X, (int) origin.Y, GraphicsDevice.Viewport.Bounds.Width,
                                                GraphicsDevice.Viewport.Bounds.Height), Color.White);
                _spriteBatch.End();

                // Block layer
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _tileLevel.Draw(_spriteBatch, _camera, _graphics.GraphicsDevice.Viewport.Bounds);
                _spriteBatch.End();

                // Dynamic elements (player, enemies, missiles, etc)
                _spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect, _camera.DisplayView);
                foreach ( IGameEntity ent in _entities ) {
                    ent.Draw(_spriteBatch, _camera);
                }
                _player.Draw(_spriteBatch, _camera);
                _spriteBatch.End();

                // Now apply post-processing to the scene
                using (
                    RenderTarget2D tempTarget = new RenderTarget2D(_graphics.GraphicsDevice,
                                                                   _graphics.PreferredBackBufferWidth,
                                                                   _graphics.PreferredBackBufferHeight) ) {
                    foreach ( PostProcessingEffect effect in _postProcessorEffects ) {
                        // Draw the scene onto a temporary render target
                        _graphics.GraphicsDevice.SetRenderTarget(tempTarget);
                        _spriteBatch.Begin();
                        _spriteBatch.Draw(renderTarget, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height),
                                          Color.White);
                        _spriteBatch.End();

                        // Then draw this temp buffer back onto the original render 
                        // target, adding this round of effect
                        _graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                        effect.SetEffectParameters(_camera, _spriteBatch);
                        _spriteBatch.Begin(0, null, null, null, null, effect.Effect);
                        _spriteBatch.Draw(tempTarget, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height),
                                          Color.White);
                        _spriteBatch.End();
                    }
                }

                // Finally draw the final scene onto the screen
                _graphics.GraphicsDevice.SetRenderTarget(null);
                _graphics.GraphicsDevice.Clear(Color.Black);
                _spriteBatch.Begin();
                _spriteBatch.Draw(renderTarget, new Rectangle(0, 0, renderTarget.Width, renderTarget.Height),
                                  Color.White);
                _spriteBatch.End();
            }

            // Draw overlays on top
            _spriteBatch.Begin();
            _healthStatus.Draw(_spriteBatch, _camera);
            _spriteBatch.End();

            // And any debug info
            DebugDraw();

            base.Draw(gameTime);
        }

        private void DebugDraw() {
            _spriteBatch.Begin();
            Constants.Draw(_spriteBatch);
            _spriteBatch.End();

            Matrix projection = _camera.SimProjection;
            Matrix view = _camera.SimView;
            _debugView.RenderDebugData(ref projection, ref view);
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
        /// Determines if any entities are overlapping the region identified by the shape at the transformation given.
        /// /summary>
        public static bool EntitiesOverlapping(PolygonShape shape, Vector2 centerOfShape) {
            return EntitiesOverlapping(shape, IdentityTransform(centerOfShape));
        }

        /// <summary>
        /// Determines if any entities are overlapping the region identified by the shape and transform given.
        /// </summary>
        public static bool EntitiesOverlapping(PolygonShape shape, Transform transform) {

            Manifold manifold = new Manifold();

            foreach ( IGameEntity ent in Instance._entities ) {
                Transform entTransform = IdentityTransform(ent.Position);
                PolygonShape entShape = ent.Shape;
                if ( entShape != null ) {
                    Collision.CollidePolygons(ref manifold, shape, ref transform, entShape, ref entTransform);
                    if ( manifold.PointCount > 0 ) {
                        return true;
                    }
                }
            }

            Transform playerTransform = Player.Instance.Transform;
            PolygonShape playerShape = Player.Instance.Shape;
            Collision.CollidePolygons(ref manifold, shape, ref transform, playerShape, ref playerTransform);

            return manifold.PointCount > 0;
        }
    }
}


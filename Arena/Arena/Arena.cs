using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Control;
using Arena.Entity;
using Arena.Entity.Enemy;
using Arena.Entity.NPC;
using Arena.Event;
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
    
    // TODO: this doesn't belong here
    enum Mode {
        NormalControl,
        RoomTransition,
        Conversation,
    }

    public class Arena : Game {
        private readonly GraphicsDeviceManager _graphics;

        public GraphicsDeviceManager GraphicsDeviceManager {
            get { return _graphics; }
        }

        private SpriteBatch _spriteBatch;
        private TileLevel _tileLevel;
        private Texture2D _background;
        private Camera2D _camera;
        private PlayerPositionMonitor _playerPositionMonitor;
        private CameraDirector _cameraDirector;
        private World _world;
        private DebugViewXNA _debugView;
        private Player _player;
        private Mode _mode;
        private readonly Stack<Mode> _modeStack = new Stack<Mode>(); 
        private InputHelper _inputHelper;
        private Conversation _currentConversation;
        private ConversationManager _conversationManager;
        private EventManager _eventManager; 

        private HealthStatus _healthStatus;
        private VisitationMap _visitationMap;

        private readonly List<IGameEntity> _entities = new List<IGameEntity>();
        private readonly List<IGameEntity> _entitiesToAdd = new List<IGameEntity>();

        private static Arena _instance;

        public static SpriteFont DebugFont;

        public static Arena Instance {
            get { return _instance; }
        }

        private const string Gravity = "World gravity (m/s/s)";
        private const string ShaderVar1 = "Shader Var 1";
        private const String ShaderVar2 = "Shader Var 2";
        private const String ShaderVar3 = "Shader Var 3";
        private const string DebugCamera = "DebugCamera";

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
            Constants.Register(new Constant(DebugCamera, .99f, Keys.C));

            _world = new World(new Vector2(0, Constants.Get(Gravity)));
            _camera = new Camera2D(_graphics.GraphicsDevice);
            _player = new Player(new Vector2(73, 28), _world); // TODO: start position unnecessary, set in tile level
            _healthStatus = new HealthStatus();
            _inputHelper = new InputHelper();
            PlayerControl.Control = new PlayerGamepadControl();

            _cameraDirector = new CameraDirector(_camera, _player, _graphics, _inputHelper);
            _playerPositionMonitor = new PlayerPositionMonitor(_player);
            _conversationManager = new ConversationManager(Content);
            _eventManager = EventManager.Instance;
            _mode = Mode.NormalControl;

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
        /// Registers a conversation as having started, which freezes the action.
        /// </summary>
        public void ConversationStarted(Conversation conversation) {
            _modeStack.Push(_mode);
            _mode = Mode.Conversation;
            _currentConversation = conversation;
        }

        /// <summary>
        /// End this conversation
        /// </summary>
        public void EndConversation() {
            _mode = _modeStack.Pop();
            _currentConversation = null;
        }        

        // TODO: there is a better way to do this.
        private float _backgroundAlpha = 1;
        public float BackgroundAlpha { get { return _backgroundAlpha;  } }

        /// <summary>
        /// Dispose of all the entities in the room mentioned.
        /// </summary>
        public void DisposeRoom(Room disposeRoom) {
            if ( disposeRoom != null ) {
                foreach ( IGameEntity gameEntity in _entities.Where(entity => disposeRoom.Contains(entity.Position)) ) {
                    gameEntity.Dispose();
                }
                _mode = Mode.RoomTransition;
            }
        }

        /// <summary>
        /// Loads assets for the room given
        /// TODO: a transition / background manager should really handle this
        /// </summary>
        public void LoadRoom(Room room) {
            if ( room.BackgroundImage != null ) {
                _background = Content.Load<Texture2D>("Background/" + room.BackgroundImage);
            }
            _mode = Mode.NormalControl;
        }

        /// <summary>
        /// Pauses the simulation and update of all game elements
        /// </summary>
        public void PauseSimulation() {
            _simulationPaused = true;
        }

        /// <summary>
        /// Resumes simulation and update of all game elements
        /// </summary>
        public void ResumeSimulation() {
            _simulationPaused = false;
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
            _tileLevel = new TileLevel(Content, Path.Combine(Content.RootDirectory, Path.Combine("Maps", "Ship.tmx")), _world);
            _visitationMap = new VisitationMap(_tileLevel);
            _healthStatus.LoadContent(Content);
            _background = Content.Load<Texture2D>("Background/Microscheme_0_edited");
            //_background = Content.Load<Texture2D>("Background/rock02");

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

            // Boostrap the map position and current room data
            _playerPositionMonitor.Update(new GameTime());
           
            // Bootstrap camera position
            _cameraDirector.TargetPlayer();
            _cameraDirector.JumpToTarget();
            _cameraDirector.ClampCameraToRegion(_playerPositionMonitor.CurrentRegion);

            //EnableOrDisableFlag(DebugViewFlags.DebugPanel);
            //EnableOrDisableFlag(DebugViewFlags.PerformanceGraph);
        }

        private void LoadStaticContent() {
            PacingEnemy.LoadContent(Content);
            Worm.LoadContent(Content);
            Beetle.LoadContent(Content);
            Shot.LoadContent(Content);
            Missile.LoadContent(Content);
            HealthPickup.LoadContent(Content);
            Bomb.LoadContent(Content);
            Sonar.LoadContent(Content);
            SolidColorEffect.LoadContent(Content);
            VisitationMap.LoadContent(Content);
            NPC.LoadContent(Content);
            Constants.font = Content.Load<SpriteFont>("DebugFont");
            Camera2D.LoadContent(Content);
            DebugFont = Content.Load<SpriteFont>("DebugFont");
            EventManager.Instance.LoadContent(Content);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        private bool _simulationPaused = false;
        private bool _nextSimStep = false;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            
            KeyboardState keyboardState = Keyboard.GetState();
            if ( GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape) )
                this.Exit();

            _entities.AddRange(_entitiesToAdd);
            _entitiesToAdd.Clear();

            _inputHelper.Update(gameTime);
            HandleDebugControl();

            if ( _mode == Mode.NormalControl ) {
                Constants.Update(_inputHelper);

                _world.Gravity = new Vector2(0, Constants.Get(Gravity));

                // Step every simulation element
                if ( !_simulationPaused || _nextSimStep ) {
                    InputHelper.Instance.Update(gameTime);

                    StepWorld(gameTime);
    
                    foreach ( IGameEntity ent in _entities ) {
                        ent.Update(gameTime);
                    }

                    // Make sure our game mode hasn't changed before giving the player a chance to respond
                    if ( _mode == Mode.NormalControl ) {
                        _player.Update(gameTime);
                    }

                    _tileLevel.Update(gameTime);
                    _nextSimStep = false;
                }

            } else if (_mode == Mode.Conversation) {
                InputHelper.Instance.Update(gameTime);
                _currentConversation.Update(gameTime);
            }

            UpdateBackgroundAlpha();

            _camera.Update(gameTime);
            _playerPositionMonitor.Update(gameTime);
            _visitationMap.Update(gameTime);
            _cameraDirector.Update(gameTime);
            _eventManager.Update(gameTime);

            _entities.RemoveAll(entity => entity.Disposed);
            _postProcessorEffects.RemoveAll(effect => effect.Disposed);

            base.Update(gameTime);
        }

        /// <summary>
        /// Steps the game world one frame, without updating any game entities.
        /// </summary>
        /// <param name="gameTime"></param>
        internal void StepWorld(GameTime gameTime) {
            float totalSeconds = gameTime == null ? 1f / 60f : (float) gameTime.ElapsedGameTime.TotalSeconds;
            _world.Step(Math.Min(totalSeconds, (1f / 30f)));
        }

        private void UpdateBackgroundAlpha() {
            if (_mode == Mode.RoomTransition && _backgroundAlpha > 0) {
                _backgroundAlpha -= .05f;
            } else if (_backgroundAlpha < 1) {
                _backgroundAlpha += .05f;
            }
        }

        private void HandleDebugControl() {
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
                        _simulationPaused = !_simulationPaused;
                        break;
                    case Keys.Space:
                        if ( PlayerControl.Control.IsKeyboardControl() ) {
                            PlayerControl.Control = new PlayerGamepadControl();                        
                        } else {
                            PlayerControl.Control = new PlayerKeyboardControl();
                        }
                        break;
                    case Keys.Enter:
                        _nextSimStep = true;
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
                                                GraphicsDevice.Viewport.Bounds.Height), Color.White * _backgroundAlpha);
                _spriteBatch.End();

                // Background content
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _tileLevel.DrawBackground(_spriteBatch, _camera, _graphics.GraphicsDevice.Viewport.Bounds);
                _spriteBatch.End();

                // Dynamic elements (player, enemies, missiles, etc)
                _spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect, _camera.DisplayView);
                foreach ( IGameEntity ent in _entities ) {
                    ent.Draw(_spriteBatch, _camera);
                }
                _player.Draw(_spriteBatch, _camera);
                _spriteBatch.End();

                // Foreground content
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _tileLevel.DrawForeground(_spriteBatch, _camera, _graphics.GraphicsDevice.Viewport.Bounds);
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

                // Draw the final scene onto the screen
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
            _visitationMap.Draw(_spriteBatch);
            _spriteBatch.End();

            // Conversation is considered an overlay
            if ( IsInConversation ) {
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _currentConversation.Draw(_spriteBatch, _camera);
                _spriteBatch.End();
            }

            // Finally, debug info
            if (Constants.Get(DebugCamera) >= 1) {
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _camera.Draw(_spriteBatch);
                _spriteBatch.End();
            }

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
        /// Determines if any entities are overlapping the region identified by the 
        /// axis-aligned bounding box given.
        /// </summary>
        public static bool EntitiesOverlapping(AABB aabb) {

            bool overlapping = false;
            Instance._world.QueryAABB(fixture => {
                if ( !fixture.IsSensor && (fixture.GetUserData().IsPlayer || fixture.GetUserData().IsEnemy ||
                                           fixture.GetUserData().IsProjectile || fixture.GetUserData().IsDoor) ) {
                    overlapping = true;
                    return false;
                }
                return true;
            }, ref aabb);

            return overlapping;
        }

        /// <summary>
        /// Returns whether a conversation is taking place.
        /// </summary>
        /// <value> </value>
        public bool IsInConversation {
            get { return _mode == Mode.Conversation; }
        }
    }
}


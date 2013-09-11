using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Enceladus.Entity.InteractiveObject;
using Enceladus.Xbox;
using Enceladus.Control;
using Enceladus.Entity;
using Enceladus.Entity.Enemy;
using Enceladus.Entity.NPC;
using Enceladus.Event;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using Enceladus.Weapon;
using FarseerPhysics;
using FarseerPhysics.Collision;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Path = System.IO.Path;

namespace Enceladus {
    
    public enum Mode {
        NormalControl,
        RoomTransition,
        Conversation,
        Saving,
        Paused,
        TitleScreen,
        Teleport,
        Death
    }

    [Flags]
    public enum DestructionFlags {
        None,
        NormalWeaponDestructionFlag,
        DashDestructionFlag, 
        BombDestructionFlag, 
        BeamDestructionFlag, 
        HolocubeDestructionFlag,
        All = 0xFFFF,
    }

    public class EnceladusGame : Game {
        private readonly GraphicsDeviceManager _graphics;

        public GraphicsDeviceManager GraphicsDeviceManager {
            get { return _graphics; }
        }

        int _totalFrames = 0;
        float _elapsedTime = 0.0f;
        int _fps = 0;

        private SpriteBatch _spriteBatch;
        private TileLevel _tileLevel;
        private PlayerIndex _slot;
        private Camera2D _camera;
        private PlayerPositionMonitor _playerPositionMonitor;
        private CameraDirector _cameraDirector;
        private World _world;
        private DebugViewXNA _debugView;
        private Player _player;
        private Mode _mode;
        private Mode _drawMode;
        private readonly Stack<Mode> _modeStack = new Stack<Mode>(); 
        private InputHelper _inputHelper;
        private PauseScreen _pauseScreen;
        private TitleScreen _titleScreen;
        private DeathScreen _deathScreen;
        private ConversationManager _conversationManager;
        private EventManager _eventManager;
        private BackgroundManager _backgroundManager;
        private AudioEngine _audioEngine;
        private MusicManager _musicManager;
        private SoundEffectManager _soundEffectManager;
        private WaitHandle _roomChangeWaitHandle;
        private Vector2 _nextPlayerPosition;
        private VisitationMap _visitationMap;
        private OverlayManager _overlayManager;

        private readonly List<IGameEntity> _entities = new List<IGameEntity>();
        private readonly List<IGameEntity> _entitiesToAdd = new List<IGameEntity>();
        private readonly object _entityLock = new object();

        private static EnceladusGame _instance;

        public static SpriteFont DebugFont;

        public static EnceladusGame Instance {
            get { return _instance; }
        }

        public Mode Mode {
            get { return _mode; }
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
        public const Category PlayerSensorCategory = Category.Cat6;

//        public const int NormalWeaponDestructionFlag = 1;
//        public const int DashDestructionFlag = 2;
//        public const int BombDestructionFlag = 4;
//        public const int BeamDestructionFlag = 8;
//        public const int HolocubeDestructionFlag = 16;

        public static Boolean Debug = true;

        public EnceladusGame() {
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

            Constants.Register(new Constant(Gravity, 30f, Keys.G));
            Constants.Register(new Constant(ShaderVar1, .5f, Keys.D1));
            Constants.Register(new Constant(ShaderVar2, .5f, Keys.D2));
            Constants.Register(new Constant(ShaderVar3, .6f, Keys.D3));
            Constants.Register(new Constant(DebugCamera, .99f, Keys.C));

            _world = new World(new Vector2(0, Constants.Get(Gravity)));
            _camera = new Camera2D(_graphics.GraphicsDevice);
            _player = new Player(new Vector2(73, 28), _world); 
            _inputHelper = new InputHelper();
            PlayerControl.Control = new PlayerGamepadControl();

            _playerPositionMonitor = new PlayerPositionMonitor(_player);
            _playerPositionMonitor.RoomChanged += RoomChanged;

            _cameraDirector = new CameraDirector(_camera, _player, _graphics, _inputHelper);

            _conversationManager = new ConversationManager(Content);
            _conversationManager.ConversationStarted += ConversationStarted;
            _conversationManager.ConversationEnded += conversation => UnsetMode();

            _backgroundManager = new BackgroundManager(Content);
            _eventManager = new EventManager();
            _pauseScreen = new PauseScreen();
            _titleScreen = new TitleScreen();
            _deathScreen = new DeathScreen();
            _musicManager = new MusicManager();
            _soundEffectManager = new SoundEffectManager();

            GoToTitleScreen();

            base.Initialize();
        }

        /// <summary>
        /// Registers an entity to receive update and draw calls.
        /// Entity will be removed when it is disposed.
        /// </summary>
        public void Register(params IGameEntity[] entities) {
            if ( entities != null ) {
                foreach ( IGameEntity e in entities ) {
                    if ( e != null ) {
                        lock ( _entityLock ) {
                            _entitiesToAdd.Add(e);
                        }
                    }
                }
            }
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
            SetMode(Mode.Conversation);
            Register(conversation);
        }

        /// <summary>
        /// Sets the game mode to the one given.
        /// </summary>
        public void SetMode(Mode mode) {
            if ( mode != _mode ) {
                _modeStack.Push(_mode);
                _mode = mode;

                // Teleport is a pseudo mode, since it doesn't affect drawing
                if ( _mode != Mode.Teleport ) {
                    _drawMode = _mode;
                }
            }
        }

        /// <summary>
        /// Immediately returns to the title screen.
        /// </summary>
        public void GoToTitleScreen() {
            _modeStack.Clear();
            _mode = Mode.NormalControl;
            SetMode(Mode.TitleScreen);
            _titleScreen.Reset();
            _musicManager.SetMusicTrack("spur");
        }

        /// <summary>
        /// Restores the game mode to whatever it was before the last call to SetMode()
        /// </summary>
        public void UnsetMode() {
            if ( _modeStack.Count == 0 ) {
                _mode = Mode.NormalControl;
            } else {
                _mode = _modeStack.Pop();
                _drawMode = _mode;
            }
        }

        /// <summary>
        /// Returns a save state with all game state populated.
        /// </summary>
        public SaveState GetSaveState() {
            return new SaveState(_slot, _visitationMap);
        }

        /// <summary>
        /// Applies the save state to the game, effectively loading it into memory.
        /// Returns a wait handle to signal when the game can resume.
        /// </summary>
        public WaitHandle ApplySaveState(SaveState saveState) {
            _slot = saveState.Slot;
            saveState.ApplyToGameState(_visitationMap);

            return TeleportPlayer(saveState.SaveStationLocation);
        }

        /// <summary>
        /// Starts a new game with the player slot given.
        /// Returns a wait handle to signal when the game can start.
        /// </summary>
        public WaitHandle NewGame(PlayerIndex slot) {
            _slot = slot;
;
            return TeleportPlayer((Vector2) _tileLevel.GetPlayerStartPosition());
        }

        /// <summary>
        /// Instantly updates the game state to the player's current location without any room transition logic.
        /// </summary>
        /// <param name="position"></param>
        public WaitHandle TeleportPlayer(Vector2 position) {
            _nextPlayerPosition = position;

            Room newRoom = _tileLevel.RoomAt(position);
            _tileLevel.SetCurrentRoom(_playerPositionMonitor.PreviousFrameRoom, newRoom);

            SetMode(Mode.Teleport);

            return _roomChangeWaitHandle;
        }

        /// <summary>
        /// Dispose of all the entities in the room mentioned.
        /// </summary>
        public void RoomChanged(Room oldRoom, Room newRoom) {
            DisposeRoom(oldRoom, newRoom);
        }

        /// <summary>
        /// Disposes all entities in the room given that aren't also in the current room
        /// </summary>
        private void DisposeRoom(Room oldRoom, Room currentRoom) {
            if ( oldRoom != null ) {
                foreach ( IGameEntity gameEntity in _entities ) {
                    if ( oldRoom.Contains(gameEntity) && !currentRoom.Contains(gameEntity) ) {
                        gameEntity.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Sets a handle to wait on an asynch room change event. 
        /// </summary>
        public void SetRoomChangeWaitHandle(WaitHandle handle) {
            _roomChangeWaitHandle = handle;
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

            _overlayManager = new OverlayManager(new HealthStatus(), new RapidFire(), new WeaponSelector(),
                                                 new VisitationMapOverlay(_visitationMap));

            _backgroundManager.LoadContent();
            _audioEngine = new AudioEngine("Content/Music/game.xgs");
            _musicManager.LoadContent(_audioEngine, Content);
            _soundEffectManager.LoadContent(_audioEngine, Content);

            // Audio engine needs an initial update after loading songs
            _audioEngine.Update();

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
           
            //EnableOrDisableFlag(DebugViewFlags.DebugPanel);
            //EnableOrDisableFlag(DebugViewFlags.PerformanceGraph);
        }

        private void LoadStaticContent() {
            SharedGraphicalAssets.LoadContent(Content);
            Door.LoadContent(Content);
            PacingEnemy.LoadContent(Content);
            Worm.LoadContent(Content);
            Beetle.LoadContent(Content);
            SkullBeetle.LoadContent(Content);
            Shot.LoadContent(Content);
            Turret.LoadContent(Content);
            Missile.LoadContent(Content);
            Beam.LoadContent(Content);
            Holocube.LoadContent(Content);
            ProjectionBeam.LoadContent(Content);
            Springboard.LoadContent(Content);
            HoverBoots.LoadContent(Content);
            HealthStatus.LoadContent(Content);
            HealthPickup.LoadContent(Content);
            WeaponSelector.LoadContent(Content);
            RapidFire.LoadContent(Content);
            GenericCollectibleItem.LoadContent(Content);
            Bomb.LoadContent(Content);
            Sonar.LoadContent(Content);
            SolidColorEffect.LoadContent(Content);
            VisitationMapOverlay.LoadContent(Content);
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
             
            // FPS counter
            _elapsedTime += (float) gameTime.ElapsedGameTime.TotalMilliseconds;            
            if ( _elapsedTime >= 1000f ) {
                _fps = _totalFrames;
                _totalFrames = 0;
                _elapsedTime %= 1000f;
            }

            KeyboardState keyboardState = Keyboard.GetState();
            if ( keyboardState.IsKeyDown(Keys.Escape) )
                this.Exit();

            // Entities get added asynchronously on room creation, 
            // as well as synchronously during gameplay (e.g. projectiles)
            lock ( _entityLock ) {
                _entities.AddRange(_entitiesToAdd);
                _entitiesToAdd.FindAll(entity => entity is IEnemy)
                              .ForEach(entity => EnemyMonitor.Instance.EnemyAdded((IEnemy) entity));
                _entitiesToAdd.Clear();
            }

            _inputHelper.Update(gameTime);
            HandleDebugControl();

            switch ( _mode ) {
                case Mode.NormalControl:
                    Constants.Update(_inputHelper);

                    _world.Gravity = new Vector2(0, Constants.Get(Gravity));

                    // Step every simulation element
                    if ( !_simulationPaused || _nextSimStep ) {
                        InputHelper.Instance.Update(gameTime);

                        StepWorld(gameTime);

                        // Make sure our game mode hasn't changed before giving the player a chance to respond
                        if ( _mode == Mode.NormalControl ) {
                            _player.Update(gameTime);
                        }

                        foreach ( IGameEntity ent in _entities ) {
                            ent.Update(gameTime);
                        }

                        _tileLevel.Update(gameTime);
                        _nextSimStep = false;
                    }

                    _playerPositionMonitor.Update(true);
                    _eventManager.Update(gameTime);
                    _visitationMap.Update(gameTime);
                    _overlayManager.Update(gameTime);
                    _cameraDirector.Update(gameTime);

                    UpdateMode();

                    break;
                case Mode.TitleScreen:
                    InputHelper.Instance.Update(gameTime);
                    _titleScreen.Update(gameTime);
                    break;
                case Mode.Teleport:
                    if ( _roomChangeWaitHandle.WaitOne(0) ) {
                        _player.CreateBody(_nextPlayerPosition);

                        _playerPositionMonitor.Update(false);
                        DisposeRoom(_playerPositionMonitor.PreviousRoom, _playerPositionMonitor.CurrentRoom);

                        _backgroundManager.LoadRoom(_playerPositionMonitor.CurrentRoom);
                        _musicManager.RoomChanged(_playerPositionMonitor.PreviousFrameRoom,
                                                  _playerPositionMonitor.CurrentRoom);
                        _cameraDirector.ForceRestart();

                        StepWorld(null);
                        UnsetMode();
                    }
                    break;
                case Mode.Paused:
                    InputHelper.Instance.Update(gameTime);
                    _pauseScreen.Update(gameTime);
                    break;
                case Mode.Death:
                    InputHelper.Instance.Update(gameTime);
                    _deathScreen.Update(gameTime);
                    foreach ( IGameEntity ent in _entities ) {
                        ent.Update(gameTime);
                    }
                    break;
                case Mode.RoomTransition:
                    _cameraDirector.Update(gameTime);
                    break;
                default:
                    InputHelper.Instance.Update(gameTime);
                    foreach ( IGameEntity ent in _entities.Where(entity => entity.UpdateInMode(_mode)) ) {
                        ent.Update(gameTime);
                    }
                    break;
            }

            _backgroundManager.Update(gameTime);
            _camera.Update(gameTime);
            _musicManager.Update();
            _soundEffectManager.Update();
            _audioEngine.Update();

            _entities.FindAll(entity => entity.Disposed && entity is IEnemy).ForEach(entity => EnemyMonitor.Instance.EnemyRemoved((IEnemy) entity));
            _entities.RemoveAll(entity => entity.Disposed);
            _postProcessorEffects.RemoveAll(effect => effect.Disposed);

            base.Update(gameTime);
        }

        /// <summary>
        /// Updates the current mode based on player input, only during normal control
        /// </summary>
        private void UpdateMode() {
            if ( PlayerControl.Control.IsNewPause() ) {
                SetMode(Mode.Paused);
            }
        }

        /// <summary>
        /// Steps the game world one frame, without updating any game entities.
        /// </summary>
        /// <param name="gameTime"></param>
        internal void StepWorld(GameTime gameTime) {
            float totalSeconds = gameTime == null ? 1f / 60f : (float) gameTime.ElapsedGameTime.TotalSeconds;
            _debugView.ResetPointCount();
            _world.Step(Math.Min(totalSeconds, (1f / 30f)));
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
                        EnableOrDisableFlag(DebugViewFlags.Sensors);
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

            // FPS counter
            _totalFrames++;

            switch ( _drawMode ) {
                case Mode.TitleScreen:
                    _titleScreen.Draw(_spriteBatch);
                    break;
                case Mode.Death:
                    DrawGameWorld(_deathScreen.GetScreenAlpha());

                    _spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect, _camera.DisplayView);
                    foreach ( IGameEntity ent in _entities.Where(entity => !entity.DrawAsOverlay) ) {
                        ent.Draw(_spriteBatch, _camera);
                    }
                    _player.Draw(_spriteBatch, _camera);
                    _spriteBatch.End();

                    _spriteBatch.Begin();
                    _deathScreen.Draw(_spriteBatch, _camera);
                    _spriteBatch.End();

                    break;
                default:
                    DrawGame();
                    break;
            }

            DebugDraw(gameTime);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Initiates the death screen
        /// </summary>
        public void Die() {
            _entities.ForEach(entity => entity.Dispose());
            _deathScreen.Reset();
            SetMode(Mode.Death);
        }

        /// <summary>
        /// Draws the game for most game modes.
        /// </summary>
        private void DrawGame() {
            DrawGameWorld(1f);

            DrawOverlayEntities();

            DrawOverlays();
        }

        /// <summary>
        /// Draws the game world and all entities, using the specified alpha
        /// </summary>
        private void DrawGameWorld(float alpha) {

            // Perversely, we need to draw the overlays first, 
            // since they need to clear a screen-sized piece of real estate.
            _overlayManager.PrepareDraw(_spriteBatch);

            using (
                // Render first to a new back buffer
                RenderTarget2D renderTarget = new RenderTarget2D(_graphics.GraphicsDevice,
                                                                 _graphics.PreferredBackBufferWidth,
                                                                 _graphics.PreferredBackBufferHeight) ) {
                _graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                _graphics.GraphicsDevice.Clear(Color.Black);

                // Background image
                _backgroundManager.Draw(_spriteBatch, _camera);

                // Background content
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _tileLevel.DrawBackground(_spriteBatch, _camera, _graphics.GraphicsDevice.Viewport.Bounds);
                _spriteBatch.End();

                // Dynamic elements (player, enemies, missiles, etc)
                _spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect, _camera.DisplayView);
                foreach ( IGameEntity ent in _entities.Where(entity => !entity.DrawAsOverlay) ) {
                    ent.Draw(_spriteBatch, _camera);
                }
                if ( !_player.Disposed ) {
                    _player.Draw(_spriteBatch, _camera);
                }
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
                                  Color.White*alpha);
                _spriteBatch.End();
            }
        }

        /// <summary>
        /// Some game entities, such as those with textual prompts, 
        /// need to be drawn on top of everything else.
        /// </summary>
        private void DrawOverlayEntities() {
            if ( _entities.Any(entity => entity.DrawAsOverlay) ) {
                _spriteBatch.Begin(0, null, null, null, null, SolidColorEffect.Effect, _camera.DisplayView);
                foreach ( IGameEntity ent in _entities.Where(entity => entity.DrawAsOverlay) ) {
                    ent.Draw(_spriteBatch, _camera);
                }
                _spriteBatch.End();
            }
        }

        /// <summary>
        /// Draws all overlays on top of the game screen
        /// </summary>
        private void DrawOverlays() {
            _overlayManager.Draw(_spriteBatch, _camera);
            if ( _mode == Mode.Paused ) {
                _spriteBatch.Begin();
                _pauseScreen.Draw(_spriteBatch, _camera);
                _spriteBatch.End();
            }
        }

        /// <summary>
        /// Draws various debugging information
        /// </summary>
        /// <param name="time"></param>
        private void DebugDraw(GameTime time) {
            // Finally, debug info
            if ( Constants.Get(DebugCamera) >= 1 ) {
                _spriteBatch.Begin(0, null, null, null, null, null, _camera.DisplayView);
                _camera.Draw(_spriteBatch);
                _spriteBatch.End();

                _spriteBatch.Begin();
                Constants.Draw(_spriteBatch);
                _spriteBatch.DrawString(SharedGraphicalAssets.DialogFont, "" + _fps + " " + time.IsRunningSlowly, new Vector2(20), Color.White);
                _spriteBatch.End();
            }

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
                                           fixture.GetUserData().IsProjectile || fixture.GetUserData().IsUserTool) ) {
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


        /// <summary>
        /// Returns whether the current room change is complete.
        /// </summary>
        public bool RoomChangeComplete() {
            return _roomChangeWaitHandle.WaitOne(10);
        }
    }
}


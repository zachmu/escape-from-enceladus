using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Test {
    public class Arena : Microsoft.Xna.Framework.Game {
        GraphicsDeviceManager graphics;
        SpriteBatch _spriteBatch;
        Level _level;
        Camera2D _camera;
        private World _world;
        private DebugViewXNA _debugView;
        private float _characterHeight = .5f;
        private float _characterWidth = .5f;

        public Arena() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //            graphics.PreferMultiSampling = true;
#if WINDOWS || XBOX
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(32f);
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

            _character = BodyFactory.CreateRectangle(_world, _characterWidth, _characterHeight, 1f);
            _character.IsStatic = false;
            _character.Restitution = 0.2f;
            _character.Friction = 0.2f;
            _character.Position = new Vector2(5,5);

            base.Initialize();
        }

        // This is a texture we can render.
        Texture2D _myTexture;

        // Set the coordinates to draw the sprite at.
//        Vector2 _spritePosition = new Vector2(5,5);

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _myTexture = Content.Load<Texture2D>("welcome16");
            _level = new Level(Content, Content.Load<Texture2D>("levelTest"), _world);

            if (_debugView == null)
            {
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
        private Body _character;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            KeyboardState keyboardState = Keyboard.GetState();
            if ( GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape) )
                this.Exit();

            Vector2 leftStick = GamePad.GetState(PlayerIndex.One).ThumbSticks.Left;
            Vector2 adjustedDelta = new Vector2(leftStick.X * 10f, leftStick.Y * -10f);
            foreach (var pressedKey in keyboardState.GetPressedKeys()) {
                switch ( pressedKey ) {
                    case Keys.Left:
                        adjustedDelta.X += -1f;
                        break;
                    case Keys.Right:
                        adjustedDelta.X += 1f;
                        break;
                    case Keys.Down:
                        adjustedDelta.Y += 1f;
                        break;
                    case Keys.Up:
                        adjustedDelta.Y += -1f;
                        break;
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
            _spriteSpeed += adjustedDelta;

            _character.ApplyLinearImpulse(adjustedDelta);

            // Move the sprite by speed, scaled by elapsed time.
/*
            _spritePosition +=
                _spriteSpeed * (float) gameTime.ElapsedGameTime.TotalSeconds;
*/

            // If the sprite goes outside a margin area, move the camera
            int margin = 40;
            Rectangle viewportMargin = new Rectangle(graphics.GraphicsDevice.Viewport.X + margin, graphics.GraphicsDevice.Viewport.Y + margin,
                graphics.GraphicsDevice.Viewport.Width - 2 * margin, graphics.GraphicsDevice.Viewport.Height - 2 * margin);
            Vector2 spriteScreenPosition = _camera.ConvertWorldToScreen(_character.Position);
            int maxx =
                viewportMargin.Right - _myTexture.Width;
            int minx = viewportMargin.Left;
            int maxy =
                viewportMargin.Bottom - _myTexture.Height;
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

            _world.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, (1f / 30f)));

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
            _level.Draw(_spriteBatch, _camera);
            Vector2 position = _character.Position;
            position.X -= _characterWidth / 2;
            position.Y -= _characterHeight / 2;
            _spriteBatch.Draw(_myTexture, ConvertUnits.ToDisplayUnits(position), Microsoft.Xna.Framework.Color.White);
            _spriteBatch.End();

            Matrix projection = _camera.SimProjection;
            Matrix view = _camera.SimView;

            _debugView.RenderDebugData(ref projection, ref view);


            base.Draw(gameTime);
        }

        private void EnableOrDisableFlag(DebugViewFlags flag)
        {
            if ((_debugView.Flags & flag) == flag)
            {
                _debugView.RemoveFlags(flag);
            }
            else
            {
                _debugView.AppendFlags(flag);
            }
        }


    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using FarseerPhysics.Dynamics;
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

        public Arena() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //            graphics.PreferMultiSampling = true;
#if WINDOWS || XBOX
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            ConvertUnits.SetDisplayUnitToSimUnitRatio(24f);
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
            _world = new World(new Vector2(0, -10f));
            _camera = new Camera2D(graphics.GraphicsDevice);

            base.Initialize();
        }

        // This is a texture we can render.
        Texture2D myTexture;

        // Set the coordinates to draw the sprite at.
        Vector2 spritePosition = Vector2.Zero;

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            myTexture = Content.Load<Texture2D>("welcome16");
            _level = new Level(Content, Content.Load<Texture2D>("levelTest"));
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        private Vector2 spriteSpeed = new Vector2((float) 100.0);

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            if ( GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed )
                this.Exit();

            Vector2 leftStick = GamePad.GetState(PlayerIndex.One).ThumbSticks.Left;
            Vector2 adjustedDelta = new Vector2(leftStick.X * 10f, leftStick.Y * -10f);
            spriteSpeed += adjustedDelta;

            // Move the sprite by speed, scaled by elapsed time.
            spritePosition +=
                spriteSpeed * (float) gameTime.ElapsedGameTime.TotalSeconds;

            // If the sprite goes outside a margin area, move the camera
            int margin = 40;
            Rectangle viewportMargin = new Rectangle(graphics.GraphicsDevice.Viewport.X + margin, graphics.GraphicsDevice.Viewport.Y + margin,
                graphics.GraphicsDevice.Viewport.Width - 2 * margin, graphics.GraphicsDevice.Viewport.Height - 2 * margin);
            int maxx =
                viewportMargin.Right - myTexture.Width;
            int minx = viewportMargin.Left;
            int maxy =
                viewportMargin.Bottom - myTexture.Height;
            int miny = viewportMargin.Top;

            // Move the camera and adjust the sprite back to the edge of the viewport.
            if ( spritePosition.X > maxx ) {
                float delta = spritePosition.X - maxx;
                spritePosition.X = maxx;
                _camera.MoveCamera(new Vector2(delta, 0));
            } else if ( spritePosition.X < minx ) {
                float delta = spritePosition.X - minx;
                spritePosition.X = minx;
                _camera.MoveCamera(new Vector2(delta, 0));
            }

            if ( spritePosition.Y > maxy ) {
                float delta = spritePosition.Y - maxy;
                spritePosition.Y = maxy;
                _camera.MoveCamera(new Vector2(0, delta));
            } else if ( spritePosition.Y < miny ) {
                float delta = spritePosition.Y - miny;
                spritePosition.Y = miny;
                _camera.MoveCamera(new Vector2(0, delta));
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            graphics.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            // Draw the sprite.
            _spriteBatch.Begin(0, null, null, null, null, null, Camera.DisplayView);
            _spriteBatch.Draw(myTexture, spritePosition, Microsoft.Xna.Framework.Color.White);
            _level.Draw(_spriteBatch, _camera);
            _spriteBatch.End();

            base.Draw(gameTime);
        }



    }
}


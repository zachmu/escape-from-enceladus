using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arena.Farseer;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {

    /// <summary>
    /// Simple break-up destruction animation
    /// </summary>
    public class ShatterAnimation : IGameEntity {

        private bool _disposed;
        private readonly Texture2D _image;
        private readonly Rectangle _originRectangle;
        private Piece[] _pieces;

        private readonly int _displayPieceWidth;
        private readonly int _displayPieceHeight;

        private int _timeAlive = 0;
        private const int TimeToLiveMs = 2000;
        private float _maxVelocity = 0;

        private const float defaultMaxVelocity = 5;

        private static Random random = new Random();

        /// <summary>
        /// Begins a new destruction animation for the image given.
        /// </summary>
        /// <param name="world"> </param>
        /// <param name="image">The image to destroy</param>
        /// <param name="originRectangle">The rectangle on the texture to split up and draw</param>
        /// <param name="position">The center of the original image</param>
        /// <param name="numPieces">The number of horizontal and vertical pieces to split the image into</param>
        public ShatterAnimation(World world, Texture2D image, Rectangle? originRectangle, Vector2 position, int numPieces) 
            : this(world, image, originRectangle, position, numPieces, defaultMaxVelocity) {        
        }

        /// <summary>
        /// Begins a new destruction animation for the image given.
        /// </summary>
        /// <param name="world"> </param>
        /// <param name="image">The image to destroy</param>
        /// <param name="originRectangle">The rectangle on the texture to split up and draw</param>
        /// <param name="position">The center of the original image</param>
        /// <param name="numPieces">The number of horizontal and vertical pieces to split the image into</param>
        /// <param name="maxVelocity">The maximum velocity of any individual piece</param>
        public ShatterAnimation(World world, Texture2D image, Rectangle? originRectangle, Vector2 position, int numPieces, float maxVelocity) {
            _image = image;
            _originRectangle = originRectangle ?? new Rectangle(0, 0, 0, 0);
            _pieces = new Piece[numPieces * numPieces];
            _maxVelocity = maxVelocity;

            int width = originRectangle == null ? _image.Width : originRectangle.Value.Width;
            int height = originRectangle == null ? _image.Height : originRectangle.Value.Height;

            _displayPieceWidth = width / numPieces;
            float simPieceWidth = ConvertUnits.ToSimUnits(_displayPieceWidth);
            _displayPieceHeight = height / numPieces;
            float simPieceHeight = ConvertUnits.ToSimUnits(_displayPieceHeight);

            int displayWidthOffset = width / 2;
            float simWidthOffset = ConvertUnits.ToSimUnits(displayWidthOffset);
            int displayHeightOffset = height / 2;
            float simHeightOffset = ConvertUnits.ToSimUnits(displayHeightOffset);

            int i = 0;
            for ( int x = 0; x < numPieces; x++ ) {
                for ( int y = 0; y < numPieces; y++ ) {
                    float posx = position.X - simWidthOffset + (x * simPieceWidth + simPieceWidth / 2);
                    float posy = position.Y - simHeightOffset + (y * simPieceHeight + simPieceHeight / 2);
                    //Body body = BodyFactory.CreateCircle(world, simPieceWidth / 2 - .05f, 1);
                    Body body = BodyFactory.CreateRectangle(world, simPieceWidth * 3f / 4f, simPieceHeight * 3f / 4f, 1);
                    body.CollidesWith = Arena.TerrainCategory;
                    body.CollisionCategories = Arena.TerrainCategory;
                    body.Position = new Vector2(posx, posy);
                    body.IsStatic = false;
                    body.FixedRotation = false;
                    body.Restitution = .6f;
                    AssignRandomDirection(body);
                    _pieces[i++] = new Piece(body, x, y);
                }
            }
        }

        private class Piece {
            public Piece(Body body, int x, int y) {
                Body = body;
                X = x;
                Y = y;
            }

            public Body Body { get; private set; }
            public int X { get; private set; }
            public int Y { get; private set; }
        }

        private void AssignRandomDirection(Body body) {
            double linearVelocity = random.NextDouble() * _maxVelocity;
            double direction = random.NextDouble() * Math.PI * 2;
            Vector2 velocity = new Vector2((float) (Math.Cos(direction) * linearVelocity),
                (float) (Math.Sin(direction) * linearVelocity));
            body.LinearVelocity = velocity;
        }

        public void Dispose() {
            _disposed = true;
            foreach ( Piece piece in _pieces ) {
                piece.Body.Dispose();
            }
        }

        public bool Disposed {
            get { return _disposed; }
        }

        public Vector2 Position {
            get { return new Vector2();}
        }

        public PolygonShape Shape {
            get { return null; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            float alpha = 1f - (float) _timeAlive / (float) TimeToLiveMs;
            int xOffset = _originRectangle.X;
            int yOffset = _originRectangle.Y;
            foreach ( Piece piece in _pieces ) {
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(piece.Body.Position);
                Vector2 origin = new Vector2(_displayPieceWidth / 2, _displayPieceHeight / 2);
                float rotation = piece.Body.Rotation;
                spriteBatch.Draw(_image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, _displayPieceWidth,
                                               _displayPieceHeight),
                                 new Rectangle(xOffset + _displayPieceWidth * piece.X, yOffset + _displayPieceHeight * piece.Y, _displayPieceWidth,
                                               _displayPieceHeight),
                                 Color.Black * alpha, rotation, origin,
                                 SpriteEffects.None, 0);

            }

        }

        public void Update(GameTime gameTime) {
            _timeAlive += gameTime.ElapsedGameTime.Milliseconds;
            if ( _timeAlive > TimeToLiveMs ) {
                Dispose();
            }
        }
    }
}

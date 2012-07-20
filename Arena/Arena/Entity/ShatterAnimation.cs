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
        private Piece[] _pieces;

        private readonly int _displayPieceWidth;
        private readonly int _displayPieceHeight;

        private int _timeAlive = 0;
        private const int TimeToLiveMs = 2000;

        private static Random random = new Random();

        /// <summary>
        /// Begins a new destruction animation for the image given.
        /// </summary>
        /// <param name="image">The image to destroy</param>
        /// <param name="position">The center of the original image</param>
        /// <param name="numPieces">The number of horizontal and vertical pieces to split the image into</param>
        public ShatterAnimation(World world, Texture2D image, Vector2 position, int numPieces) {
            _image = image;
            _pieces = new Piece[numPieces * numPieces];

            _displayPieceWidth = _image.Width / numPieces;
            float simPieceWidth = ConvertUnits.ToSimUnits(_displayPieceWidth);
            _displayPieceHeight = _image.Height / numPieces;
            float simPieceHeight = ConvertUnits.ToSimUnits(_displayPieceHeight);

            int displayWidthOffset = _image.Width / 2;
            float simWidthOffset = ConvertUnits.ToSimUnits(displayWidthOffset);
            int displayHeightOffset = _image.Height / 2;
            float simHeightOffset = ConvertUnits.ToSimUnits(displayHeightOffset);
            int i = 0;
            for ( int x = 0; x < numPieces; x++ ) {
                for ( int y = 0; y < numPieces; y++ ) {
                    float posx = position.X - simWidthOffset + (x * simPieceWidth + simPieceWidth / 2);
                    float posy = position.Y - simHeightOffset + (y * simPieceHeight + simPieceHeight / 2);
                    Body body = BodyFactory.CreateRectangle(world, simPieceWidth, simPieceHeight, 1);
                    body.CollidesWith = Arena.TerrainCategory;
                    body.CollisionCategories = Arena.TerrainCategory;
//                    body.CollidesWith = Category.All;
//                    body.CollisionCategories = Category.All;
                    body.Position = new Vector2(posx, posy);
                    body.IsStatic = false;
                    body.FixedRotation = false;
                    body.Restitution = .6f;
                    AssignRandomDirection(body);
                    _pieces[i++] = new Piece(body, x, y);
                }
            }
        }

        private void AssignRandomDirection(Body body) {
            double linearVelocity = random.NextDouble() * 5;
            double direction = random.NextDouble() * Math.PI * 2;
            Vector2 velocity = new Vector2((float) (Math.Cos(direction) * linearVelocity), 
                (float) (Math.Sin(direction) * linearVelocity));
            body.LinearVelocity = velocity;
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
            foreach ( Piece piece in _pieces ) {
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(piece.Body.Position);
                Vector2 origin = new Vector2(_displayPieceWidth / 2, _displayPieceHeight / 2);
                float rotation = piece.Body.Rotation;
                spriteBatch.Draw(_image,
                                 new Rectangle((int) displayPosition.X, (int) displayPosition.Y, _displayPieceWidth,
                                               _displayPieceHeight),
                                 new Rectangle(_displayPieceWidth * piece.X, _displayPieceHeight * piece.Y, _displayPieceWidth,
                                               _displayPieceHeight),
                                 Color.White * alpha, rotation, origin,
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Test {
    class Level {
        Tile[] Tiles { get; set; }
        int Height { get; set; }
        int Width { get; set; }

        public Level(ContentManager cm, Texture2D source) {
            this.Height = source.Height;
            this.Width = source.Width;

            Color[] data = new Color[Height * Width];
            source.GetData<Color>(data);
            Tiles = new Tile[data.Length];

            Texture2D texture = cm.Load<Texture2D>("met_tile");
            LandTile.Image = texture;
            BlankTile.Image = cm.Load<Texture2D>("star");

            for ( int y = 0; y < Height; y++ ) {
                for ( int x = 0; x < Width; x++ ) {
                    int index = y * Width + x;
                    if ( data[index] == Color.Black ) {
                        Tiles[index] = new LandTile();
                    } else {
                        Tiles[index] = new BlankTile();
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            for ( int y = 0; y < Height; y++ ) {
                for ( int x = 0; x < Width; x++ ) {
                    int index = y * Width + x;
                    Tiles[index].Draw(spriteBatch, camera, x, y);
                }
            }
        }
    }

    abstract class Tile {
        internal const int TILE_SIZE = 32;
        internal abstract void Draw(SpriteBatch spriteBatch, Camera2D camera, int x, int y);
    }

    class BlankTile : Tile {

        public static Texture2D Image { get; set; }

        private Vector2[] stars = new Vector2[3];
        private const int starSize = 3;

        static Random r = new Random();

        public BlankTile() {
            
            for ( int i = 0; i < stars.Length; i++ ) {
                stars[i] = new Vector2((float) ( r.NextDouble() * (double) TILE_SIZE ), (float) (r.NextDouble() * (double) TILE_SIZE ));
            }             
        }

        internal override void Draw(SpriteBatch spriteBatch, Camera2D camera, int x, int y) {
            var tileCorner = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
            foreach ( Vector2 star in stars ) {
                if ( camera.Viewport.Intersects(new Rectangle((int)(star.X + tileCorner.X), (int) (star.Y + tileCorner.Y), starSize, starSize)) ) {
                    Vector2 projectedPosition = new Vector2(( star.X + tileCorner.X ) - camera.Viewport.X, ( star.Y + tileCorner.Y ) - camera.Viewport.Y);
                    spriteBatch.Draw(Image, projectedPosition, Color.White);
                }
            }
        }
    }

    class LandTile : Tile {

        public static Texture2D Image { get; set; }

        internal override void Draw(SpriteBatch spriteBatch, Camera2D camera, int x, int y) {
            Vector2 basePosition = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
            if ( camera.Viewport.Intersects(new Rectangle((int) basePosition.X, (int) basePosition.Y, TILE_SIZE, TILE_SIZE)) ) {
                Vector2 projectedPosition = new Vector2(basePosition.X - camera.Viewport.X, basePosition.Y - camera.Viewport.Y);
                spriteBatch.Draw(Image, projectedPosition, Color.White);
            }
        }
    }
}

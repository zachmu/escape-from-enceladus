/*
Squared.Tiled
Copyright (C) 2009 Kevin Gadd

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Kevin Gadd kevin.gadd@gmail.com http://luminance.org/
*/
/*
 * Updates by Stephen Belanger - July, 13 2009
 * 
 * -added ProhibitDtd = false, so you don't need to remove the doctype line after each time you edit the map.
 * -changed everything to use SortedLists for easier referencing
 * -added objectgroups
 * -added movable and resizable objects
 * -added object images
 * -added meta property support to maps, layers, object groups and objects
 * -added non-binary encoded layer data
 * -added layer and object group transparency
 * 
 * TODO: I might add support for .tsx Tileset definitions. Note sure yet how beneficial that would be...
*/
/*
 * Modifications by Zach Musgrave - 10 June 2012.
 * 
 * - Added Tile class
 * - Fixed bug with tile info caching (e.g. GetTile(x,y))
 * - Fixed bug when loading objects with the same name
 * - Fixed bug when loading an object without a height or width attribute
 * - Fixed layer properties loading bugs
 * - Moved load logic into its own file
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Arena.Entity;
using Arena.Farseer;
using Arena.Xbox;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Arena.Map {

    public partial class Tileset {
        public class TilePropertyList : Dictionary<string, string> {
        }

        public string Name;
        public int FirstTileId;
        public int TileWidth;
        public int TileHeight;
        public int Spacing;
        public int Margin;
        public Dictionary<int, TilePropertyList> TileProperties = new Dictionary<int, TilePropertyList>();
        public string Image;
        protected Texture2D _Texture;
        protected int _TexWidth, _TexHeight;

        public TilePropertyList GetTileProperties(int index) {
            index -= FirstTileId;

            if ( index < 0 )
                return null;

            TilePropertyList result = null;
            TileProperties.TryGetValue(index, out result);

            return result;
        }

        public Texture2D Texture {
            get {
                return _Texture;
            }
            set {
                _Texture = value;
                _TexWidth = value.Width;
                _TexHeight = value.Height;
            }
        }

        internal bool MapTileToRect(int index, ref Rectangle rect) {
            index -= FirstTileId;

            if ( index < 0 )
                return false;

            int rowSize = _TexWidth / TileWidth;
            int row = index / rowSize;
            int numRows = _TexHeight / TileHeight;
            if ( row >= numRows )
                return false;

            int col = index % rowSize;

            rect.X = col * TileWidth + col * Spacing + Margin;
            rect.Y = row * TileHeight + row * Spacing + Margin;
            rect.Width = TileWidth;
            rect.Height = TileHeight;
            return true;
        }
    }

    public partial class Layer {
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        internal Map _map;

        public string Name;
        public int Width, Height;
        public float Opacity = 1;
        public int[] Tiles;
        public byte[] FlipAndRotate;

        /// <summary>
        /// Returns the tile at these coordinates, or null if no such tile exists.
        /// </summary>
        public Tile GetTile(int x, int y) {
            if ( (x < 0) || (y < 0) || (x >= Width) || (y >= Height) )
                return null;

            int index = (y * Width) + x;
            return GetTiles()[index];
        }

        /// <summary>
        /// Returns the tile at these coordinates, or null if no such tile exists.
        /// </summary>
        public Tile GetTile(Vector2 v) {
            return GetTile((int) v.X, (int) v.Y);
        }

        private IList<Tile> _tiles;

        /// <summary>
        /// Returns all tiles in this layer as a dense list,
        /// with nulls representing empty spaces.
        /// </summary>
        public IList<Tile> GetTiles() {

            if ( _tiles == null ) {
                TileInfo[] tileInfoCache = _map.GetTileInfoCache();
                _tiles = new List<Tile>();
                for ( int y = 0; y < Height; y++ ) {
                    for ( int x = 0; x < Width; x++ ) {
                        int cacheIndex = Tiles[(y * Width) + x] - 1;
                        if ( (cacheIndex >= 0) && (cacheIndex < tileInfoCache.Length) ) {
                            _tiles.Add(new Tile(this, new Vector2(x, y), cacheIndex));
                        } else {
                            _tiles.Add(null);
                        }
                    }
                }
            }

            return _tiles;
        }

        internal readonly List<Tile> _destroyedTiles = new List<Tile>(); 

        /// <summary>
        /// Returns the tile underneath this one, or null if there is no tile there
        /// </summary>
        internal Tile GetDownTile(Tile tile) {
            if ( tile == null )
                return null;
            return GetTile((int) tile.Position.X, (int) tile.Position.Y + 1);
        }

        internal Tile GetRightTile(Tile tile) {
            if ( tile == null )
                return null;
            return GetTile((int) tile.Position.X + 1, (int) tile.Position.Y);
        }

        internal Tile GetUpTile(Tile tile) {
            if ( tile == null )
                return null;
            return GetTile((int) tile.Position.X, (int) tile.Position.Y - 1);
        }

        internal Tile GetLeftTile(Tile tile) {
            if ( tile == null )
                return null;
            return GetTile((int) tile.Position.X - 1, (int) tile.Position.Y);
        }

        public void Draw(SpriteBatch batch, ICollection<Tileset> tilesets, Rectangle visibleWorld) {

            int minx = Math.Max(visibleWorld.Left - 1, 0);
            int miny = Math.Max(visibleWorld.Top - 1, 0);
            int maxx = Math.Min(visibleWorld.Right + 2, Width);
            int maxy = Math.Min(visibleWorld.Bottom + 2, Height);

            for ( int y = miny; y <= maxy; y++ ) {
                for ( int x = minx; x <= maxx; x++ ) {
                    var tile = GetTile(x, y);
                    if ( tile != null )
                        tile.Draw(batch);
                }
            }
        }

        public void Update(GameTime gameTime) {
            Layer blockLayer = _map.Layers["Blocks"];
            foreach ( Tile tile in _destroyedTiles ) {
                tile.Update(gameTime);
                int x = (int) tile.Position.X;
                int y = (int) tile.Position.Y;
                int index = y * Width + x;
                Tile blockLayerTile = blockLayer.GetTile(x, y);
                if ( blockLayerTile != null ) {
                    blockLayerTile.Update(gameTime);
                }
            }

            _destroyedTiles.RemoveAll(tile => !tile.Disposed && tile.Age > Tile.FadeInTime);
            //_destroyedTiles.Remove
        }
    }

    public struct TileInfo {
        public Texture2D Texture;
        public Rectangle Rectangle;
    }

    public class Tile : IEquatable<Tile> {

        /// <summary>
        /// Returns the simulation position of this tile's upper-left corner.
        /// </summary>
        public Vector2 Position { get; private set; }

        const float HalfTileSize = TileLevel.TileSize / 2;

        // TODO: share this data
        public readonly IList<Body> Bodies = new List<Body>();
        public HashSet<Tile> Group;
        private readonly int _tileInfoIndex;

        private Layer _layer;
        private Layer GetLayer() {
            return _layer;
        }

        public bool Disposed { get; internal set; }
        public int TimeUntilReappear { get; internal set; }
        public int Age { get; internal set; }

        public Tile(Layer layer, Vector2 position, int tileInfoIndex) {
            Position = position;
            _layer = layer;
            _tileInfoIndex = tileInfoIndex;
        }

        public Tile GetLeftTile() {
            return GetLayer().GetLeftTile(this);
        }

        public Tile GetUpTile() {
            return GetLayer().GetUpTile(this);
        }

        public Tile GetRightTile() {
            return GetLayer().GetRightTile(this);
        }

        public Tile GetDownTile() {
            return GetLayer().GetDownTile(this);
        }

        public bool Equals(Tile other) {
            if ( ReferenceEquals(null, other) ) {
                return false;
            }
            if ( ReferenceEquals(this, other) ) {
                return true;
            }
            return other.Position.Equals(Position);
        }

        public override bool Equals(object obj) {
            if ( ReferenceEquals(null, obj) ) {
                return false;
            }
            if ( ReferenceEquals(this, obj) ) {
                return true;
            }
            if ( obj.GetType() != typeof(Tile) ) {
                return false;
            }
            return Equals((Tile) obj);
        }

        public override int GetHashCode() {
            return Position.GetHashCode();
        }

        public static bool operator ==(Tile left, Tile right) {
            return Equals(left, right);
        }

        public static bool operator !=(Tile left, Tile right) {
            return !Equals(left, right);
        }

        /// <summary>
        /// Destroys this tile, removing it and any underlying block model from the game model.
        /// </summary>
        public void Dispose() {
            if ( Disposed )
                return;

            Age = 1;
            TimeUntilReappear = BlockTimeUntilReappear;
            DestroyAttachedBodies();
            Disposed = true;

            GetLayer()._destroyedTiles.Add(this);
        }

        /// <summary>
        /// Destroys all Box2d bodies attached to this tile's group.
        /// </summary>
        public void DestroyAttachedBodies() {
            foreach ( Body body in Bodies ) {
                body.Dispose();
            }
        }

        /// <summary>
        /// Revives this tile, restoring it and any underlying block model to the game model.
        /// </summary>
        public void Revive() {
            if ( !Disposed )
                return;

            Console.WriteLine("Reviving tile at {0}", Position);
            Disposed = false;
        }

        public void Draw(SpriteBatch batch) {
            if ( Disposed )
                return;

            float alpha = 1.0f;
            if ( Age > 0 && Age < FadeInTime ) {
                alpha = (float) Age / (float) FadeInTime;
            }

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(Position + new Vector2(.5f));
            TileInfo tileInfo = GetLayer()._map.GetTileInfoCache()[_tileInfoIndex];
            int index = ((int) Position.Y * GetLayer().Width) + (int) Position.X;

            byte flipAndRotate = GetLayer().FlipAndRotate[index];
            SpriteEffects flipEffect = SpriteEffects.None;
            float rotation = 0f;

            if ( (flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0 ) {
                flipEffect |= SpriteEffects.FlipHorizontally;
            }
            if ( (flipAndRotate & Layer.VerticalFlipDrawFlag) != 0 ) {
                flipEffect |= SpriteEffects.FlipVertically;
            }
            if ( (flipAndRotate & Layer.DiagonallyFlipDrawFlag) != 0 ) {
                if ( (flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0 && (flipAndRotate & Layer.VerticalFlipDrawFlag) != 0 ) {
                    rotation = (float) (Math.PI / 2);
                    flipEffect ^= SpriteEffects.FlipVertically;
                } else if ( (flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0 ) {
                    rotation = (float) -(Math.PI / 2);
                    flipEffect ^= SpriteEffects.FlipVertically;
                } else {
                    rotation = (float) (Math.PI / 2);
                    flipEffect ^= SpriteEffects.FlipHorizontally;
                }
            }

            batch.Draw(tileInfo.Texture, displayPosition, tileInfo.Rectangle,
                       Color.White * alpha, rotation, new Vector2(32), 1f, flipEffect, 0);
        }

        /// <summary>
        /// Intended for external callers only, this returns the tile info 
        /// for the block layer corresponding to this tile.
        /// </summary>
        public TileInfo GetBlockTextureInfo() {
            int tileInfoIndex = GetLayer()._map.Layers["Blocks"].GetTile(this.Position)._tileInfoIndex;
            return GetLayer()._map.GetTileInfoCache()[tileInfoIndex];
        }

        public void Update(GameTime gameTime) {
            if ( Disposed ) {
                TimeUntilReappear -= gameTime.ElapsedGameTime.Milliseconds;
                if ( TimeUntilReappear <= 0 ) {                    
                    TileLevel.CurrentLevel.ReviveTile(this);
                }
            } else {
                Age += gameTime.ElapsedGameTime.Milliseconds;
            }
        }

        public bool EntitiesOverlapping() {
            Vertices vertices = PolygonTools.CreateRectangle(HalfTileSize, HalfTileSize);
            PolygonShape shape = new PolygonShape(vertices, 0f);

            return Arena.EntitiesOverlapping(shape, Position + new Vector2(HalfTileSize, HalfTileSize));
        }

        private const int BlockTimeUntilReappear = 5000;
        public const int FadeInTime = 1000;        
    }

    public partial class ObjectGroup {
        public List<Object> Objects = new List<Object>();
        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        public string Name;
        public int Width, Height, X, Y;
        float Opacity = 1;

        public void Draw(Map result, SpriteBatch batch, Rectangle rectangle) {
            foreach ( var obj in Objects ) {
                if ( obj.Texture != null ) {
                    obj.Draw(batch, rectangle);
                }
            }
        }
    }

    public partial class Object {
        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        public string Name, Image;
        public int Width, Height, X, Y;

        protected Texture2D _Texture;
        protected int _TexWidth, _TexHeight;

        public Texture2D Texture {
            get {
                return _Texture;
            }
            set {
                _Texture = value;
                _TexWidth = value.Width;
                _TexHeight = value.Height;
            }
        }


        public void Draw(SpriteBatch batch, Rectangle visibleWorld) {
            var tileCorner = new Vector2(X - 1f / 2f,
                                         Y - 1f / 2f);
            Vector2 displayPosition = new Vector2();
            ConvertUnits.ToDisplayUnits(ref tileCorner, out displayPosition);

            int minx = Math.Max(visibleWorld.Left - 1, 0);
            int miny = Math.Max(visibleWorld.Top - 1, 0);
            int maxx = Math.Min(visibleWorld.Right + 2, Width);
            int maxy = Math.Min(visibleWorld.Bottom + 2, Height);

            if ( this.X + this.Width > minx && this.X < maxx )
                if ( this.Y + this.Height > miny && this.Y < maxy ) {
                    batch.Draw(_Texture, displayPosition, new Rectangle(0, 0, _Texture.Width, _Texture.Height), Color.White);
                }
        }
    }

    public partial class Map {
        public Dictionary<string, Tileset> Tilesets = new Dictionary<string, Tileset>();
        public Dictionary<string, Layer> Layers = new Dictionary<string, Layer>();
        public Dictionary<string, ObjectGroup> ObjectGroups = new Dictionary<string, ObjectGroup>();
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        public int Width, Height;
        public int TileWidth, TileHeight;
        private TileInfo[] _tileInfoCache = null;

        public TileInfo[] GetTileInfoCache() {
            if ( _tileInfoCache == null ) {
                _tileInfoCache = BuildTileInfoCache(Tilesets.Values);
            }
            return _tileInfoCache;
        }

        protected TileInfo[] BuildTileInfoCache(ICollection<Tileset> tilesets) {
            Rectangle rect = new Rectangle();
            var cache = new List<TileInfo>();
            int i = 1;

        next:
            foreach (Tileset tileset in tilesets) {
                if ( tileset.MapTileToRect(i, ref rect) ) {
                    cache.Add(new TileInfo {
                        Texture = tileset.Texture,
                        Rectangle = rect
                    });
                    i += 1;
                    goto next;
                }
            }

            return cache.ToArray();
        }


        public void DrawBackground(SpriteBatch batch, Rectangle visibleWorld) {
            foreach (
                Layer layer in
                    Layers.Values.Where(
                        layer =>
                        !layer.Properties.ContainsKey("invisible") && !layer.Properties.ContainsKey("foreground")) ) {
                layer.Draw(batch, Tilesets.Values, visibleWorld);
            }
        }

        public void DrawForeground(SpriteBatch batch, Rectangle visibleWorld) {
            foreach (
                Layer layer in
                    Layers.Values.Where(
                        layer => layer.Properties.ContainsKey("foreground")) ) {
                layer.Draw(batch, Tilesets.Values, visibleWorld);
            }
        }

        public void Update(GameTime gameTime) {
            foreach ( Layer layer in Layers.Values ) {
                layer.Update(gameTime);
            }
        }
    }
}

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Arena;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using System.IO;
using System.IO.Compression;

namespace Squared.Tiled {
    public class Tileset {
        public class TilePropertyList : Dictionary<string, string> {
        }

        public string Name;
        public int FirstTileID;
        public int TileWidth;
        public int TileHeight;
        public Dictionary<int, TilePropertyList> TileProperties = new Dictionary<int, TilePropertyList>();
        public string Image;
        protected Texture2D _Texture;
        protected int _TexWidth, _TexHeight;

        internal static Tileset Load(XmlReader reader) {
            var result = new Tileset();

            result.Name = reader.GetAttribute("name");
            result.FirstTileID = int.Parse(reader.GetAttribute("firstgid"));
            result.TileWidth = int.Parse(reader.GetAttribute("tilewidth"));
            result.TileHeight = int.Parse(reader.GetAttribute("tileheight"));

            int currentTileId = -1;

            while ( reader.Read() ) {
                var name = reader.Name;

                switch ( reader.NodeType ) {
                    case XmlNodeType.Element:
                        switch ( name ) {
                            case "image":
                                result.Image = reader.GetAttribute("source");
                                break;
                            case "tile":
                                currentTileId = int.Parse(reader.GetAttribute("id"));
                                break;
                            case "property": {
                                    TilePropertyList props;
                                    if ( !result.TileProperties.TryGetValue(currentTileId, out props) ) {
                                        props = new TilePropertyList();
                                        result.TileProperties[currentTileId] = props;
                                    }

                                    props[reader.GetAttribute("name")] = reader.GetAttribute("value");
                                } break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return result;
        }

        public TilePropertyList GetTileProperties(int index) {
            index -= FirstTileID;

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
            index -= FirstTileID;

            if ( index < 0 )
                return false;

            int rowSize = _TexWidth / TileWidth;
            int row = index / rowSize;
            int numRows = _TexHeight / TileHeight;
            if ( row >= numRows )
                return false;

            int col = index % rowSize;

            rect.X = col * TileWidth;
            rect.Y = row * TileHeight;
            rect.Width = TileWidth;
            rect.Height = TileHeight;
            return true;
        }
    }

    public class Layer {
        public SortedList<string, string> Properties = new SortedList<string, string>();
        internal Map _map;

        public string Name;
        public int Width, Height;
        public float Opacity = 1;
        public int[] Tiles;

        internal static Layer Load(Map map, XmlReader reader) {
            var result = new Layer();
            result._map = map;

            if ( reader.GetAttribute("name") != null )
                result.Name = reader.GetAttribute("name");
            if ( reader.GetAttribute("width") != null )
                result.Width = int.Parse(reader.GetAttribute("width"));
            if ( reader.GetAttribute("height") != null )
                result.Height = int.Parse(reader.GetAttribute("height"));
            if ( reader.GetAttribute("opacity") != null )
                result.Opacity = float.Parse(reader.GetAttribute("opacity"));

            result.Tiles = new int[result.Width * result.Height];

            while ( !reader.EOF ) {
                var name = reader.Name;

                switch ( reader.NodeType ) {
                    case XmlNodeType.Element:
                        switch ( name ) {
                            case "data": {
                                    if ( reader.GetAttribute("encoding") != null ) {
                                        var encoding = reader.GetAttribute("encoding");
                                        var compressor = reader.GetAttribute("compression");
                                        switch ( encoding ) {
                                            case "base64": {
                                                    int dataSize = (result.Width * result.Height * 4) + 1024;
                                                    var buffer = new byte[dataSize];
                                                    reader.ReadElementContentAsBase64(buffer, 0, dataSize);

                                                    Stream stream = new MemoryStream(buffer, false);
                                                    if ( compressor == "gzip" || compressor == "zlib" )
                                                        stream = new GZipStream(stream, CompressionMode.Decompress, false);

                                                    using ( stream )
                                                    using ( var br = new BinaryReader(stream) ) {
                                                        for ( int i = 0; i < result.Tiles.Length; i++ )
                                                            result.Tiles[i] = br.ReadInt32();
                                                    }

                                                    continue;
                                                }
                                                ;

                                            default:
                                                throw new Exception("Unrecognized encoding.");
                                        }
                                    } else {
                                        using ( var st = reader.ReadSubtree() ) {
                                            int i = 0;
                                            while ( !st.EOF ) {
                                                switch ( st.NodeType ) {
                                                    case XmlNodeType.Element:
                                                        if ( st.Name == "tile" ) {
                                                            if ( i < result.Tiles.Length ) {
                                                                result.Tiles[i] = int.Parse(st.GetAttribute("gid"));
                                                                i++;
                                                            }
                                                        }

                                                        break;
                                                    case XmlNodeType.EndElement:
                                                        break;
                                                }

                                                st.Read();
                                            }
                                        }
                                    }
                                    Console.WriteLine("It made it!");
                                }
                                break;
                            case "properties": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        while ( !st.EOF ) {
                                            switch ( st.NodeType ) {
                                                case XmlNodeType.Element:
                                                    if ( st.Name == "property" ) {
                                                        if ( st.GetAttribute("name") != null ) {
                                                            result.Properties.Add(st.GetAttribute("name"),
                                                                                  st.GetAttribute("value"));
                                                        }
                                                    }

                                                    break;
                                                case XmlNodeType.EndElement:
                                                    break;
                                            }

                                            st.Read();
                                        }
                                    }
                                }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        /// <summary>
        /// Returns the tile at these coordinates, or null if no such tile exists.
        /// </summary>
        public Tile GetTile(int x, int y) {
            if ( (x < 0) || (y < 0) || (x >= Width) || (y >= Height) )
                return null;

            int index = (y * Width) + x;
            return GetTiles()[index];
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

        private List<Tile> _destroyedTiles = new List<Tile>(); 

        /// <summary>
        /// Destroys the given tile.  Meant to be called on tiles in the collision layer only; 
        /// the underlying Block layer will be automatically updated to reflect the changes.
        /// </summary>
        internal void DestroyTile(Tile tile) {
            Layer blockLayer = _map.Layers["Blocks"];

            int x = (int) tile.Position.X;
            int y = (int) tile.Position.Y;
            int index = y * Width + x;
            Tile blockLayerTile = blockLayer.GetTile(x, y);
            if ( blockLayerTile != null ) {
                blockLayerTile.Disposed = true;
                blockLayerTile.TimeUntilReappear = tile.TimeUntilReappear;
            }

            _destroyedTiles.Add(tile);
        }

        /// <summary>
        /// Revives the given tile.  Meant to be called on tiles in the collision layer only; 
        /// the underlying Block layer will be automatically updated.
        /// </summary>
        /// <param name="tile"></param>
        internal void ReviveTile(Tile tile) {
            Layer blockLayer = _map.Layers["Blocks"];

            int x = (int) tile.Position.X;
            int y = (int) tile.Position.Y;
            int index = y * Width + x;
            Tile blockLayerTile = blockLayer.GetTile(x, y);
            if ( blockLayerTile != null ) {
                blockLayerTile.Disposed = false;
            }
        }

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

        public void Draw(SpriteBatch batch, IList<Tileset> tilesets, Rectangle visibleWorld) {

            int minx = Math.Max(visibleWorld.Left - 1, 0);
            int miny = Math.Max(visibleWorld.Top - 1, 0);
            int maxx = Math.Min(visibleWorld.Right + 1, Width);
            int maxy = Math.Min(visibleWorld.Bottom + 1, Height);

            for ( int y = miny; y <= maxy; y++ ) {
                for ( int x = minx; x <= maxx; x++ ) {
                    var tile = GetTile(x, y);
                    if ( tile != null )
                        tile.Draw(batch);
                }
            }
        }

        internal IDictionary<Tile, int> deadTiles = new Dictionary<Tile, int>(); 

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

            _destroyedTiles.RemoveAll(tile => !tile.Disposed);
        } 
    }

    public struct TileInfo {
        public Texture2D Texture;
        public Rectangle Rectangle;
    }

    public class Tile : IEquatable<Tile> {
        public Vector2 Position { get; private set; }

        // TODO: share this data
        public readonly IList<Body> Bodies = new List<Body>();
        public ISet<Tile> Group;
        private readonly int _tileInfoIndex;

        private Layer Layer { get; set; }
        public bool Disposed { get; internal set; }
        public int TimeUntilReappear { get; internal set; }

        public Tile(Layer layer, Vector2 position, int tileInfoIndex) {
            Position = position;
            Layer = layer;
            _tileInfoIndex = tileInfoIndex;
        }

        public Tile GetLeftTile() {
            return Layer.GetLeftTile(this);
        }

        public Tile GetUpTile() {
            return Layer.GetUpTile(this);
        }

        public Tile GetRightTile() {
            return Layer.GetRightTile(this);
        }

        public Tile GetDownTile() {
            return Layer.GetDownTile(this);
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

            TimeUntilReappear = BlockTimeUntilReappear;
            DestroyAttachedBodies();
            Disposed = true;

            Layer.DestroyTile(this);
        }

        /// <summary>
        /// Destroys all Box2d bodies attached to this tile's group.
        /// </summary>
        public void DestroyAttachedBodies() {
            foreach ( Body body in Bodies ) {
                Console.WriteLine("Disposing body with id {0} ", RuntimeHelpers.GetHashCode(body));
                body.Dispose();
            }
            Bodies.Clear();            

            foreach ( Tile t in Group ) {
                t.Bodies.Clear();
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
            TileLevel.CurrentLevel.ReviveTile(this);
            
            Layer.ReviveTile(this);
        }

        public void Draw(SpriteBatch batch) {
                var tileCorner = new Vector2(Position.X - 1f / 2f,
                                             Position.Y - 1f / 2f);
                Vector2 displayPosition = new Vector2();
                ConvertUnits.ToDisplayUnits(ref tileCorner, out displayPosition);
                TileInfo tileInfo = Layer._map.GetTileInfoCache()[_tileInfoIndex];
            if ( !Disposed ) {
                batch.Draw(tileInfo.Texture, displayPosition, tileInfo.Rectangle, Color.White);
            } else if (TimeUntilReappear <= BlockTimeUntilReappear / 4) {
                float alpha = 1 - TimeUntilReappear / (float) (BlockTimeUntilReappear / 4);
                batch.Draw(tileInfo.Texture, displayPosition, tileInfo.Rectangle, Color.White * alpha);
            }
        }

        public void Update(GameTime gameTime) {
            if ( Disposed ) {
                TimeUntilReappear -= gameTime.ElapsedGameTime.Milliseconds;
                if ( TimeUntilReappear <= 0 ) {
                    Revive();
                }
                //Console.WriteLine("Updating tile with game time {0}", gameTime.ElapsedGameTime.Milliseconds);
            }
        }

        static readonly private int BlockTimeUntilReappear = 2000;
    }

    public class ObjectGroup {
        public SortedList<string, Object> Objects = new SortedList<string, Object>();
        public SortedList<string, string> Properties = new SortedList<string, string>();

        public string Name;
        public int Width, Height, X, Y;
        float Opacity = 1;

        internal static ObjectGroup Load(XmlReader reader) {
            var result = new ObjectGroup();

            if ( reader.GetAttribute("name") != null )
                result.Name = reader.GetAttribute("name");
            if ( reader.GetAttribute("width") != null )
                result.Width = int.Parse(reader.GetAttribute("width"));
            if ( reader.GetAttribute("height") != null )
                result.Height = int.Parse(reader.GetAttribute("height"));
            if ( reader.GetAttribute("x") != null )
                result.X = int.Parse(reader.GetAttribute("x"));
            if ( reader.GetAttribute("y") != null )
                result.Y = int.Parse(reader.GetAttribute("y"));
            if ( reader.GetAttribute("opacity") != null )
                result.Opacity = float.Parse(reader.GetAttribute("opacity"));

            while ( !reader.EOF ) {
                var name = reader.Name;

                switch ( reader.NodeType ) {
                    case XmlNodeType.Element:
                        switch ( name ) {
                            case "object": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        st.Read();
                                        var objects = Object.Load(st);
                                        result.Objects.Add(objects.Name, objects);
                                    }
                                } break;
                            case "properties": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        while ( !st.EOF ) {
                                            switch ( st.NodeType ) {
                                                case XmlNodeType.Element:
                                                    if ( st.Name == "property" ) {
                                                        st.Read();
                                                        if ( st.GetAttribute("name") != null ) {
                                                            result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                        }
                                                    }

                                                    break;
                                                case XmlNodeType.EndElement:
                                                    break;
                                            }

                                            st.Read();
                                        }
                                    }
                                } break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        public void Draw(Map result, SpriteBatch batch, Rectangle rectangle, Vector2 viewportPosition) {
            foreach ( var objects in Objects.Values ) {
                if ( objects.Texture != null ) {
                    objects.Draw(batch, rectangle, new Vector2(this.X * result.TileWidth, this.Y * result.TileHeight), viewportPosition, this.Opacity);
                }
            }
        }
    }

    public class Object {
        public SortedList<string, string> Properties = new SortedList<string, string>();

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

        internal static Object Load(XmlReader reader) {
            var result = new Object();

            result.Name = reader.GetAttribute("name");
            result.X = int.Parse(reader.GetAttribute("x"));
            result.Y = int.Parse(reader.GetAttribute("y"));
            result.Width = int.Parse(reader.GetAttribute("width"));
            result.Height = int.Parse(reader.GetAttribute("height"));

            while ( !reader.EOF ) {
                switch ( reader.NodeType ) {
                    case XmlNodeType.Element:
                        if ( reader.Name == "properties" ) {
                            using ( var st = reader.ReadSubtree() ) {
                                while ( !st.EOF ) {
                                    switch ( st.NodeType ) {
                                        case XmlNodeType.Element:
                                            if ( st.Name == "property" ) {
                                                st.Read();
                                                if ( st.GetAttribute("name") != null ) {
                                                    result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                }
                                            }

                                            break;
                                        case XmlNodeType.EndElement:
                                            break;
                                    }

                                    st.Read();
                                }
                            }
                        }
                        if ( reader.Name == "image" ) {
                            result.Image = reader.GetAttribute("source");
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        public void Draw(SpriteBatch batch, Rectangle rectangle, Vector2 offset, Vector2 viewportPosition, float opacity) {
            Vector2 viewPos = viewportPosition;

            int minX = (int) Math.Floor(viewportPosition.X);
            int minY = (int) Math.Floor(viewportPosition.Y);
            int maxX = (int) Math.Ceiling((rectangle.Width + viewportPosition.X));
            int maxY = (int) Math.Ceiling((rectangle.Height + viewportPosition.Y));

            if ( this.X + offset.X + this.Width > minX && this.X + offset.X < maxX )
                if ( this.Y + offset.Y + this.Height > minY && this.Y + offset.Y < maxY ) {
                    int x = (int) (this.X + offset.X - viewportPosition.X);
                    int y = (int) (this.Y + offset.Y - viewportPosition.Y);
                    //batch.Draw(_Texture, new Rectangle(x, y, this.Width, this.Height), new Rectangle(0, 0, _Texture.Width, _Texture.Height), new Color(Color.White, opacity));
                }
        }
    }

    public class Map {
        public SortedList<string, Tileset> Tilesets = new SortedList<string, Tileset>();
        public SortedList<string, Layer> Layers = new SortedList<string, Layer>();
        public SortedList<string, ObjectGroup> ObjectGroups = new SortedList<string, ObjectGroup>();
        public SortedList<string, string> Properties = new SortedList<string, string>();
        public int Width, Height;
        public int TileWidth, TileHeight;
        private TileInfo[] _tileInfoCache = null;

        public TileInfo[] GetTileInfoCache() {
            if ( _tileInfoCache == null ) {
                _tileInfoCache = BuildTileInfoCache(Tilesets.Values);
            }
            return _tileInfoCache;
        }

        protected TileInfo[] BuildTileInfoCache(IList<Tileset> tilesets) {
            Rectangle rect = new Rectangle();
            var cache = new List<TileInfo>();
            int i = 1;

        next:
            for ( int t = 0; t < tilesets.Count; t++ ) {
                if ( tilesets[t].MapTileToRect(i, ref rect) ) {
                    cache.Add(new TileInfo {
                        Texture = tilesets[t].Texture,
                        Rectangle = rect
                    });
                    i += 1;
                    goto next;
                }
            }

            return cache.ToArray();
        }

        public static Map Load(string filename, ContentManager content) {
            var result = new Map();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;

            using ( var stream = System.IO.File.OpenText(filename) )
            using ( var reader = XmlReader.Create(stream, settings) )
                while ( reader.Read() ) {
                    var name = reader.Name;

                    switch ( reader.NodeType ) {
                        case XmlNodeType.DocumentType:
                            if ( name != "map" )
                                throw new Exception("Invalid map format");
                            break;
                        case XmlNodeType.Element:
                            switch ( name ) {
                                case "map": {
                                        result.Width = int.Parse(reader.GetAttribute("width"));
                                        result.Height = int.Parse(reader.GetAttribute("height"));
                                        result.TileWidth = int.Parse(reader.GetAttribute("tilewidth"));
                                        result.TileHeight = int.Parse(reader.GetAttribute("tileheight"));
                                    } break;
                                case "tileset": {
                                        using ( var st = reader.ReadSubtree() ) {
                                            st.Read();
                                            var tileset = Tileset.Load(st);
                                            result.Tilesets.Add(tileset.Name, tileset);
                                        }
                                    } break;
                                case "layer": {
                                        using ( var st = reader.ReadSubtree() ) {
                                            st.Read();
                                            var layer = Layer.Load(result, st);
                                            result.Layers.Add(layer.Name, layer);
                                        }
                                    } break;
                                case "objectgroup": {
                                        using ( var st = reader.ReadSubtree() ) {
                                            st.Read();
                                            var objectgroup = ObjectGroup.Load(st);
                                            result.ObjectGroups.Add(objectgroup.Name, objectgroup);
                                        }
                                    } break;
                                case "properties": {
                                        using ( var st = reader.ReadSubtree() ) {
                                            while ( !st.EOF ) {
                                                switch ( st.NodeType ) {
                                                    case XmlNodeType.Element:
                                                        if ( st.Name == "property" ) {
                                                            st.Read();
                                                            if ( st.GetAttribute("name") != null ) {
                                                                result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                            }
                                                        }

                                                        break;
                                                    case XmlNodeType.EndElement:
                                                        break;
                                                }

                                                st.Read();
                                            }
                                        }
                                    } break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            break;
                        case XmlNodeType.Whitespace:
                            break;
                    }
                }

            foreach ( var tileset in result.Tilesets.Values ) {
                // TODO: fix this bug
                tileset.Texture = content.Load<Texture2D>(
                    Path.Combine("Maps", Path.GetDirectoryName(tileset.Image), Path.GetFileNameWithoutExtension(tileset.Image))
                );
            }

            foreach ( var objects in result.ObjectGroups.Values ) {
                foreach ( var item in objects.Objects.Values ) {
                    if ( item.Image != null ) {
                        item.Texture = content.Load<Texture2D>
                        (
                            Path.Combine
                            (
                                Path.GetDirectoryName(item.Image),
                                Path.GetFileNameWithoutExtension(item.Image)
                            )
                        );
                    }
                }
            }

            return result;
        }

        public void Draw(SpriteBatch batch, Rectangle visibleWorld) {
            foreach ( Layer layer in Layers.Values.Where(layer => !layer.Properties.ContainsKey("invisible")) ) {
                layer.Draw(batch, Tilesets.Values, visibleWorld);
            }

//            foreach ( var objectGroup in ObjectGroups.Values ) {
//                objectGroup.Draw(this, batch, visibleWorld, viewportPosition);
//            }
        }

        public void Update(GameTime gameTime) {
            foreach ( Layer layer in Layers.Values ) {
                layer.Update(gameTime);
            }
        }
    }
}

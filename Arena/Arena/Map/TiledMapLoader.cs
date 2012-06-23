/*
 * Partial classes that implement the loading logic for Tiled map entities.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Map {

    partial class Tileset {

        internal static Tileset Load(XmlReader reader) {
            var result = new Tileset();

            result.Name = reader.GetAttribute("name");
            result.FirstTileId = int.Parse(reader.GetAttribute("firstgid"));
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
                                Tileset.TilePropertyList props;
                                if ( !result.TileProperties.TryGetValue(currentTileId, out props) ) {
                                    props = new Tileset.TilePropertyList();
                                    result.TileProperties[currentTileId] = props;
                                }

                                props[reader.GetAttribute("name")] = reader.GetAttribute("value");
                            }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return result;
        }

    }

    public partial class Layer {
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
    }

    public partial class ObjectGroup {
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
                                    var obj = Object.Load(st);
                                    result.Objects.Add(obj);
                                }
                            }
                                break;
                            case "properties": {
                                using ( var st = reader.ReadSubtree() ) {
                                    while ( !st.EOF ) {
                                        switch ( st.NodeType ) {
                                            case XmlNodeType.Element:
                                                if ( st.Name == "property" ) {
                                                    st.Read();
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
    }

    public partial class Object {
        internal static Object Load(XmlReader reader) {
            var result = new Object();

            result.Name = reader.GetAttribute("name");
            result.X = int.Parse(reader.GetAttribute("x"));
            result.Y = int.Parse(reader.GetAttribute("y"));

            int width;
            if ( int.TryParse(reader.GetAttribute("width"), out width) ) {
                result.Width = width;
            }
            int height;
            if ( int.TryParse(reader.GetAttribute("height"), out height) ) {
                result.Height = height;
            }

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

    }

    public partial class Map {
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
                                }
                                    break;
                                case "tileset": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        st.Read();
                                        var tileset = Tileset.Load(st);
                                        result.Tilesets.Add(tileset.Name, tileset);
                                    }
                                }
                                    break;
                                case "layer": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        st.Read();
                                        var layer = Layer.Load(result, st);
                                        result.Layers.Add(layer.Name, layer);
                                    }
                                }
                                    break;
                                case "objectgroup": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        st.Read();
                                        var objectgroup = ObjectGroup.Load(st);
                                        result.ObjectGroups.Add(objectgroup.Name, objectgroup);
                                    }
                                }
                                    break;
                                case "properties": {
                                    using ( var st = reader.ReadSubtree() ) {
                                        while ( !st.EOF ) {
                                            switch ( st.NodeType ) {
                                                case XmlNodeType.Element:
                                                    if ( st.Name == "property" ) {
                                                        st.Read();
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
                        case XmlNodeType.Whitespace:
                            break;
                    }
                }

            foreach ( var tileset in result.Tilesets.Values ) {
                // TODO: fix this bug
                tileset.Texture = content.Load<Texture2D>(
                    Path.Combine("Maps", Path.GetDirectoryName(tileset.Image),
                                 Path.GetFileNameWithoutExtension(tileset.Image))
                    );
            }

            foreach ( var objects in result.ObjectGroups.Values ) {
                foreach ( var item in objects.Objects ) {
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

    }
}

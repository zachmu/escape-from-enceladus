using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Test {
    internal class Level {
        private Tile[] Tiles { get; set; }
        private int Height { get; set; }
        private int Width { get; set; }

        public Level(ContentManager cm, Texture2D source, World world) {
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
                        var position = new Vector2(x, y);
                        Tiles[index] = new LandTile(position);
                    } else {
                        Tiles[index] = new BlankTile(new Vector2(x, y));
                    }
                }
            }

            InitializeEdges(world);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            for ( int y = 0; y < Height; y++ ) {
                for ( int x = 0; x < Width; x++ ) {
                    int index = y * Width + x;
                    Tiles[index].Draw(spriteBatch, camera);
                }
            }
        }

        /// <summary>
        /// Analyzes and stores the edges of the tiles, to be used by the physics engine.
        /// </summary>
        /// <param name="world"> </param>
        public void InitializeEdges(World world) {
            // First we break the tiles down into connected groups.  Each one can be represented by one representative tile, usually its upper left.
            HashSet<Tile> tileGroups = FindGroups();

            Console.Out.WriteLine("Found {0} groups of tiles", tileGroups.Count);

            CreateEdges(tileGroups, world);

//            foreach ( Tile tile in Tiles.Where(tile => tile.IsSolid()) ) {
//                Body rectangle = BodyFactory.CreateRectangle(world, 1f, 1f, 1f);
//                rectangle.IsStatic = true;
//                rectangle.Restitution = 0.2f;
//                rectangle.Friction = 0.2f;
//                rectangle.Position = tile._position;
//            }
        }

        /// <summary>
        /// Creates world edges for the groups of tiles given
        /// </summary>
        private void CreateEdges(IEnumerable<Tile> tileGroups, World world) {
            // Alternate coordinate system: 0,0 refers to the upper left corner of the upper-left tile,
            // 1,1 the bottom-right corner of that same tile.  Thus a tile's four corners are defined by:
            // Upper-left: x, y
            // Upper-right: x+1, y
            // Bottom-right: x+1, y+1
            // Bottom-left: x, y+1

            foreach ( Tile group in tileGroups ) {

                Edges edges = new Edges();
                ISet<Tile> seenTiles = new HashSet<Tile>();
                seenTiles.Add(group);
                FindConnectedTiles(group, seenTiles);

                foreach ( Tile tile in seenTiles ) {

                    Tile left = GetLeftTile(tile);
                    if ( left != null && !left.IsSolid() ) {
                        edges.Add(new Edge(tile._position, tile._position + new Vector2(0, 1)));
                    }

                    Tile up = GetUpTile(tile);
                    if ( up != null && !up.IsSolid() ) {
                        edges.Add(new Edge(tile._position, tile._position + new Vector2(1, 0)));
                    }

                    Tile right = GetRightTile(tile);
                    if ( right != null && !right.IsSolid() ) {
                        edges.Add(new Edge(tile._position + new Vector2(1, 0), tile._position + new Vector2(1, 1)));
                    }

                    Tile down = GetDownTile(tile);
                    if ( down != null && !down.IsSolid() ) {
                        edges.Add(new Edge(tile._position + new Vector2(0, 1), tile._position + new Vector2(1, 1)));
                    }

                }

                Vector2 initialVertex = edges.GetInitialVertex();
                Vector2 currentVertex = initialVertex;
                // Pick a random edge to start walking.  This determines the handedness of the loop we will build
                Edge currentEdge = edges.GetEdgesWithVertex(currentVertex).First();
                Vertices chain = new Vertices();
                Vector2 offset = new Vector2(-.5f, -.5f); // offset to account for different in position v. edge
                // work our way through the vertices, linking them together into a chain
                do {
                    chain.Add(currentVertex + offset);
                    currentVertex = currentEdge.GetOtherVertex(currentVertex);
                    foreach ( Edge edge in edges.GetEdgesWithVertex(currentVertex) ) {
                        if ( edge != currentEdge ) {
                            currentEdge = edge;
                            break;
                        }
                    }
                } while ( currentVertex != initialVertex );

                BodyFactory.CreateLoopShape(world, chain);
            }
        }

        /// <summary>
        /// Collection of edges with vertex querying capability
        /// </summary>
        internal class Edges {
            private IDictionary<Vector2, ICollection<Edge>> edgesByVertex = new Dictionary<Vector2, ICollection<Edge>>();

            public void Add(Edge edge) {
                foreach (Vector2 endpoint in new Vector2[] { edge.One, edge.Two }) {
                    if (!edgesByVertex.ContainsKey(endpoint)) {
                        edgesByVertex.Add(endpoint, new HashSet<Edge>());
                    }
                    edgesByVertex[endpoint].Add(edge);
                }
            }

            public bool ContainsVertex(Vector2 vertex) {
                return edgesByVertex.ContainsKey(vertex);
            }

            public ICollection<Edge> GetEdgesWithVertex(Vector2 vertex) {
                return edgesByVertex[vertex];
            }

            public Vector2 GetInitialVertex() {
                return edgesByVertex.Keys.First();
            }
        }

        /// <summary>
        /// Just a simple line segment.
        /// </summary>
        internal class Edge : IEquatable<Edge> {
            internal Vector2 One { get; private set; }
            internal Vector2 Two { get; private set; }

            public override string ToString() {
                return string.Format("One: {0}, Two: {1}", One, Two);
            }

            public Edge(Vector2 one, Vector2 two) {
                One = one;
                Two = two;
            }

            public Vector2 GetOtherVertex(Vector2 vertex) {
                if (vertex == One)
                    return Two;
                if (vertex == Two)
                    return One;
                throw new Exception("No matching vertex found for vertex {0} in edge {1}");
            }

            public bool Equals(Edge other) {
                if ( ReferenceEquals(null, other) ) {
                    return false;
                }
                if ( ReferenceEquals(this, other) ) {
                    return true;
                }
                return (other.One.Equals(One) && other.Two.Equals(Two))
                    || (other.One.Equals(Two) && other.Two.Equals(One));
            }

            public override bool Equals(object obj) {
                if ( ReferenceEquals(null, obj) ) {
                    return false;
                }
                if ( ReferenceEquals(this, obj) ) {
                    return true;
                }
                if ( obj.GetType() != typeof ( Edge ) ) {
                    return false;
                }
                return Equals((Edge) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return One.GetHashCode() ^ Two.GetHashCode();
                }
            }

            public static bool operator ==(Edge left, Edge right) {
                return Equals(left, right);
            }

            public static bool operator !=(Edge left, Edge right) {
                return !Equals(left, right);
            }
        }

        private HashSet<Tile> FindGroups() {
            var groups = new HashSet<Tile>();
            var seenTiles = new HashSet<Tile>();

            foreach ( Tile tile in Tiles.Where(tile => tile.IsSolid()) ) {
                if ( !seenTiles.Contains(tile) ) {
                    if ( !groups.Contains(tile) ) {
                        groups.Add(tile);
                        FindConnectedTiles(tile, seenTiles);
                    }
                }
            }

            return groups;
        }

        /// <summary>
        /// Finds all tiles that are connected to this one.
        /// </summary>
        private void FindConnectedTiles(Tile tile, ISet<Tile> seenTiles) {
            seenTiles.Add(tile);
            foreach ( Tile connected in FindAdjacentSolidTiles(tile) ) {
                if ( !seenTiles.Contains(connected) ) {
                    FindConnectedTiles(connected, seenTiles);
                }
            }
        }

        /// <summary>
        /// Returns the 0-4 adjacent solid tiles connected to this one.
        /// </summary>
        private IEnumerable<Tile> FindAdjacentSolidTiles(Tile tile) {
            return new Tile[] {GetLeftTile(tile), GetUpTile(tile), GetRightTile(tile), GetDownTile(tile)}.Where(adj => adj != null && adj.IsSolid()).ToList();
        }

        private Tile GetDownTile(Tile tile) {
            Tile downTile = null;
            if ( (int) tile._position.Y < Height - 1 ) {
                int down = ((int) tile._position.Y + 1) * Width + (int) tile._position.X;
                downTile = Tiles[down];
            }
            return downTile;
        }

        private Tile GetRightTile(Tile tile) {
            Tile rightTile = null;
            if ( (int) tile._position.X < Width - 1 ) {
                int right = (int) tile._position.Y * Width + (int) tile._position.X + 1;
                rightTile = Tiles[right];
            }
            return rightTile;
        }

        private Tile GetUpTile(Tile tile) {
            Tile upTile = null;
            if ( (int) tile._position.Y > 0 ) {
                int up = ((int) tile._position.Y - 1) * Width + (int) tile._position.X;
                upTile = Tiles[up];
            }
            return upTile;
        }

        private Tile GetLeftTile(Tile tile) {
            Tile leftTile = null;
            if ( (int) tile._position.X > 0 ) {
                int left = (int) tile._position.Y * Width + (int) tile._position.X - 1;
                leftTile = Tiles[left];
            }
            return leftTile;
        }

        private abstract class Tile {
            internal const float TILE_SIZE = 1f;
            internal const int TILE_DISPLAY_SIZE = 32;
            internal abstract void Draw(SpriteBatch spriteBatch, Camera2D camera);
            internal abstract Boolean IsSolid();
            internal Vector2 _position;
        }

        private class BlankTile : Tile {

            public static Texture2D Image { get; set; }

            private Vector2[] stars = new Vector2[3];

            private static Random r = new Random();

            public BlankTile(Vector2 position) {
                _position = position;

                for ( int i = 0; i < stars.Length; i++ ) {
                    stars[i] = new Vector2((float) (r.NextDouble() * (double) TILE_DISPLAY_SIZE),
                                           (float) (r.NextDouble() * (double) TILE_DISPLAY_SIZE));
                }
            }

            internal override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
                var tileCorner = new Vector2(_position.X * TILE_SIZE - TILE_SIZE / 2f,
                                             _position.Y * TILE_SIZE - TILE_SIZE / 2f);
                var displayUnits = new Vector2();
                ConvertUnits.ToDisplayUnits(ref tileCorner, out displayUnits);
                foreach ( Vector2 star in stars ) {
                    spriteBatch.Draw(Image, displayUnits + star, Color.White);
                }
            }

            internal override bool IsSolid() {
                return false;
            }
        }

        private class LandTile : Tile {

            public static Texture2D Image { get; set; }

            public LandTile(Vector2 position) {
                _position = position;
            }

            internal override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
                var position = new Vector2(_position.X * TILE_SIZE - TILE_SIZE / 2f,
                                           _position.Y * TILE_SIZE - TILE_SIZE / 2f);
                Vector2 displayPosition = new Vector2();
                ConvertUnits.ToDisplayUnits(ref position, out displayPosition);
                spriteBatch.Draw(Image, displayPosition, Color.White);
            }

            internal override bool IsSolid() {
                return true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Arena {

    public static class VerticesExtensions {

        /// <summary>
        /// Returns the circular index for the integer given.
        /// </summary>
        public static int GetCircularIndex<T>(this List<T> v, int i) {
            if ( i >= 0 && i < v.Count ) {
                return i;
            } else if ( i < 0 ) {
                return v.Count + i;
            } else {
                return i % v.Count;
            }
        }
    }

    public class Level {
        private Tile[] Tiles { get; set; }
        private int Height { get; set; }
        private int Width { get; set; }
        private IList<Body> _loops = new List<Body>();
        private SpriteFont _debugFont;

        internal const float TileSize = 1f;
        internal const int TileDisplaySize = 32;

        public static Level CurrentLevel { get; private set; }

        public Level(ContentManager cm, Texture2D source, World world) {
            this.Height = source.Height;
            this.Width = source.Width;

            Color[] data = new Color[Height * Width];
            source.GetData<Color>(data);
            Tiles = new Tile[data.Length];

            Texture2D texture = cm.Load<Texture2D>("met_tile");
            LandTile.Image = texture;
            BlankTile.Image = cm.Load<Texture2D>("star");
            _debugFont = cm.Load<SpriteFont>("debugFont");

            for ( int y = 0; y < Height; y++ ) {
                for ( int x = 0; x < Width; x++ ) {
                    int index = y * Width + x;
                    if ( data[index] == Color.Black ) {
                        Tiles[index] = new LandTile(new Vector2(x, y));
                    } else {
                        Tiles[index] = new BlankTile(new Vector2(x, y));
                    }
                }
            }

            InitializeEdges(world);
            CurrentLevel = this;
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
            // First we break the tiles down into connected groups.
            Console.WriteLine("Finding groups");
            FindGroups();

            // Then create edges using these groups
            Console.WriteLine("Creating edges");
            CreateEdges(world);

            //            foreach ( Tile tile in Tiles.Where(tile => tile.IsSolid()) ) {
            //                Body rectangle = BodyFactory.CreateRectangle(world, 1f, 1f, 1f);
            //                rectangle.IsStatic = true;
            //                rectangle.Restitution = 0.2f;
            //                rectangle.Friction = 0.2f;
            //                rectangle.Position = tile._position;
            //            }
        }

        /// <summary>
        /// Gets the tile located at the sim location given, having the collision normal given.
        /// </summary>
        internal Tile GetTile(Vector2 location, Vector2 normal) {

            // Nudge the location of the collision a bit in the appropriate direction 
            // so that it will be inside the tile that was hit.
            if ( normal.X > 0 ) {
                location += new Vector2(-.1f, 0);
            } else if ( normal.X < 0 ) {
                location += new Vector2(.1f, 0);
            }
            if ( normal.Y > 0 ) {
                location += new Vector2(0, -.1f);
            } else if ( normal.Y < 0 ) {
                location += new Vector2(0, .1f);
            }

            Console.WriteLine("Evaluating tile collision at {0}", location);

            int x = (int) location.X;
            int y = (int) location.Y;
            int idx = y * Width + x;
            if ( idx > Tiles.Length ) {
                throw new ArgumentOutOfRangeException("Calculated an index of " + idx);
            }

            Tile tile = Tiles[idx];
            Console.WriteLine("Found the tile at {0}", tile.Position);

            // Assume that this tile, or the one of its neighbors, is the cause of the collision
            Tile leftTile = GetLeftTile(tile);
            Tile rightTile = GetRightTile(tile);
            IList<Tile> candidates = new List<Tile>() { tile, GetUpTile(tile), GetDownTile(tile), 
                leftTile, GetUpTile(leftTile), GetDownTile(leftTile), 
                rightTile, GetUpTile(rightTile), GetDownTile(rightTile) };           

            foreach ( Tile t in candidates.Where(cand => cand != null && cand.IsSolid()) ) {
                if ( t != null ) {
                    Vector2 upperleft = t.Position - new Vector2(TileSize / 2);
                    Vector2 lowerRight = t.Position + new Vector2(TileSize / 2);
                    Console.WriteLine("Evaluating tile with bounds {0},{1}", upperleft, lowerRight);
                    if ( location.X >= upperleft.X && location.X <= lowerRight.X && location.Y >= upperleft.Y &&
                         location.Y <= lowerRight.Y ) {
                         return t;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates world edges for all tiles
        /// </summary>
        private void CreateEdges(World world) {
            // Alternate coordinate system: 0,0 refers to the upper left corner of the upper-left tile,
            // 1,1 the bottom-right corner of that same tile.  Thus a tile's four corners are defined by:
            // Upper-left: x, y
            // Upper-right: x+1, y
            // Bottom-right: x+1, y+1
            // Bottom-left: x, y+1

            HashSet<Tile> seen = new HashSet<Tile>();

            foreach ( Tile tile in Tiles.Where(tile => tile.IsSolid()) ) {
                if ( !seen.Contains(tile) ) {
                    seen.UnionWith(tile.Group);
                    CreateEdges(world, tile);
                }
            }
        }

        /// <summary>
        /// Creates the edges for the group containing the tile given.  Group membership must already be established.
        /// </summary>
        private void CreateEdges(World world, Tile tile) {
            Edges edges = new Edges();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach ( Tile t in tile.Group ) {
                Tile left = GetLeftTile(t);
                if ( left != null && !left.IsSolid() ) {
                    edges.Add(GetLeftEdge(t));
                }

                Tile up = GetUpTile(t);
                if ( up != null && !up.IsSolid() ) {
                    edges.Add(GetTopEdge(t));
                }

                Tile right = GetRightTile(t);
                if ( right != null && !right.IsSolid() ) {
                    edges.Add(GetRightEdge(t));
                }

                Tile down = GetDownTile(t);
                if ( down != null && !down.IsSolid() ) {
                    edges.Add(GetBottomEdge(t));
                }
            }
            watch.Stop();
            Console.WriteLine("Creating edges object took {0} ticks", watch.ElapsedTicks);
            watch.Reset();

            watch.Start();
            while ( edges.HasEdges() ) {
                Vector2 initialVertex = edges.GetInitialVertex();
                Vector2 currentVertex = initialVertex;
                // Pick a random edge to start walking.  This determines the handedness of the loop we will build
                Edge currentEdge = edges.GetEdgesWithVertex(currentVertex).First();
                Edge prevEdge = null;
                Vertices chain = new Vertices();
                IList<Vector2> processedVertices = new List<Vector2>();
                Vector2 offset = new Vector2(-TileSize / 2); // offset to account for different in position v. edge

                // work our way through the vertices, linking them together into a chain
                do {
                    processedVertices.Add(currentVertex);

                    // only add vertices that aren't colinear with our current edge
                    if ( prevEdge == null || !AreEdgesColinear(prevEdge, currentEdge) ) {
                        chain.Add(currentVertex + offset);
                    }
                    currentVertex = currentEdge.GetOtherVertex(currentVertex);
                    foreach ( Edge edge in edges.GetEdgesWithVertex(currentVertex) ) {
                        if ( edge != currentEdge ) {
                            prevEdge = currentEdge;
                            currentEdge = edge;
                            break;
                        }
                    }
                } while ( currentVertex != initialVertex );

                Stopwatch factoryWatch = new Stopwatch();
                factoryWatch.Start();
                Body loopShape = BodyFactory.CreateLoopShape(world, chain);
                factoryWatch.Stop();
                Console.WriteLine("Body factory took {0} ticks", factoryWatch.ElapsedTicks);

                foreach ( Tile t in tile.Group ) {
                    t.Bodies.Add(loopShape);
                }

                edges.Remove(processedVertices);
            }
            watch.Stop();
            Console.WriteLine("Processing edges into shape took {0} ticks", watch.ElapsedTicks);
        }

        private bool AreEdgesColinear(Edge prevEdge, Edge currentEdge) {
            return (currentEdge.One.X == prevEdge.One.X
                    && currentEdge.Two.X == prevEdge.Two.X)
                   || (currentEdge.One.Y == prevEdge.One.Y
                       && currentEdge.Two.Y == prevEdge.Two.Y);
        }

        private static Edge GetBottomEdge(Tile tile) {
            return new Edge(tile.Position + new Vector2(0, 1), tile.Position + new Vector2(1, 1));
        }

        private static Edge GetRightEdge(Tile tile) {
            return new Edge(tile.Position + new Vector2(1, 0), tile.Position + new Vector2(1, 1));
        }

        private static Edge GetTopEdge(Tile tile) {
            return new Edge(tile.Position, tile.Position + new Vector2(1, 0));
        }

        private static Edge GetLeftEdge(Tile tile) {
            return new Edge(tile.Position, tile.Position + new Vector2(0, 1));
        }

        /// <summary>
        /// Destroys this tile
        /// </summary>
        public void DestroyTile(World world, Tile t) {
            //                Edge top = GetTopEdge(this);
            //                Edge bottom = GetBottomEdge(this);
            //                Edge left = GetLeftEdge(this);
            //                Edge right = GetRightEdge(this);
            //
            //                foreach ( Body body in Level.CurrentLevel._loops ) {
            //                    foreach ( Fixture fix in body.FixtureList ) {
            //                        LoopShape shape = (LoopShape) fix.Shape;
            //                        // Find the edges that are colinear with our bounds
            //                        Vector2 curr = shape.Vertices.First();
            //                        for ( int i = 1; i < shape.Vertices.Count; i++ ) {
            //                            Vector2 vertex = shape.Vertices[i];
            //                            if ( IsColinear(curr, vertex) ) {
            //                                DeformBody(body, shape, shape.Vertices.GetCircularIndex(i - 1));
            //                                return;
            //                            }
            //                            curr = vertex;
            //                        }
            //                    }
            //                }

            t.Dispose();

            IEnumerable<Tile> neighbors = FindAdjacentSolidTiles(t);
            var seenTiles = new HashSet<Tile>();

            foreach ( Tile tile in neighbors ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<Tile> group = new HashSet<Tile>();

                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    FindConnectedTiles(tile, group, seenTiles);
                    watch.Stop();
                    Console.WriteLine("Finding connected tiles took {0} ticks", watch.ElapsedTicks);
                    watch.Reset();

                    watch.Start();
                    CreateEdges(world, tile);
                    watch.Stop();
                    Console.WriteLine("Creating edges took {0} ticks", watch.ElapsedTicks);
                }
            }
        }

        /// <summary>
        /// Deforms the given loop by removing this tile from it.  The edge formed by (startingIndex, startingIndex + 1) is an edge of this tile.
        /// </summary>
        /// <param name="body">The body to deform</param>
        /// <param name="loop">The body's loop shape</param>
        /// <param name="startingIndex">The first index in the vertices list where this tile shares an edge</param>
        //        private void DeformBody(Body body, LoopShape loop, int startingIndex) {
        //            // first we need a copy of the list to deform
        //            List<Vector2> clone = new List<Vector2>(loop.Vertices);
        //
        //            // Now we need to determine which of this tile's edges overlap with the loop.
        //            // This tells us how many links we need to remove from the chain, and which of our edges to insert.
        //            // TODO: ensure clockwise winding order
        //            Edge top = GetTopEdge(this);
        //            Edge bottom = GetBottomEdge(this);
        //            Edge left = GetLeftEdge(this);
        //            Edge right = GetRightEdge(this);
        //            IList<Edge> edgesToInsert = new List<Edge>() { top, right, bottom, left };
        //
        //            List<int> verticesToRemove = new List<int>();
        //
        //            // If this is the start of the chain, unwind until we find the clockwise beginning of this tile
        //            if ( startingIndex == 0 ) {
        //                int i = 0;
        //                Edge e = new Edge(loop.Vertices[i], loop.Vertices[i + 1]);
        //                while ( IsColinear(e) ) {
        //                    i = loop.Vertices.GetCircularIndex(i - 1);
        //                    e = new Edge(loop.Vertices[i], loop.Vertices[i + 1]);
        //                }
        //                DeformBody(body, loop, loop.Vertices.GetCircularIndex(i + 1));
        //            }
        //
        //            // Look at the next four segments of the chain to find this tile
        //            int one = loop.Vertices.GetCircularIndex(startingIndex);
        //            int two = loop.Vertices.GetCircularIndex(startingIndex + 1);
        //            int three = loop.Vertices.GetCircularIndex(startingIndex + 2);
        //            int four = loop.Vertices.GetCircularIndex(startingIndex + 3);
        //            int five = loop.Vertices.GetCircularIndex(startingIndex + 4);
        //            Edge edgeOne = new Edge(loop.Vertices[one], loop.Vertices[two]);
        //            Edge edgeTwo = new Edge(loop.Vertices[two], loop.Vertices[three]);
        //            Edge edgeThree = new Edge(loop.Vertices[three], loop.Vertices[four]);
        //            Edge edgeFour = new Edge(loop.Vertices[four], loop.Vertices[five]);
        //
        //            // Special case: this is the last tile in a loop shape
        //            if ( edgesToInsert.Contains(edgeOne) &&
        //                edgesToInsert.Contains(edgeTwo) &&
        //                edgesToInsert.Contains(edgeThree) &&
        //                edgesToInsert.Contains(edgeFour) ) {
        //                body.Dispose();
        //                return;
        //            }
        //
        //            if ( edgeOne == top ) {
        //                if ( edgeTwo == right ) {
        //                    if ( edgeThree == bottom ) { // this is a right-end piece
        //                        RemoveAndInsert(clone, startingIndex, 4, left);
        //                    } else { // this is a corner
        //                        RemoveAndInsert(clone, startingIndex, 4, left);
        //                    }
        //                } else { // this is part of a floor
        //
        //                }
        //            } else if ( edgeOne == right ) {
        //
        //            } else if ( edgeOne == bottom ) {
        //
        //            } else if ( edgeOne == left ) {
        //
        //            } else {
        //                throw new Exception("No matching edge found");
        //            }
        //
        //            body.Dispose();
        //        }
        //
        //        private void RemoveAndInsert(List<Vector2> chain, int removeIndex, int numToRemove, params Edge[] toInsert) {
        //            for ( int i = 0; i < numToRemove; i++ ) {
        //                chain.RemoveAt(chain.Count > removeIndex ? removeIndex : 0);
        //            }
        //
        //            if ( removeIndex > chain.Count ) {
        //                removeIndex = chain.Count;
        //            }
        //            Vector2 curr = toInsert[0].One;
        //            chain.Insert(removeIndex, curr);
        //            int offset = 1;
        //            foreach ( Edge e in toInsert ) {
        //                curr = e.GetOtherVertex(curr);
        //                chain.Insert(removeIndex + offset++, curr);
        //            }
        //        }

        /// <summary>
        /// Collection of edges with vertex querying capability
        /// </summary>
        internal class Edges {
            private readonly IDictionary<Vector2, ICollection<Edge>> _edgesByVertex =
                new Dictionary<Vector2, ICollection<Edge>>();

            public void Add(Edge edge) {
                foreach ( Vector2 endpoint in new Vector2[] { edge.One, edge.Two } ) {
                    if ( !_edgesByVertex.ContainsKey(endpoint) ) {
                        _edgesByVertex.Add(endpoint, new HashSet<Edge>());
                    }
                    _edgesByVertex[endpoint].Add(edge);
                }
            }

            public bool ContainsVertex(Vector2 vertex) {
                return _edgesByVertex.ContainsKey(vertex);
            }

            public ICollection<Edge> GetEdgesWithVertex(Vector2 vertex) {
                return _edgesByVertex[vertex];
            }

            public Vector2 GetInitialVertex() {
                return _edgesByVertex.Keys.First();
            }

            public bool HasEdges() {
                return _edgesByVertex.Count > 0;
            }

            public void Remove(IList<Vector2> chain) {
                foreach (Vector2 v in chain) {
                    _edgesByVertex.Remove(v);
                }
            }
        }

        /// <summary>
        /// Just a simple line segment.
        /// </summary>
        public class Edge : IEquatable<Edge> {
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
                if ( vertex == One )
                    return Two;
                if ( vertex == Two )
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
                if ( obj.GetType() != typeof(Edge) ) {
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

        /// <summary>
        /// Establishes connectedness groups for all tiles in the level.
        /// </summary>
        private void FindGroups() {
            var seenTiles = new HashSet<Tile>();

            foreach ( Tile tile in Tiles.Where(tile => tile.IsSolid()) ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<Tile> group = new HashSet<Tile>();
                    FindConnectedTiles(tile, group, seenTiles);
                }
            }
        }

        /// <summary>
        /// Finds all tiles that are connected to this one, setting its membership to be the group given.
        /// </summary>
        internal void FindConnectedTiles(Tile tile, ISet<Tile> group, ISet<Tile> seenTiles) {
            //Console.WriteLine("Adding tile {0} to group {1}", tile.Position, group);

            group.Add(tile);
            seenTiles.Add(tile);
            tile.Group = group;
            foreach ( Tile connected in FindAdjacentSolidTiles(tile) ) {
                if ( !seenTiles.Contains(connected) ) {
                    FindConnectedTiles(connected, group, seenTiles);
                }
            }
        }

        /// <summary>
        /// Returns the 0-4 adjacent solid tiles connected to this one.
        /// </summary>
        internal IEnumerable<Tile> FindAdjacentSolidTiles(Tile tile) {
            return new Tile[] { GetLeftTile(tile), GetUpTile(tile), GetRightTile(tile), GetDownTile(tile) }.Where(adj => adj != null && adj.IsSolid()).ToList();
        }

        internal Tile GetDownTile(Tile tile) {
            if ( tile == null ) return null;
            Tile downTile = null;
            if ( (int) tile.Position.Y < Height - 1 ) {
                int down = ((int) tile.Position.Y + 1) * Width + (int) tile.Position.X;
                downTile = Tiles[down];
            }
            return downTile;
        }

        internal Tile GetRightTile(Tile tile) {
            if ( tile == null ) return null;
            Tile rightTile = null;
            if ( (int) tile.Position.X < Width - 1 ) {
                int right = (int) tile.Position.Y * Width + (int) tile.Position.X + 1;
                rightTile = Tiles[right];
            }
            return rightTile;
        }

        internal Tile GetUpTile(Tile tile) {
            if ( tile == null ) return null;
            Tile upTile = null;
            if ( (int) tile.Position.Y > 0 ) {
                int up = ((int) tile.Position.Y - 1) * Width + (int) tile.Position.X;
                upTile = Tiles[up];
            }
            return upTile;
        }

        internal Tile GetLeftTile(Tile tile) {
            if ( tile == null ) return null;
            Tile leftTile = null;
            if ( (int) tile.Position.X > 0 ) {
                int left = (int) tile.Position.Y * Width + (int) tile.Position.X - 1;
                leftTile = Tiles[left];
            }
            return leftTile;
        }

        public abstract class Tile {
            internal abstract void Draw(SpriteBatch spriteBatch, Camera2D camera);
            internal abstract bool IsSolid();
            internal abstract void Dispose();
            internal abstract bool IsDisposed();
            internal Vector2 Position;
            internal IList<Body> Bodies = new List<Body>();
            internal ISet<Tile> Group;
        }

        private class BlankTile : Tile {

            public static Texture2D Image { get; set; }

            private readonly Vector2[] _stars = new Vector2[3];

            private static Random r = new Random();

            public BlankTile(Vector2 position) {
                Position = position;

                for ( int i = 0; i < _stars.Length; i++ ) {
                    _stars[i] = new Vector2((float) (r.NextDouble() * (double) TileDisplaySize),
                                           (float) (r.NextDouble() * (double) TileDisplaySize));
                }
            }

            internal override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
                var tileCorner = new Vector2(Position.X - TileSize / 2f,
                                             Position.Y - TileSize / 2f);
                var displayUnits = new Vector2();
                ConvertUnits.ToDisplayUnits(ref tileCorner, out displayUnits);
                foreach ( Vector2 star in _stars ) {
                    spriteBatch.Draw(Image, displayUnits + star, Color.White);
                }
            }

            internal override bool IsSolid() {
                return false;
            }

            internal override void Dispose() {
            }

            internal override bool IsDisposed() {
                return false;
            }
        }

        private class LandTile : Tile {

            public static Texture2D Image { get; set; }
            private bool _disposed = false;

            public LandTile(Vector2 position) {
                Position = position;
            }

            internal override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
                if ( !IsDisposed() ) {
                    var tileCorner = new Vector2(Position.X - TileSize / 2f,
                                                 Position.Y - TileSize / 2f);
                    Vector2 displayPosition = new Vector2();
                    ConvertUnits.ToDisplayUnits(ref tileCorner, out displayPosition);
                    spriteBatch.Draw(Image, displayPosition, Color.White);
                }

                
                if (Arena.Debug) 
                    DebugPrint(spriteBatch, camera);
            }

            private void DebugPrint(SpriteBatch spriteBatch, Camera2D camera) {
                var tileCorner = new Vector2(Position.X - TileSize / 2f,
                                             Position.Y - TileSize / 2f);
                var displayPosition = new Vector2();
                ConvertUnits.ToDisplayUnits(ref tileCorner, out displayPosition);
                spriteBatch.DrawString(Level.CurrentLevel._debugFont, String.Format("{0},{1}", Position.X, Position.Y), displayPosition, Color.YellowGreen);
            }

            internal override bool IsSolid() {
                return !IsDisposed();
            }

            internal override void Dispose() {
                _disposed = true;
                foreach (Body body in Bodies) {
                    body.Dispose();
                }
            }

            internal override bool IsDisposed() {
                return _disposed;
            }
        }
    }
}

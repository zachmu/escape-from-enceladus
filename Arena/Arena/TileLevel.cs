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
using Squared.Tiled;
using TiledTile = Squared.Tiled.Layer.TiledTile;

namespace Arena {

    public class TileLevel {
        private SpriteFont _debugFont;

        internal const float TileSize = 1f;
        internal Layer CollisionLayer;

        public static TileLevel CurrentLevel { get; private set; }
        public Map LevelMap { get; private set; }

        public TileLevel(ContentManager cm, String mapFile, World world) {
            CurrentLevel = this;

            //BlankTile.Image = cm.Load<Texture2D>("star");
            _debugFont = cm.Load<SpriteFont>("debugFont");

            LevelMap = Map.Load(mapFile, cm);
            CollisionLayer = LevelMap.Layers["Collision"];

            InitializeEdges(world);
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera, Rectangle viewportSize) {
            LevelMap.Draw(spriteBatch, viewportSize, camera.ConvertScreenToWorld(new Vector2(0f, 0f)));
        }

        /// <summary>
        /// Analyzes and stores the edges of the tiles, to be used by the physics engine.
        /// </summary>
        /// <param name="world"> </param>
        public void InitializeEdges(World world) {
            // First we break the tiles down into connected groups.
            FindGroups();

            // Then create edges using these groups
            CreateEdges(world);
        }

        /// <summary>
        /// Gets the tile located at the sim location given, having the collision normal given.
        /// </summary>
        internal TiledTile GetTile(Vector2 location, Vector2 normal) {

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

            // Assume that this tile, or the one of its neighbors, is the cause of the collision
            IList<TiledTile> candidates = new List<TiledTile>() {
                                                          CollisionLayer.GetTile(x, y),
                                                          CollisionLayer.GetTile(x, y - 1),
                                                          CollisionLayer.GetTile(x, y + 1),
                                                          CollisionLayer.GetTile(x + 1, y),
                                                          CollisionLayer.GetTile(x - 1, y),
                                                          CollisionLayer.GetTile(x + 1, y + 1),
                                                          CollisionLayer.GetTile(x - 1, y - 1),
                                                          CollisionLayer.GetTile(x + 1, y - 1),
                                                          CollisionLayer.GetTile(x - 1, y + 1),
            };

            foreach ( TiledTile t in candidates.Where(cand => cand != null) ) {
                Vector2 upperleft = t.Position - new Vector2(TileSize / 2);
                Vector2 lowerRight = t.Position + new Vector2(TileSize / 2);
                Console.WriteLine("Evaluating tile with bounds {0},{1}", upperleft, lowerRight);
                if ( location.X >= upperleft.X && location.X <= lowerRight.X && location.Y >= upperleft.Y &&
                     location.Y <= lowerRight.Y ) {
                    return t;
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

            HashSet<TiledTile> seen = new HashSet<TiledTile>();

            foreach ( Layer.TiledTile tile in CollisionLayer.GetTiles().Where(tile => tile != null) ) {
                if ( !seen.Contains(tile) ) {
                    seen.UnionWith(tile.Group);
                    CreateEdges(world, tile);
                }
            }
        }

        /// <summary>
        /// Creates the edges for the group containing the tile given.  Group membership must already be established.
        /// </summary>
        private void CreateEdges(World world, TiledTile tile) {
            Edges edges = new Edges();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach ( TiledTile t in tile.Group ) {
                TiledTile left = t.GetLeftTile();
                if ( left == null ) {
                    edges.Add(GetLeftEdge(t));
                }

                TiledTile up = t.GetUpTile();
                if ( up == null ) {
                    edges.Add(GetTopEdge(t));
                }

                TiledTile right = t.GetRightTile();
                if ( right == null ) {
                    edges.Add(GetRightEdge(t));
                }

                TiledTile down = t.GetDownTile();
                if ( down == null ) {
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

                foreach ( TiledTile t in tile.Group ) {
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

        private static Edge GetBottomEdge(TiledTile tile) {
            return new Edge(tile.Position + new Vector2(0, 1), tile.Position + new Vector2(1, 1));
        }

        private static Edge GetRightEdge(TiledTile tile) {
            return new Edge(tile.Position + new Vector2(1, 0), tile.Position + new Vector2(1, 1));
        }

        private static Edge GetTopEdge(TiledTile tile) {
            return new Edge(tile.Position, tile.Position + new Vector2(1, 0));
        }

        private static Edge GetLeftEdge(TiledTile tile) {
            return new Edge(tile.Position, tile.Position + new Vector2(0, 1));
        }

        /// <summary>
        /// Destroys this tile
        /// </summary>
        public void DestroyTile(World world, TiledTile t) {
            t.Dispose();

            IEnumerable<TiledTile> neighbors = FindAdjacentSolidTiles(t);
            var seenTiles = new HashSet<TiledTile>();

            foreach ( TiledTile tile in neighbors ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<TiledTile> group = new HashSet<TiledTile>();

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
                foreach ( Vector2 v in chain ) {
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
            var seenTiles = new HashSet<TiledTile>();            

            foreach ( TiledTile tile in CollisionLayer.GetTiles().Where(tile => tile != null) ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<TiledTile> group = new HashSet<TiledTile>();
                    FindConnectedTiles(tile, group, seenTiles);
                }
            }
        }

        /// <summary>
        /// Finds all tiles that are connected to this one, setting its membership to be the group given.
        /// </summary>
        internal void FindConnectedTiles(TiledTile tile, ISet<TiledTile> group, ISet<TiledTile> seenTiles) {
            //Console.WriteLine("Adding tile {0} to group {1}", tile.Position, group);

            group.Add(tile);
            seenTiles.Add(tile);
            tile.Group = group;
            foreach ( Layer.TiledTile connected in FindAdjacentSolidTiles(tile) ) {
                if ( !seenTiles.Contains(connected) ) {
                    FindConnectedTiles(connected, group, seenTiles);
                }
            }
        }

        /// <summary>
        /// Returns the 0-4 adjacent solid tiles connected to this one.
        /// </summary>
        internal IEnumerable<Layer.TiledTile> FindAdjacentSolidTiles(Layer.TiledTile tile) {
            return
                new Layer.TiledTile[] { tile.GetLeftTile(), tile.GetUpTile(), tile.GetRightTile(), tile.GetDownTile() }.Where(
                    adj => adj != null).ToList();
        }
    }

}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Arena.Entity;
using Arena.Entity.Enemy;
using Arena.Entity.NPC;
using Arena.Farseer;
using Arena.Weapon;
using Arena.Xbox;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Arena.Map {

    public class TileLevel {
        public const float TileSize = 1f;
        private const string RoomLayerName = "Rooms";
        private const string CollisionLayerName = "Blocks";
        private const string DestructionLayerName = "Destruction";
        private const string DoorLayerName = "Doors";
        public static int TileDisplaySize;
        private readonly Layer _collisionLayer;
        private readonly List<Room> _rooms = new List<Room>();
        private readonly Dictionary<Room, List<Door>> _doorsByRoom = new Dictionary<Room, List<Door>>(); 
        private readonly List<DestructionRegion> _destructionRegions = new List<DestructionRegion>();
        private readonly List<Door> _doors = new List<Door>(); 
        private readonly Map _levelMap;
        private readonly World _world;
        private readonly HashSet<Tile> _tilesToRemove = new HashSet<Tile>();
        private readonly HashSet<Tile> _tilesToAdd = new HashSet<Tile>();

        public static TileLevel CurrentLevel { get; private set; }

        public static Room CurrentRoom { get; private set; }

        public int Width {
            get { return _levelMap.Width; }
        }

        public int Height {
            get { return _levelMap.Height; }
        }

        public TileLevel(ContentManager cm, String mapFile, World world, Vector2 startPosition) {
            CurrentLevel = this;

            _world = world;
            
             TileDisplaySize = (int) ConvertUnits.ToDisplayUnits(TileSize);

            _levelMap = Map.Load(mapFile, cm);
            Door.LoadContent(cm);
            
            _collisionLayer = _levelMap.Layers[CollisionLayerName];
            
            try {
                ObjectGroup roomGroup = _levelMap.ObjectGroups[RoomLayerName];
                InitializeRooms(roomGroup);
            } catch {
                // No rooms layer is fine, we'll just create a room for them
                //_rooms.Add(new Room(new Vector2(0, 0), new Vector2(_levelMap.Width, _levelMap.Height)));
            }

            try {
                ObjectGroup doorGroup = _levelMap.ObjectGroups[DoorLayerName];
                InitializeDoors(doorGroup);
            } catch {
                // no doors on this level (tests, mostly)
            }

            try {
                ObjectGroup playerGroup = _levelMap.ObjectGroups["PlayerStart"];
                foreach ( Object region in playerGroup.Objects ) {
                    var topLeft = ConvertUnits.ToSimUnits(new Vector2(region.X, region.Y));
                    Player.Instance.Position = topLeft;
                    break;
                }
            } catch {
                Player.Instance.Position = new Vector2(10, 10);
            }

            SetCurrentRoom(startPosition);
        }

        private void InitializeDoors(ObjectGroup doorGroup) {
            foreach ( Object region in doorGroup.Objects ) {
                var topLeft = ConvertUnits.ToSimUnits(new Vector2(region.X, region.Y));
                var bottomRight = ConvertUnits.ToSimUnits(new Vector2(region.X + region.Width, region.Y + region.Height));
                Door door = new Door(_world, topLeft, bottomRight);
                _doors.Add(door);

                // Add the door to any straddling rooms
                _rooms.ForEach(room => {
                    if ( room.Intersects(door) ) {
                        if ( !_doorsByRoom.ContainsKey(room) ) {
                            _doorsByRoom[room] = new List<Door>();
                        }
                        _doorsByRoom[room].Add(door);
                    }
                });
            }
        }

        private void InitializeDestructionRegions() {
            ObjectGroup destructionRegion = _levelMap.ObjectGroups[DestructionLayerName];
            foreach ( Object region in destructionRegion.Objects ) {
                Vector2 topLeft = ConvertUnits.ToSimUnits(new Vector2(region.X, region.Y));
                if ( CurrentRoom.Contains(topLeft) ) {
                    Vector2 bottomRight =
                        ConvertUnits.ToSimUnits(new Vector2(region.X + region.Width, region.Y + region.Height));

                    int flags = 0;
                    foreach ( String weaponName in region.Properties.Keys ) {
                        switch ( weaponName ) {
                            case "shot":
                                flags |= Shot.Flags;
                                break;
                            case "missile":
                                flags |= Missile.Flags;
                                break;
                        }
                    }
                    if ( flags == 0 ) {
                        flags = 0xFFFF;
                    }

                    Vector2 currTopLeft = topLeft;
                    while ( currTopLeft.Y <= bottomRight.Y ) {
                        currTopLeft.X = topLeft.X;
                        while ( currTopLeft.X <= bottomRight.X ) {
                            _destructionRegions.Add(new DestructionRegion(_world, currTopLeft,
                                                                          currTopLeft + new Vector2(1f), flags));
                            currTopLeft.X += TileSize;
                        }
                        currTopLeft.Y += TileSize;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the set of rooms in this level.  
        /// The level data records rectangular regions called "rooms"; some of these are 
        /// actually rooms, and some are rectangular portions of rooms.  Membership of region 
        /// to room is marked with an ID.
        /// </summary>
        private void InitializeRooms(ObjectGroup rooms) {

            Dictionary<String, List<Object>> multiRegionRooms = new Dictionary<string, List<Object>>();
            foreach ( Object region in rooms.Objects ) {
                if ( region.Properties.ContainsKey("id") ) {
                    string id = region.Properties["id"];
                    if ( !multiRegionRooms.ContainsKey(id) ) {
                        multiRegionRooms[id] = new List<Object>();
                    }
                    multiRegionRooms[id].Add(region);
                } else {
                    _rooms.Add(new Room(region));
                }
            }

            foreach ( String id in multiRegionRooms.Keys ) {
                _rooms.Add(new Room(multiRegionRooms[id]));
            }
        }

        /// <summary>
        /// Sets and returns the current room based on the point given.
        /// Tears down any managed resources associated with the old room and creates them for this one.
        /// </summary>
        public Room SetCurrentRoom(Vector2 position) {
            CurrentRoom = GetNextRoom(position);

            TearDownEdges();
            InitializeEdges();
            CreateEnemies();
            CreateNPCs();
            TearDownDestructionRegions();
            InitializeDestructionRegions();

            Arena.Instance.StepWorld(null);

            return CurrentRoom;
        }

        /// <summary>
        /// Returns the next room, besides the current one, that contains the point given.
        /// </summary>
        public Room GetNextRoom(Vector2 position) {
            return _rooms.FirstOrDefault(room => room != CurrentRoom && room.Contains(position, TileSize / 2f));
        }

        private void TearDownDestructionRegions() {
            _destructionRegions.ForEach(region => region.Dispose());
            _destructionRegions.Clear();
        }

        private void TearDownEdges() {
            foreach ( Tile t in _collisionLayer.GetTiles().Where(tile => tile != null) ) {
                t.DestroyAttachedBodies();
            }
        }

        private void CreateNPCs() {
            Dictionary<string, ObjectGroup> objectGroups = _levelMap.ObjectGroups;
            if ( objectGroups.ContainsKey("NPC") ) {
                ObjectGroup npcGroup = objectGroups["NPC"];
                foreach ( Object region in npcGroup.Objects ) {
                    Vector2 pos = ConvertUnits.ToSimUnits(region.X, region.Y);
                    if ( CurrentRoom.Contains(pos) ) {
                        Arena.Instance.Register(NPCFactory.Create(region, _world));
                    }
                }
            }
        }

        private void CreateEnemies() {
            Dictionary<string, ObjectGroup> objectGroups = _levelMap.ObjectGroups;
            if (objectGroups.ContainsKey("Enemies")) {
                ObjectGroup enemies = objectGroups["Enemies"];
                foreach ( Object obj in enemies.Objects ) {
                    Vector2 pos = ConvertUnits.ToSimUnits(obj.X, obj.Y);
                    if ( CurrentRoom.Contains(pos) ) {
                        Arena.Instance.Register(EnemyFactory.CreateEnemy(obj, _world));
                    }
                }
            }
        }

        public void DrawBackground(SpriteBatch spriteBatch, Camera2D camera, Rectangle viewportSize) {
            var topLeft = camera.ConvertScreenToWorld(new Vector2(0f, 0f));
            var bottomRight = camera.ConvertScreenToWorld(new Vector2(viewportSize.Width, viewportSize.Height));
            var diff = bottomRight - topLeft;
            _levelMap.DrawBackground(spriteBatch, new Rectangle((int) topLeft.X, (int) topLeft.Y, (int) diff.X, (int) diff.Y));
            _doors.ForEach(door => door.Draw(spriteBatch, camera));
        }

        public void DrawForeground(SpriteBatch spriteBatch, Camera2D camera, Rectangle viewportSize) {
            var topLeft = camera.ConvertScreenToWorld(new Vector2(0f, 0f));
            var bottomRight = camera.ConvertScreenToWorld(new Vector2(viewportSize.Width, viewportSize.Height));
            var diff = bottomRight - topLeft;
            _levelMap.DrawForeground(spriteBatch, new Rectangle((int) topLeft.X, (int) topLeft.Y, (int) diff.X, (int) diff.Y));
        }

        public void Update(GameTime gameTime) {
            _levelMap.Update(gameTime);

            if ( _tilesToRemove.Count > 0 || _tilesToAdd.Count > 0 ) {
                HashSet<Tile> affectedTiles = new HashSet<Tile>();
                foreach ( Tile t in _tilesToRemove ) {
                    t.Dispose();
                    affectedTiles.Add(t);
                    TileInfo tileInfo = t.GetBlockTextureInfo();
#if XBOX
                    int numPieces = 2;
#else
                    int numPieces = 4;
#endif
                    Arena.Instance.Register(new ShatterAnimation(_world, tileInfo.Texture, tileInfo.Rectangle,
                                                                 t.Position + new Vector2(TileSize / 2, TileSize / 2),
                                                                 numPieces, 20f));
                }

                // When recreating tiles, we need to be careful not to recreate 
                // any on top of the player or any other entities.
                List<Tile> safeToAdd = _tilesToAdd.Where(tile => !tile.EntitiesOverlapping()).ToList();
                foreach ( Tile t in safeToAdd ) {
                    t.Revive();
                    FindAdjacentSolidTiles(t).ForEach(tile => tile.DestroyAttachedBodies());
                    affectedTiles.Add(t);
                }
                RecreateEdges(affectedTiles);

                _tilesToRemove.Clear();
                safeToAdd.ForEach(tile => _tilesToAdd.Remove(tile));
            }

            _doors.ForEach(door => door.Update(gameTime));
        }

        /// <summary>
        /// Analyzes and stores the edges of the tiles, to be used by the physics engine.
        /// </summary>
        private void InitializeEdges() {
            // First we break the tiles down into connected groups.
            FindGroups();

            // Then create edges using these groups
            CreateEdges();
        }

        /// <summary>
        /// Gets the tile located at the sim location given, having the collision normal given.
        /// </summary>
        internal Tile GetCollidedTile(Vector2 location, Vector2 normal) {

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

            List<Vector2> positions = new List<Vector2> {
                new Vector2(x, y),
                new Vector2(x, y - 1),
                new Vector2(x, y + 1),
                new Vector2(x + 1, y),
                new Vector2(x - 1, y),
                new Vector2(x + 1, y + 1),
                new Vector2(x - 1, y - 1),
                new Vector2(x + 1, y - 1),
                new Vector2(x - 1, y + 1),
            };

            foreach ( Vector2 v in positions ) {
                Tile t = _collisionLayer.GetTile(v);
                if ( !TileNullOrDisposed(t) ) {
                    Vector2 upperleft = t.Position;
                    Vector2 lowerRight = t.Position + new Vector2(TileSize);
//                    Console.WriteLine("Evaluating tile with bounds {0},{1}", upperleft, lowerRight);
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
        private void CreateEdges() {
            // Alternate coordinate system: 0,0 refers to the upper left corner of the upper-left tile,
            // 1,1 the bottom-right corner of that same tile.  Thus a tile's four corners are defined by:
            // Upper-left: x, y
            // Upper-right: x+1, y
            // Bottom-right: x+1, y+1
            // Bottom-left: x, y+1

            HashSet<Tile> seen = new HashSet<Tile>();

            foreach ( Tile tile in _collisionLayer.GetTiles().Where(IsLiveTileInCurrentRoom()) ) {
                if ( !seen.Contains(tile) ) {
                    seen.UnionWith(tile.Group);
                    CreateEdges(tile);
                }
            }
        }

        /// <summary>
        /// Creates the edges for the group containing the tile given.  Group membership must already be established.
        /// </summary>
        private void CreateEdges(Tile tile) {
            Edges edges = new Edges();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach ( Tile t in tile.Group ) {
                t.Bodies.Clear();

                Tile left = t.GetLeftTile();
                if ( TileNullOrDisposed(left) ) {
                    edges.Add(GetLeftEdge(t));
                }

                Tile up = t.GetUpTile();
                if ( TileNullOrDisposed(up) ) {
                    edges.Add(GetTopEdge(t));
                }

                Tile right = t.GetRightTile();
                if ( TileNullOrDisposed(right) ) {
                    edges.Add(GetRightEdge(t));
                }

                Tile down = t.GetDownTile();
                if ( TileNullOrDisposed(down) ) {
                    edges.Add(GetBottomEdge(t));
                }
            }
            watch.Stop();
            //Console.WriteLine("Creating edges object took {0} ticks", watch.ElapsedTicks);
            watch.Reset();

            watch.Start();
            while ( edges.HasEdges() ) {
                Vector2 initialVertex = edges.GetInitialVertex();
                Vector2 currentVertex = initialVertex;
                // Pick a random edge to start walking.  This determines the handedness of the loop we will build
                Edge currentEdge = edges.GetEdgesWithVertex(currentVertex).First();
                Edge prevEdge = null;
                Vertices chain = new Vertices();
                Edges processedEdges = new Edges();

                int i = 0;
                // work our way through the vertices, linking them together into a chain
                do {
                    processedEdges.Add(currentEdge);

                    // only add vertices that aren't colinear with our current edge
                    if ( prevEdge == null || !AreEdgesColinear(prevEdge, currentEdge) ) {
                        chain.Add(currentVertex);
                    }
                    Edge edge = edges.GetNextEdge(currentEdge, currentVertex);
                    currentVertex = currentEdge.GetOtherVertex(currentVertex);
                    prevEdge = currentEdge;
                    currentEdge = edge;

                    if ( i++ > 10000 ) {
                        i++;
                    }
                } while ( currentVertex != initialVertex );

                Stopwatch factoryWatch = new Stopwatch();

                factoryWatch.Start();
                //Console.WriteLine("Creating chain with vertices {0}", string.Join(",", chain));
                Body loopShape = BodyFactory.CreateLoopShape(_world, chain);
                loopShape.Friction = .5f;
                loopShape.IsStatic = true;
                loopShape.CollidesWith = Category.All;
                loopShape.CollisionCategories = Arena.TerrainCategory;
                loopShape.UserData = UserData.NewTerrain();

                //Console.WriteLine("Created body with id {0} ", RuntimeHelpers.GetHashCode(loopShape));
                factoryWatch.Stop();
                //Console.WriteLine("Body factory took {0} ticks", factoryWatch.ElapsedTicks);

                foreach ( Tile t in tile.Group ) {
                    t.Bodies.Add(loopShape);
                }

                edges.Remove(processedEdges);
            }
            watch.Stop();
            //Console.WriteLine("Processing edges into shape(s) took {0} ticks", watch.ElapsedTicks);
        }

        private bool TileNullOrDisposed(Tile tile) {
            return !IsLiveTileInCurrentRoom().Invoke(tile);
        }

        private bool AreEdgesColinear(Edge prevEdge, Edge currentEdge) {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            return (currentEdge.One.X == prevEdge.One.X
                    && currentEdge.Two.X == prevEdge.Two.X)
                   || (currentEdge.One.Y == prevEdge.One.Y
                       && currentEdge.Two.Y == prevEdge.Two.Y);
            // ReSharper restore CompareOfFloatsByEqualityOperator
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
        /// Notify the level that a tile was hit by a shot
        /// </summary>
        public void TileHitBy(Tile hitTile, Projectile shot) {
            if ( hitTile != null && IsTileDestroyedBy(hitTile, shot) ) {
                DestroyTile(hitTile);
            }
        }

        /// <summary>
        /// Returns whether this tile is destroyed by the shot given, accoding to the destruction map.
        /// </summary>
        private bool IsTileDestroyedBy(Tile hitTile, Projectile projectile) {
            return _destructionRegions.Any(region => region.Contains(hitTile) && region.DestroyedBy(projectile));
        }

        /// <summary>
        /// Destroys this tile, unless it's already been destroyed.
        /// </summary>
        public void DestroyTile(Tile t) {
            if ( !t.Disposed ) {
                _tilesToRemove.Add(t);
            }
        }

        /// <summary>
        /// Revives the tile given, restoring it to the game model.
        /// </summary>
        public void ReviveTile(Tile tile) {
            _tilesToAdd.Add(tile);
        }

        private void RecreateEdges(IEnumerable<Tile> tiles) {
            HashSet<Tile> tilesToConsider = new HashSet<Tile>();
            foreach ( Tile t in tiles ) {
                tilesToConsider.Add(t);
                FindAdjacentSolidTiles(t).ForEach(tile => tilesToConsider.Add(tile));
            }
            var seenTiles = new HashSet<Tile>();

            foreach (
                Tile tile in tilesToConsider.Where(IsLiveTileInCurrentRoom()) ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<Tile> group = new HashSet<Tile>();

                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    FindConnectedTiles(tile, group, seenTiles);
                    watch.Stop();
                    Console.WriteLine("Finding connected tiles took {0} ticks", watch.ElapsedTicks);
                    watch.Reset();

                    watch.Start();
                    CreateEdges(tile);
                    watch.Stop();
                    Console.WriteLine("Creating edges took {0} ticks", watch.ElapsedTicks);
                }
            }

            Player.Instance.NotifyTerrainChange();
        }

        /// <summary>
        /// Collection of edges with vertex querying capability
        /// </summary>
        private class Edges {
            private readonly IDictionary<Vector2, ICollection<Edge>> _edgesByVertex =
                new Dictionary<Vector2, ICollection<Edge>>();

            public void Add(Edge edge) {
                foreach ( Vector2 endpoint in new[] {edge.One, edge.Two} ) {
                    if ( !_edgesByVertex.ContainsKey(endpoint) ) {
                        _edgesByVertex.Add(endpoint, new HashSet<Edge>());
                    }
                    _edgesByVertex[endpoint].Add(edge);
                }
            }

            public IEnumerable<Edge> GetEdgesWithVertex(Vector2 vertex) {
                return _edgesByVertex[vertex];
            }

            public Vector2 GetInitialVertex() {
                return _edgesByVertex.Keys.First();
            }

            public bool HasEdges() {
                return _edgesByVertex.Count > 0;
            }

            public void Remove(IEnumerable<Vector2> chain) {
                foreach ( Vector2 v in chain ) {
                    _edgesByVertex.Remove(v);
                }
            }

            public void Remove(Edges processedEdges) {
                foreach ( Vector2 v in processedEdges._edgesByVertex.Keys ) {
                    if ( _edgesByVertex.ContainsKey(v) ) {
                        var edges = _edgesByVertex[v];
                        foreach ( Edge e in processedEdges._edgesByVertex[v] ) {
                            edges.Remove(e);
                        }
                        if ( edges.Count == 0 ) {
                            _edgesByVertex.Remove(v);
                        }
                    }
                }
            }

            /// <summary>
            /// Returns the next edge to walk after this one, which will always attempt to try to take a right-hand turn.
            /// </summary>
            /// <param name="currentEdge"> </param>
            /// <param name="initialVertex">The initial vertex of this edge to determine which direction to walk.</param>
            public Edge GetNextEdge(Edge edge, Vector2 initialVertex) {
                var otherVertex = edge.GetOtherVertex(initialVertex);
                Edge down = new Edge(otherVertex, otherVertex + new Vector2(0, 1));
                Edge up = new Edge(otherVertex, otherVertex + new Vector2(0, -1));
                Edge left = new Edge(otherVertex, otherVertex + new Vector2(-1, 0));
                Edge right = new Edge(otherVertex, otherVertex + new Vector2(1, 0));                

                if ( otherVertex.X > initialVertex.X ) { // walking right
                    return new[] { down, up, right }.FirstOrDefault(e => _edgesByVertex[otherVertex].Contains(e));
                } else if (otherVertex.X < initialVertex.X) { // walking left
                    return new[] { up, down, left }.FirstOrDefault(e => _edgesByVertex[otherVertex].Contains(e));                    
                } else if (otherVertex.Y > initialVertex.Y) { // walking down
                    return new[] { left, right, down }.FirstOrDefault(e => _edgesByVertex[otherVertex].Contains(e));                                        
                } else if (otherVertex.Y < initialVertex.Y) { // walking up
                    return new[] { right, left, up }.FirstOrDefault(e => _edgesByVertex[otherVertex].Contains(e));                    
                } else {
                    throw new Exception(String.Format("Illegal state when walking edge {0} from {1}", edge, initialVertex));
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

            foreach ( Tile tile in _collisionLayer.GetTiles().Where(IsLiveTileInCurrentRoom()) ) {
                if ( !seenTiles.Contains(tile) ) {
                    HashSet<Tile> group = new HashSet<Tile>();
                    FindConnectedTiles(tile, group, seenTiles);
                }
            }
        }

        /// <summary>
        /// Finds all tiles that are connected to this one, setting its membership to be the group given.
        /// </summary>
        private void FindConnectedTiles(Tile tile, HashSet<Tile> group, HashSet<Tile> seenTiles) {
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
        private List<Tile> FindAdjacentSolidTiles(Tile tile) {
            return
                new[] { tile.GetLeftTile(), tile.GetUpTile(), tile.GetRightTile(), tile.GetDownTile() }.Where(
                    IsLiveTileInCurrentRoom()).ToList();
        }

        /// <summary>
        /// Returns a function to tell whether a given tile is active and in the current room
        /// </summary>
        /// <returns></returns>
        private Func<Tile, bool> IsLiveTileInCurrentRoom() {
            return adj => adj != null && !adj.Disposed && CurrentRoom.Contains(adj.Position, TileSize);
        }

        /// <summary>
        /// Returns the collision-layer tile at the point given, or null if there isn't one.
        /// </summary>
        public Tile GetTile(Vector2 position) {
            return _collisionLayer.GetTile(position);
        }

        /// <summary>
        /// Returns whether there is a live (non-destroyed) collision layer tile at the point given.
        /// </summary>
        public bool IsLiveTile(Vector2 position) {
            Tile t = GetTile(position);
            return t != null && !t.Disposed;
        }

        /// <summary>
        /// Returns the room at the coordinates given or null if there isn't one
        /// </summary>
        public Room RoomAt(int x, int y) {
            return _rooms.FirstOrDefault(room => room.Contains(x, y));
        }

        /// <summary>
        /// Returns the room at the coordinates given or null if there isn't one
        /// </summary>
        public Room RoomAt(Vector2 point) {
            return _rooms.FirstOrDefault(room => room.Contains(point));
        }

        /// <summary>
        /// Returns the set of doors attached to this room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public List<Door> GetDoorsAttachedToRoom(Room room) {
            return _doorsByRoom[room] ?? new List<Door>();
        }
    }
}

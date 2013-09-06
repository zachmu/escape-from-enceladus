using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Overlay;
using Enceladus.Util;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// A holocube placement "weapon"
    /// </summary>
    public class Holocube : GameEntityAdapter, IWeapon {

        private Vector2 _start;
        private float _angle;
        private Vector2 _cubeCorner;
        private Direction _direction;
        private World _world;
        internal static Texture2D Image;
        private readonly RandomWalk _alpha;
        private bool _projecting;
        private readonly Projection _projection;

        private const int MaxRange = 30;
        private const float MinAlpha = .2f;
        private const float MaxAlpha = .9f;

        public static void LoadContent(ContentManager cm) {
            Image = cm.Load<Texture2D>("Projectile/Holocube0000");
        }

        public Holocube(World world, Vector2 start, Direction direction) {
            _projection = new Projection();
            _alpha = new RandomWalk(1, .1f, .2f, .8f);
            _world = world;
            UpdateProjection(start, direction);
        }

        public void UpdateProjection(Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
            _alpha.Update(MinAlpha, MaxAlpha);
            DetermineLength();
        }

        internal static Random Random = new Random();

        private void DetermineLength() {

            // Don't forget to invert the y coordinate because of the differing y axes
            Vector2 diff = new Vector2((float) Math.Cos(_angle) * MaxRange, (float) -Math.Sin(_angle) * MaxRange);
            Vector2 end = _start + diff;

            _cubeCorner = _world.RayCastTileCorner(_start, end);
            if ( _cubeCorner == Vector2.Zero ) {
                _projecting = false;
                _cubeCorner = end;
            } else {
                _projecting = true;
            }

            _legalPlacement = !EnceladusGame.EntitiesOverlapping(new AABB(_cubeCorner + new Vector2(.01f), _cubeCorner + new Vector2(TileLevel.TileSize - .02f)));
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Color color = _legalPlacement && _projecting ? SolidColorEffect.DisabledColor : Color.PaleVioletRed;

            if ( _projecting ) {
                // Draw the cube itself
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
                spriteBatch.Draw(Image, displayPosition, null, color * _alpha.Value,
                                 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);
            }

            Vector2 beamEnd = _cubeCorner + new Vector2(.5f);
            Vector2 diff = (beamEnd - _start);
            float length = diff.Length();

            // Don't draw the beam if it's too close to the player, since it looks funny
            if ( length > 2 ) {
                _projection.Draw(spriteBatch, _start, _cubeCorner, _direction, color * _alpha.Value);
            }
        }

        public void Fire() {
            if ( _projecting && _legalPlacement ) {
                HolocubeBlock block = new HolocubeBlock(_world, _cubeCorner);
                EnceladusGame.Instance.Register(block);
                PlacedBlocks.Enqueue(block);
                if ( PlacedBlocks.Count > MaxNumBlocks ) {
                    HolocubeBlock holocubeBlock = PlacedBlocks.Dequeue();
                    if ( !holocubeBlock.Disposed ) {
                        holocubeBlock.Destroy();
                    }
                }
            } else {
                SoundEffectManager.Instance.PlaySoundEffect("nope");
            }
        }

        public int DestructionFlags {
            get { return EnceladusGame.HolocubeDestructionFlag; }
        }

        public float BaseDamage {
            get { return 1; }
        }

        private const int MaxNumBlocks = 1;

        private static readonly Queue<HolocubeBlock> PlacedBlocks = new Queue<HolocubeBlock>();
        private bool _legalPlacement;
    }

    /// <summary>
    /// Static, solid holocube that has been placed.
    /// </summary>
    public class HolocubeBlock : GameEntityAdapter {

        private readonly Vector2 _cubeCorner;
        private readonly Timer _timeToLive;
        private readonly Body _block;
        private readonly World _world;
        private const double DissolveTimeMs = 2000;
        private const double SolidTimeMs = 8000;
        private readonly RandomWalk _alpha;

        public HolocubeBlock(World world, Vector2 cubeCorner) {
            _cubeCorner = cubeCorner;
            _timeToLive = new Timer(SolidTimeMs + DissolveTimeMs);
            _block = BodyFactory.CreateRectangle(world, TileLevel.TileSize, TileLevel.TileSize, 1f, UserData.NewHolocube());
            _block.IsStatic = true;
            _block.CollisionCategories = EnceladusGame.TerrainCategory;
            _block.CollidesWith = Category.All;
            _block.Position = cubeCorner + new Vector2(TileLevel.TileSize / 2f);
            _alpha = new RandomWalk(1f, .05f, .3f, .7f);

            _world = world;
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
            spriteBatch.Draw(Holocube.Image, displayPosition, null, SolidColorEffect.DisabledColor * _alpha.Value,
                             0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);
        }

        public override void Update(GameTime gameTime) {
            _timeToLive.Update(gameTime);
            if ( !Disposed && _timeToLive.IsTimeUp() ) {
                Dispose();
            }
            UpdateAlpha();
        }

        private void UpdateAlpha() {
            float minAlpha = _timeToLive.TimeLeft > DissolveTimeMs ? .75f : .1f;
            float maxAlpha = (float) (_timeToLive.TimeLeft > DissolveTimeMs ? 1f : _timeToLive.TimeLeft / DissolveTimeMs * .75f);
            _alpha.Update(minAlpha, maxAlpha);
        }

        public override Vector2 Position {
            get { return _cubeCorner; }
        }

        public override void Dispose() {
            if ( _block != null ) {
                _block.Dispose();
            }
        }

        public override bool Disposed {
            get {
                if ( _block != null ) {
                    return _block.IsDisposed;
                } else {
                    return false;
                }
            }
        }

        public void Destroy() {
            Dispose();
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, Holocube.Image,
                                                                 SolidColorEffect.DisabledColor * _alpha.Value, null,
                                                                 _cubeCorner + new Vector2(TileLevel.TileSize / 2), 4,
                                                                 4f));
        }
    }
}

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

        private readonly World _world;
        internal static Texture2D HolocubeImage;
        private readonly RandomWalk _alpha;
        private readonly ProjectionBeam _projectionBeam;
        private readonly Projection _projection;

        private const float MinAlpha = .2f;
        private const float MaxAlpha = .9f;

        public static void LoadContent(ContentManager cm) {
            HolocubeImage = cm.Load<Texture2D>("Projectile/Holocube0000");
        }

        public Holocube(World world, Vector2 start, Direction direction) {
            _projectionBeam = new ProjectionBeam();
            _projection = new Projection(world);
            _alpha = new RandomWalk(1, .1f, .2f, .8f);
            _world = world;
            UpdateProjection(start, direction);
        }

        public void UpdateProjection(Vector2 start, Direction direction) {
            _projection.UpdateProjection(start, direction);
        }

        public override void Update(GameTime gameTime) {
            _alpha.Update(MinAlpha, MaxAlpha);
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Color color = _projection.IsProjecting && _projection.IsLegalPlacement ? SolidColorEffect.DisabledColor : Color.PaleVioletRed;

            if ( _projection.IsProjecting ) {
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_projection.CubeCorner);
                spriteBatch.Draw(HolocubeImage, displayPosition, null, color,
                                 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);

            }

            Vector2 beamEnd = _projection.CubeCorner + new Vector2(.5f);
            Vector2 diff = (beamEnd - _projection.Start);
            float length = diff.Length();

            // Don't draw the beam if it's too close to the player, since it looks funny
            if ( length > 2 ) {
                _projectionBeam.Draw(spriteBatch, _projection.Start, _projection.CubeCorner, _projection.Direction, color * _alpha.Value);
            }
        }

        public void Fire() {
            if ( _projection.IsProjecting && _projection.IsLegalPlacement ) {
                HolocubeBlock block = new HolocubeBlock(_world, _projection.CubeCorner);
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
            spriteBatch.Draw(Holocube.HolocubeImage, displayPosition, null, SolidColorEffect.DisabledColor * _alpha.Value,
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
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, Holocube.HolocubeImage,
                                                                 SolidColorEffect.DisabledColor * _alpha.Value, null,
                                                                 _cubeCorner + new Vector2(TileLevel.TileSize / 2), 4,
                                                                 4f));
        }
    }
}

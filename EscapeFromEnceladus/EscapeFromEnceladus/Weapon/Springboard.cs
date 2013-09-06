using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using Enceladus.Util;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// The springboard item
    /// </summary>
    public class Springboard : GameEntityAdapter {


        private readonly World _world;
        internal static Texture2D CompressedImage;
        internal static Texture2D ExpandedImage;
        private readonly RandomWalk _alpha;
        private readonly ProjectionBeam _projectionBeam;
        private readonly Projection _projection;

        private const float MinAlpha = .2f;
        private const float MaxAlpha = .9f;

        private const int MaxNumBlocks = 1;
        private static readonly Queue<SpringboardBlock> PlacedSprings = new Queue<SpringboardBlock>();

        public static void LoadContent(ContentManager cm) {
            CompressedImage = cm.Load<Texture2D>("Projectile/SpringBoard0000");
            ExpandedImage = cm.Load<Texture2D>("Projectile/SpringBoard0001");
        }

        public Springboard(World world, Vector2 start, Direction direction) {
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
                spriteBatch.Draw(CompressedImage, displayPosition, null, color * _alpha.Value,
                                 0, new Vector2(0, 64), Vector2.One, SpriteEffects.None, 1.0f);
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
                SpringboardBlock block = new SpringboardBlock(_world, _projection.CubeCorner);
                EnceladusGame.Instance.Register(block);
                PlacedSprings.Enqueue(block);
                if ( PlacedSprings.Count > MaxNumBlocks ) {
                    SpringboardBlock springBlock = PlacedSprings.Dequeue();
                    if ( !springBlock.Disposed ) {
                        springBlock.Destroy();
                    }
                }
            } else {
                SoundEffectManager.Instance.PlaySoundEffect("nope");
            }
        }
    }

    /// <summary>
    /// Static, solid holocube that has been placed.
    /// </summary>
    public class SpringboardBlock : GameEntityAdapter {

        private readonly Vector2 _cubeCorner;
        private readonly Timer _timeToLive;
        private readonly Body _block;
        private readonly World _world;
        private bool _expanded;

        private Texture2D Image { get { return _expanded ? Springboard.ExpandedImage : Springboard.CompressedImage; } }

        public SpringboardBlock(World world, Vector2 cubeCorner) {
            _cubeCorner = cubeCorner;
            _timeToLive = new Timer(10000);
            _block = BodyFactory.CreateRectangle(world, TileLevel.TileSize, TileLevel.TileSize, 1f, UserData.NewHolocube());
            _block.IsStatic = true;
            _block.CollisionCategories = EnceladusGame.TerrainCategory;
            _block.CollidesWith = Category.All;
            _block.Position = cubeCorner + new Vector2(TileLevel.TileSize / 2f);

            _world = world;
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
            spriteBatch.Draw(Image, displayPosition, null, SolidColorEffect.DisabledColor,
                             0, new Vector2(0, 64), Vector2.One, SpriteEffects.None, 1.0f);
        }

        public override void Update(GameTime gameTime) {
            _timeToLive.Update(gameTime);
            if ( !Disposed && _timeToLive.IsTimeUp() ) {
                Dispose();
            }
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
            Rectangle originRectangle = new Rectangle(0, 64, 64, 64);
            EnceladusGame.Instance.Register(new ShatterAnimation(_world, Image,
                                                                 SolidColorEffect.DisabledColor, originRectangle, 
                                                                 _cubeCorner + new Vector2(TileLevel.TileSize / 2), 4,
                                                                 4f));
        }
    }

}

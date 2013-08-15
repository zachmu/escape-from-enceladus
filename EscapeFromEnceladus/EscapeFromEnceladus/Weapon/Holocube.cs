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
        private Vector2 _end;
        private Vector2 _cubeCorner;
        private Direction _direction;
        private World _world;
        internal static Texture2D Image;
        internal static Texture2D ProjectionImage;
        private float _alpha;
        private bool _projecting;

        private const int MaxRange = 30;
        private const float MinAlpha = .2f;
        private const float MaxAlpha = .9f;

        public static void LoadContent(ContentManager cm) {
            Image = cm.Load<Texture2D>("Projectile/Holocube0000");
            ProjectionImage = cm.Load<Texture2D>("Projectile/Holocube0001");
        }

        public Holocube(World world, Vector2 start, Direction direction) {
            UpdateProjection(world, start, direction);
        }

        public void UpdateProjection(World world, Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
            _world = world;
            _alpha = MinAlpha;
            DetermineLength();
            UpdateAlpha();
        }

        internal static Random Random = new Random();

        // Simple randomized flicker effect
        private void UpdateAlpha() {
            double rand = Random.NextDouble();
            if ( rand < .2f && _alpha > MinAlpha) {
                _alpha = Math.Max(MinAlpha, _alpha - .1f);
            } else if ( rand > .8f && _alpha < MaxAlpha ) {
                _alpha = Math.Min(MaxAlpha, _alpha + .1f);
            }
        }

        private void DetermineLength() {

            // Don't forget to invert the y coordinate because of the differing y axes
            Vector2 diff = new Vector2((float) Math.Cos(_angle) * MaxRange, (float) -Math.Sin(_angle) * MaxRange);
            Vector2 end = _start + diff;
            Vector2 angle = diff;
            angle.Normalize();

            float closestFraction = 1;
            Vector2 closestPoint = end;
            Vector2 closestCorner = end;
            _projecting = false;
            _world.RayCast((fixture, point, normal, fraction) => {
                if ( fraction < closestFraction && 
                     ((fixture.GetUserData().IsDoor && !fixture.GetUserData().Door.IsOpen())
                     || (fixture.GetUserData().IsTerrain && !fixture.GetUserData().IsUserTool)) ) {
                    closestFraction = fraction;
                    closestPoint = point;
                    // back up the ray just a tad
                    Vector2 less = _start + (diff * fraction) - (angle * .02f);
                    closestCorner = Region.GetContainingTile(less);
                    _projecting = true;
                }
                return 1;
            }, _start, end);

            _end = closestPoint;
            _cubeCorner = closestCorner;

            _legalPlacement = !EnceladusGame.EntitiesOverlapping(new AABB(_cubeCorner + new Vector2(.01f), _cubeCorner + new Vector2(TileLevel.TileSize - .02f)));
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Color color = _legalPlacement ? SolidColorEffect.DisabledColor : Color.PaleVioletRed;

            if ( _projecting ) {
                // Draw the cube itself
                Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
                spriteBatch.Draw(Image, displayPosition, null, color * _alpha,
                                 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);
            }

            // Draw the projector beam
            Vector2 origin = new Vector2(0, ProjectionImage.Height / 2);

            Vector2 beamEnd = _cubeCorner + new Vector2(.5f);
            Vector2 diff = (beamEnd - _start);
            float length = diff.Length();

            // Don't draw the beam if it's too close to the player, since it looks funny
            if ( length > 2 ) {
                float unitLegth = ConvertUnits.ToSimUnits(ProjectionImage.Width);
                float lengthRatio = length / unitLegth;
                float widthRatio;
                switch ( _direction ) {
                    case Direction.Left:
                    case Direction.Right:
                    case Direction.Up:
                    case Direction.Down:
                        widthRatio = 1f;
                        break;
                    case Direction.UpLeft:
                    case Direction.UpRight:
                    case Direction.DownLeft:
                    case Direction.DownRight:
                        widthRatio = (float) Math.Sqrt(2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                float angle = (float) Math.Atan2(-diff.Y, diff.X);

                spriteBatch.Draw(ProjectionImage, ConvertUnits.ToDisplayUnits(_start), null,
                                 color * _alpha, -angle, origin, new Vector2(lengthRatio, widthRatio),
                                 SpriteEffects.None, 1.0f);
            }
        }

        public void Fire() {
            if ( _projecting && _legalPlacement ) {
                HolocubeBlock block = new HolocubeBlock(_world, _cubeCorner);
                EnceladusGame.Instance.Register(block);
                PlacedBlocks.Enqueue(block);
                if ( PlacedBlocks.Count > MaxNumBlocks ) {
                    PlacedBlocks.Dequeue().Destroy();
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
        private float _alpha;

        public HolocubeBlock(World world, Vector2 cubeCorner) {
            _cubeCorner = cubeCorner;
            _timeToLive = new Timer(SolidTimeMs + DissolveTimeMs);
            _block = BodyFactory.CreateRectangle(world, TileLevel.TileSize, TileLevel.TileSize, 1f, UserData.NewHolocube());
            _block.IsStatic = true;
            _block.CollisionCategories = EnceladusGame.TerrainCategory;
            _block.CollidesWith = Category.All;
            _block.Position = cubeCorner + new Vector2(TileLevel.TileSize / 2f);
            _alpha = 1f;

            Player.Instance.NotifyTerrainChange();

            _world = world;
        }

        public override void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(_cubeCorner);
            spriteBatch.Draw(Holocube.Image, displayPosition, null, SolidColorEffect.DisabledColor * _alpha,
                             0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1.0f);
        }

        public override void Update(GameTime gameTime) {
            _timeToLive.Update(gameTime);
            if ( !Disposed && _timeToLive.IsTimeUp() ) {
                Dispose();
            }
            UpdateAlpha();
        }

        // Simple randomized flicker effect
        private void UpdateAlpha() {
            double rand = Holocube.Random.NextDouble();
            double minAlpha = _timeToLive.TimeLeft > DissolveTimeMs ? .75f : .1f;
            double maxAlpha = _timeToLive.TimeLeft > DissolveTimeMs ? 1f : _timeToLive.TimeLeft / DissolveTimeMs * .75f;
            if ( rand < .3f ) {
                _alpha = (float) Math.Max(minAlpha, _alpha - .05f);
            } else if ( rand > .7f ) {
                _alpha = (float) Math.Min(maxAlpha, _alpha + .05f);
            }
        }


        public override Vector2 Position {
            get { return _cubeCorner; }
        }

        public override void Dispose() {
            if ( _block != null ) {
                _block.Dispose();
                Player.Instance.NotifyTerrainChange();
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
                                                                 SolidColorEffect.DisabledColor * _alpha, null,
                                                                 _cubeCorner + new Vector2(TileLevel.TileSize / 2), 4,
                                                                 4f));
        }
    }
}

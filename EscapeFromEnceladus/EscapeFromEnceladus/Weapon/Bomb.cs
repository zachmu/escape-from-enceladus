using System;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// The bomb isn't really a projectile since it just sits there, but it does
    /// everything projectiles do, like hurt enemies and damage terrain.
    /// </summary>
    public class Bomb : Projectile, IGameEntity {

        private World _world;

        public const float Width = .375f;
        public static readonly float Height = ConvertUnits.ToSimUnits(26f);
        private const float ExplodeRadius = 1f;
        private bool _exploded;

        private const int NumFrames = 12;
        private const int FrameTime = 150;
        private static readonly Texture2D[] Animation = new Texture2D[NumFrames];
        private const int ExplodeFrame = 10;

        private int _animationFrame = 0;
        private long _timeSinceLastAnimationUpdate;
        private Texture2D _image;
        private Texture2D Image {
            get { return _image; }
            set {
                _image = value;
                _timeSinceLastAnimationUpdate = 0;
            }
        }

        public const int Flags = 4;

        public Bomb(Vector2 position, World world, Direction direction)
            : base(position, world, direction, 0, Width, Height) {
            Image = Animation[0];
            _timeToLiveMs = FrameTime * (NumFrames + 1);
            _world = world;
            _body.CollidesWith = EnceladusGame.TerrainCategory;
            _body.CollisionCategories = EnceladusGame.TerrainCategory;
            _body.IsStatic = false;
            _body.IgnoreGravity = false;
        }

        public static void LoadContent(ContentManager content) {
            for ( int i = 0; i < NumFrames; i++ ) {
                Animation[i] = content.Load<Texture2D>(String.Format("Character/Bomb/Bomb{0:0000}", i));
            }
        }

        public override int DestructionFlags {
            get { return Flags; }
        }

        public override int BaseDamage {
            get { return 3; }
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D camera) {
            // Draw origin is center
            Vector2 position = _body.Position;

            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            if ( _direction == Direction.Left ) {
                displayPosition += new Vector2(12, 0);
            } else {
                displayPosition -= new Vector2(12, 0);                
            }
            spriteBatch.Draw(Image,
                             new Rectangle((int) displayPosition.X, (int) displayPosition.Y, Image.Width, Image.Height),
                             null, SolidColorEffect.DisabledColor, 0f, new Vector2(Image.Width / 2, Image.Height / 2),
                             _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        }

        public override void Update(GameTime gameTime) {

            _timeSinceLastAnimationUpdate += gameTime.ElapsedGameTime.Milliseconds;

            if ( _animationFrame >= ExplodeFrame && !_exploded ) {
                Explode();
            }

            int frameTime = FrameTime;
            if (_animationFrame >= ExplodeFrame) {
                frameTime = 0;
            }

            if ( _timeSinceLastAnimationUpdate > frameTime ) {
                if (_animationFrame >= NumFrames) {
                    _animationFrame = NumFrames - 1;
                }
                Image = Animation[_animationFrame++];
            }

            base.Update(gameTime);
        }

        public bool DrawAsOverlay {
            get { return false; }
        }

        public bool UpdateInMode(Mode mode) {
            return mode == Mode.NormalControl; 
        }

        private void Explode() {
            AABB aabb = new AABB(_body.Position - new Vector2(0, TileLevel.TileSize / 2 - Height / 2), ExplodeRadius,
                                 ExplodeRadius);
            _world.QueryAABB(fixture => {
                if ( fixture.GetUserData().IsDestructibleRegion ) {                    
                    var hitTile = TileLevel.CurrentLevel.GetTile(fixture.GetUserData().Destruction.Position);
                    TileLevel.CurrentLevel.TileHitBy(hitTile, this);
                } else if ( fixture.GetUserData().IsEnemy ) {
                    fixture.GetUserData().Enemy.HitBy(this);
                }
                return true;
            }, ref aabb);

            SoundEffectManager.Instance.PlaySoundEffect("bomb");
            _body.IgnoreGravity = true;

            _exploded = true;
        }

        /// <summary>
        /// Don't collide with anything
        /// </summary>
        protected override OnCollisionEventHandler CollisionHandler() {
            return (a, b, contact) =>
                   true;
        }
    }
}

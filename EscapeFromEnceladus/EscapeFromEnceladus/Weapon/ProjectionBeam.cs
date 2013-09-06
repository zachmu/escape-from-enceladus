using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {

    /// <summary>
    /// Weapon projection beam, one tile wide.
    /// </summary>
    public class ProjectionBeam : GameEntityAdapter {

        public static Texture2D ProjectionImage;

        public static void LoadContent(ContentManager cm) {
            ProjectionImage = cm.Load<Texture2D>("Projectile/Holocube0001");
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 start, Vector2 projectedTileCorner, Direction direction, Color color) {
            // Draw the projector beam
            Vector2 origin = new Vector2(0, ProjectionImage.Height / 2);

            Vector2 beamEnd = projectedTileCorner + new Vector2(TileLevel.TileSize / 2f);
            Vector2 diff = (beamEnd - start);
            float length = diff.Length();

            float unitLegth = ConvertUnits.ToSimUnits(ProjectionImage.Width);
            float lengthRatio = length / unitLegth;
            float widthRatio;
            switch ( direction ) {
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

            spriteBatch.Draw(ProjectionImage, ConvertUnits.ToDisplayUnits(start), null,
                             color, -angle, origin, new Vector2(lengthRatio, widthRatio),
                             SpriteEffects.None, 1.0f);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Entity;
using Enceladus.Farseer;
using Enceladus.Map;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Weapon {
    
    /// <summary>
    /// A projected (not yet real) weapon or tool placement, aligned to the world grid.
    /// </summary>
    public class Projection {

        private Vector2 _start;
        private float _angle;
        private Direction _direction;
        private Vector2 _cubeCorner;
        private World _world;
        private bool _projecting;
        private bool _legalPlacement;
        private const int MaxRange = 30;

        public Projection(World world) {
            _world = world;
        }

        public bool IsLegalPlacement {
            get { return _legalPlacement; }
        }

        public bool IsProjecting {
            get { return _projecting; }
        }

        public Vector2 CubeCorner {
            get { return _cubeCorner; }
        }

        public float Angle {
            get { return _angle; }
        }

        public Vector2 Start {
            get { return _start; }
        }

        public Direction Direction {
            get { return _direction; }
        }

        public void UpdateProjection(Vector2 start, Direction direction) {
            _start = start;
            _direction = direction;
            _angle = Projectile.GetAngle(direction);
            DetermineLength();
        }

        private void DetermineLength() {

            // Don't forget to invert the y coordinate because of the differing y axes
            Vector2 diff = new Vector2((float) Math.Cos(Angle) * MaxRange, (float) -Math.Sin(Angle) * MaxRange);
            Vector2 end = Start + diff;

            _cubeCorner = _world.RayCastTileCorner(Start, end);
            if ( CubeCorner == Vector2.Zero ) {
                _projecting = false;
                _cubeCorner = end;
            } else {
                _projecting = true;
            }

            _legalPlacement = !EnceladusGame.EntitiesOverlapping(new AABB(CubeCorner + new Vector2(.01f), CubeCorner + new Vector2(TileLevel.TileSize - .02f)));
        }
    }
}

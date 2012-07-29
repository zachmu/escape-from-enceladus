using Arena.Farseer;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Arena.Entity {
    public class Entity {

        protected World _world;
        protected Body _body;

        protected bool _isStanding;

        protected virtual bool IsStanding {
            get { return _isStanding; }
            set { _isStanding = value; }
        }

        protected Color _flashColor = Color.OrangeRed;
        protected bool _drawSolidColor;
        protected int _flashTime;
        private const int flashChangeMs = 32;

        protected bool _isTouchingCeiling;

        protected virtual bool IsTouchingCeiling {
            get { return _isTouchingCeiling; }
            set { _isTouchingCeiling = value; }
        }

        protected int _ignoreTerrainCollisionsNextNumFrames = 0;

        protected void UpdateStanding() {
            if ( _ignoreTerrainCollisionsNextNumFrames > 0 ) {
                return;
            }

            bool isStanding = false;
            bool isTouchingCeiling = false;

            var contactEdge = _body.ContactList;
            FixedArray2<Vector2> points;
            while ( contactEdge != null ) {
                if ( contactEdge.Contact.IsTouching() && contactEdge.Other.GetUserData().IsTerrain ) {
                    Vector2 normal;
                    contactEdge.Contact.GetWorldManifold(out normal, out points);
                    if ( normal.Y < -.8 ) {
                        isStanding = true;
                    } else if ( normal.Y > .8 ) {
                        isTouchingCeiling = true;
                    }
                }
                contactEdge = contactEdge.Next;
            }

            IsStanding = isStanding;
            IsTouchingCeiling = isTouchingCeiling;
        }

        protected void UpdateFlash(GameTime gameTime) {
            if ( _flashTime > 0 ) {
                _flashTime -= gameTime.ElapsedGameTime.Milliseconds;
                _drawSolidColor = (_flashTime / flashChangeMs) % 2 == 1;
            }
            if ( _flashTime <= 0 ) {
                _drawSolidColor = false;
            }
        }
    }
}
using Arena.Farseer;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Arena.Entity {
    public class Entity {

        protected World _world;
        protected Body _body;
        private readonly Fixture _floorSensor;
        
        protected bool _isStanding;
        protected virtual bool IsStanding {
            get { return _isStanding; }
            set { _isStanding = value; }
        }

        protected bool _isTouchingCeiling;
        protected virtual bool IsTouchingCeiling {
            get { return _isTouchingCeiling; }
            set { _isTouchingCeiling = value; }
        }

        protected void UpdateStanding() {
            bool isStanding = false;
            bool isTouchingCeiling = false;

            var contactEdge = _body.ContactList;
            while ( contactEdge != null ) {
                if ( contactEdge.Contact.IsTouching() && contactEdge.Other.GetUserData().IsTerrain ) {
                    FixedArray2<Vector2> points;
                    Vector2 normal;
                    contactEdge.Contact.GetWorldManifold(out normal, out points);
                    if ( normal.Y < -.8 ) {
                        isStanding = true;
                    } else if (normal.Y > .8) {
                        isTouchingCeiling = true;
                    }
                }
                contactEdge = contactEdge.Next;
            }

            IsStanding = isStanding;
            IsTouchingCeiling = isTouchingCeiling;
        }
    }
}
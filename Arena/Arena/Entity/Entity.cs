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

        protected void UpdateStanding() {
            var contactEdge = _body.ContactList;
            while ( contactEdge != null ) {
                if ( contactEdge.Contact.FixtureA.Body.GetUserData().IsTerrain
                     || contactEdge.Contact.FixtureB.Body.GetUserData().IsTerrain ) {
                    FixedArray2<Vector2> points;
                    Vector2 normal;
                    contactEdge.Contact.GetWorldManifold(out normal, out points);
                    if ( normal.Y < -.8 ) {
                        IsStanding = true;
                        return;
                    }
                }
                contactEdge = contactEdge.Next;
            }

            IsStanding = false;
        }
    }
}
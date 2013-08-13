using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using Microsoft.Xna.Framework;

namespace Enceladus.Farseer {

    public static class FarseerExtensions {
        
        /// <summary>
        /// Returns the custom user data for the body given, 
        /// returning an empty object if none was established.
        /// </summary>
        public static UserData GetUserData(this Body body) {
            return (UserData) (body == null ? new UserData() : body.UserData ?? new UserData());
        }

        /// <summary>
        /// Returns the custom user data for the body of the fixture given, 
        /// returning an empty object if none was established. A null fixture 
        /// has empty user data.
        /// </summary>
        public static UserData GetUserData(this Fixture fixture) {
            return (UserData) (fixture == null ? new UserData() : fixture.UserData ?? fixture.Body.UserData ?? new UserData());
        }

        public static AABB GetTransformedAABB(this Fixture fixture, int childIndex) {
            AABB fab;
            fixture.GetAABB(out fab, childIndex);
            return new AABB(fab.LowerBound + fixture.Body.Position, fab.UpperBound + fixture.Body.Position);
        }

        /// <summary>
        /// Returns the closest fixture in the given ray cast, or null if no fixture is in the ray.
        /// </summary>
        public static Fixture RayCastClosest(this World world, Vector2 start, Vector2 end) {
            float closestFraction = 1;
            Fixture closestFixture = null;
            world.RayCast((fixture, point, normal, fraction) => {
                if ( fraction < closestFraction ) {
                    closestFraction = fraction;
                    closestFixture = fixture;
                }
                return 1;
            }, start, end);
            return closestFixture;
        }

        /// <summary>
        /// Returns the normal on the player's body resulting from the contact given, 
        /// of which the player body must be one fixture.
        /// </summary>
        public static Vector2 GetPlayerNormal(this Contact contact, Body player) {
            Vector2 normal;
            FixedArray2<Vector2> points;
            contact.GetWorldManifold(out normal, out points);
            return player == contact.FixtureA.Body ? normal*-1 : normal;
        }

    }
}

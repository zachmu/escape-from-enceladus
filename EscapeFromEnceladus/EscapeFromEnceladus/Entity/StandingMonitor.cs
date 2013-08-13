using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enceladus.Farseer;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;

namespace Enceladus.Entity {

    /// <summary>
    /// Class to help track whether an entity is in contact with the ground or ceiling
    /// </summary>
    public class StandingMonitor {
        
        public bool IsStanding { get; set; }
        public bool IsTouchingCeiling { get; set; }
        public int IgnoreStandingUpdatesNextNumFrames { get; set; }

        /// <summary>
        /// Updates bookkeeping counters
        /// </summary>
        public void UpdateCounters() {
            if ( IgnoreStandingUpdatesNextNumFrames > 0 ) {
                IgnoreStandingUpdatesNextNumFrames--;
            }
        }

        /// <summary>
        /// Updates the standing and ceiling status using the body's current contacts, 
        /// given the location of its lowest point.
        /// </summary>
        public void UpdateStanding(Body body, World world, Vector2 standingLocation, float width) {

            bool isStanding = false;
            bool isTouchingCeiling = false;

            var contactEdge = body.ContactList;
            while ( contactEdge != null ) {
                if ( !contactEdge.Contact.FixtureA.IsSensor && !contactEdge.Contact.FixtureB.IsSensor &&
                     (contactEdge.Contact.IsTouching() &&
                      (contactEdge.Other.GetUserData().IsTerrain || contactEdge.Other.GetUserData().IsDoor)) ) {
                    Vector2 normal = contactEdge.Contact.GetPlayerNormal(body);
                    if ( normal.Y < -.8 ) {
                        isStanding = true;
                    } else if ( normal.Y > .8 ) {
                        isTouchingCeiling = true;
                    }
                }
                contactEdge = contactEdge.Next;
            }

            /*
             * If we didn't find any contact points, it could mean that it's because Box2d isn't playing nicely with
             * a newly created body (as when a tile reappears).  In that case, try to find the ground under our feet
             * with a ray cast.
             */
            float delta = .1f;
            if ( !isStanding ) {
                foreach ( Vector2 start in new Vector2[] {
                    standingLocation + new Vector2(-width / 2, -delta),
                    standingLocation + new Vector2(width / 2, -delta),
                } ) {
                    world.RayCast((fixture, point, normal, fraction) => {
                        if ( fixture.GetUserData().IsTerrain ) {
                            isStanding = true;
                            return 0;
                        }
                        return -1;
                    }, start, start + new Vector2(0, 2 * delta));
                }
            }

            if ( IgnoreStandingUpdatesNextNumFrames <= 0 ) {
                IsStanding = isStanding;
            }

            IsTouchingCeiling = isTouchingCeiling;
        }
    }
}

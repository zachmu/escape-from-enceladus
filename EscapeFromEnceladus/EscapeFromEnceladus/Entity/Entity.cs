using System;
using Enceladus.Farseer;
using FarseerPhysics.Collision;
using FarseerPhysics.Collision.Shapes;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Enceladus.Entity {
    public abstract class Entity {

        protected World _world;
        protected Body _body;

        public Vector2 Position { get { return _body.Position; } }

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

        // Returns where this entity is standing
        protected abstract Vector2 GetStandingLocation();

        protected int _ignoreStandingUpdatesNextNumFrames = 0;

        /// <summary>
        /// Updates the standing and ceiling status using the body's current contacts.
        /// </summary>
        protected void UpdateStanding() {

            bool isStanding = false;
            bool isTouchingCeiling = false;

            var contactEdge = _body.ContactList;
            while ( contactEdge != null ) {
                if ( !contactEdge.Contact.FixtureA.IsSensor && !contactEdge.Contact.FixtureB.IsSensor &&
                     (contactEdge.Contact.IsTouching() &&
                      (contactEdge.Other.GetUserData().IsTerrain || contactEdge.Other.GetUserData().IsDoor)) ) {
                    Vector2 normal = contactEdge.Contact.GetPlayerNormal(_body);
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
                Vector2 feet = GetStandingLocation();
                Vector2 start = feet + new Vector2(0, -delta);
                _world.RayCast((fixture, point, normal, fraction) => {
                    if ( fixture.GetUserData().IsTerrain ) {
                        isStanding = true;
                        return 0;
                    }
                    return -1;
                }, start, start + new Vector2(0, 2*delta));
            }

            if ( _ignoreStandingUpdatesNextNumFrames <= 0 ) {
                IsStanding = isStanding;
            }

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
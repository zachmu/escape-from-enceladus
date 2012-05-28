using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using FarseerPhysics.SamplesFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Test {
    internal class Player {
        private const float CharacterHeight = 2f;
        private const float CharacterWidth = 1f;
        private const int MaxJumpingFrames = 50;

        private const float JumpMultiplier = .4f;
        private const float RunMultiplier = .4f;

        private enum Direction {
            Left,
            Right
        };

        private Direction _direction = Direction.Right;

        /// <summary>
        /// How many frames the character has been holding down the jump button
        /// </summary>
        private int _jumpingFrames = -1;

        public Texture2D Image { get; set; }

        private readonly Body _body;

        public Vector2 Position {
            get { return _body.Position; }
        }

        public Player(Vector2 position, World world) {
            _body = BodyFactory.CreateRectangle(world, CharacterWidth, CharacterHeight, 10f);
            _body.IsStatic = false;
            _body.Restitution = 0.0f;
            _body.Friction = 1f;
            _body.Position = position;
            _body.FixedRotation = true;
        }

        public void Draw(SpriteBatch spriteBatch, Camera2D c) {
            Vector2 position = _body.Position;
            position.X -= CharacterWidth / 2;
            position.Y -= CharacterHeight / 2;
            Vector2 displayPosition = ConvertUnits.ToDisplayUnits(position);
            float height = ConvertUnits.ToDisplayUnits(CharacterHeight);
            float width = ConvertUnits.ToDisplayUnits(CharacterWidth);
            spriteBatch.Draw(Image, new Rectangle((int) displayPosition.X, (int) displayPosition.Y, (int) width, (int) height), 
                null, Color.White, 0f, new Vector2(), _direction == Direction.Right ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        }

        public void ApplyForce(Vector2 force) {
            _body.ApplyLinearImpulse(force);
        }

        public void Update() {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            Vector2 leftStick = gamePadState.ThumbSticks.Left;

            Vector2 adjustedDelta = new Vector2(leftStick.X, 0);

            foreach ( var pressedKey in keyboardState.GetPressedKeys() ) {
                switch ( pressedKey ) {
                    case Keys.Left:
                        adjustedDelta.X += -1f;
                        break;
                    case Keys.Right:
                        adjustedDelta.X += 1f;
                        break;
                    case Keys.Down:
                        adjustedDelta.Y += 1f;
                        break;
                    case Keys.Up:
                        adjustedDelta.Y += -1f;
                        break;
                }
            }

            HandleJump(gamePadState);

            if (gamePadState.IsButtonDown(Buttons.X)) {
                
            }

            HandleRun(adjustedDelta);

        }

        private void HandleRun(Vector2 adjustedDelta) {
            if ( adjustedDelta.X < 0 ) {
                _direction = Direction.Left;
                _body.LinearVelocity += adjustedDelta * RunMultiplier;
            } else if ( adjustedDelta.X > 0 ) {
                _direction = Direction.Right;
                _body.LinearVelocity += adjustedDelta * RunMultiplier;
            } else {
                _body.LinearVelocity = new Vector2(0, _body.LinearVelocity.Y);
            }
        }

        private void HandleJump(GamePadState gamePadState) {
            if ( gamePadState.IsButtonDown(Buttons.A) ) {
                if ( _jumpingFrames > 0 && _jumpingFrames < MaxJumpingFrames ) {
                    _body.ApplyLinearImpulse(new Vector2(0, (-MaxJumpingFrames + _jumpingFrames) * JumpMultiplier));
                    _jumpingFrames++;
                } else {
                    ContactEdge contactEdge = _body.ContactList;
                    if ( contactEdge != null ) {
                        Vector2 normal;
                        FixedArray2<Vector2> points;
                        while ( contactEdge.Next != null ) {
                            if ( contactEdge.Contact.IsTouching() ) {
                                contactEdge.Contact.GetWorldManifold(out normal, out points);
                                // A normal of -1 in the y direction indicates standing on something
                                if ( normal.Y == -1 ) {
                                    _body.ApplyLinearImpulse(new Vector2(0, -100));
                                    _jumpingFrames = 1;
                                }
                            }
                            contactEdge = contactEdge.Next;
                        }
                    }
                }
            } else {
                _jumpingFrames = -1;
            }
        }
    }
}

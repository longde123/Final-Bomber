﻿using System.Diagnostics;
using FBLibrary.Core;
using Final_Bomber.Entities;
using Final_Bomber.Sprites;
using Final_Bomber.WorldEngine;
using Microsoft.Xna.Framework;

namespace Final_Bomber.Core.Players
{
    internal class OnlinePlayer : Player
    {
        public OnlinePlayer(int id)
            : base(id)
        {
        }

        public void Update(GameTime gameTime, Map map, int[,] hazardMap)
        {
            base.Update(gameTime, map, hazardMap);
        }

        protected override void Move(GameTime gameTime, Map map, int[,] hazardMap)
        {
            Debug.Print("Look Direction: " + CurrentDirection);
            Vector2 motionVector = Vector2.Zero;

            switch (CurrentDirection)
            {
                case LookDirection.Down:
                    Sprite.CurrentAnimation = AnimationKey.Down;
                    motionVector.Y = 1;
                    break;
                case LookDirection.Left:
                    Sprite.CurrentAnimation = AnimationKey.Left;
                    motionVector.X = -1;
                    break;
                case LookDirection.Right:
                    Sprite.CurrentAnimation = AnimationKey.Right;
                    motionVector.X = 1;
                    break;
                case LookDirection.Up:
                    Sprite.CurrentAnimation = AnimationKey.Up;
                    motionVector.Y = -1;
                    break;
            }

            if (motionVector != Vector2.Zero)
            {
                var dt = (float) gameTime.ElapsedGameTime.TotalSeconds;
                Sprite.IsAnimating = true;
                Sprite.Position += motionVector*(Sprite.Speed*dt);
            }
            else
            {
                Sprite.IsAnimating = false;
            }

            #region Moving action

            /*
            // If the human player wants to move
            if (motion != Vector2.Zero)
            {
                this.IsMoving = true;
                Sprite.IsAnimating = true;
                motion.Normalize();

                Vector2 nextPosition = Sprite.Position + motion * Sprite.Speed;
                Point nextPositionCell = Engine.VectorToCell(nextPosition, Sprite.Dimension);

                #region Moving of the player

                // We move the player
                Sprite.Position += motion * Sprite.Speed;

                // If the player want to go to top...
                if (motion.Y == -1)
                {
                    // ...  and that there is a wall
                    if (WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
                    {
                        // If he is more on the left side, we lag him to the left
                        if (IsMoreLeftSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y - 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
                                Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                        }
                        else if (IsMoreRightSide() &&
                                 !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y - 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                                Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                        }
                    }
                    // ... and that there is no wall
                    else
                    {
                        // If he is more on the left side
                        if (IsMoreLeftSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                        }
                        // If he is more on the right side
                        else if (IsMoreRightSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                        }
                    }
                }
                // If the player want to go to bottom and that there is a wall
                else if (motion.Y == 1)
                {
                    // Wall at the bottom ?
                    if (WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                    {
                        // If he is more on the left side, we lag him to the left
                        if (IsMoreLeftSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y + 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
                                Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                        }
                        else if (IsMoreRightSide() &&
                                 !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y + 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                                Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                        }
                    }
                    else
                    {
                        // If he is more on the left side
                        if (IsMoreLeftSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                        }
                        // If he is more on the right side
                        else if (IsMoreRightSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                        }
                    }
                }
                // If the player want to go to left and that there is a wall
                else if (motion.X == -1)
                {
                    if (WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
                    {
                        // If he is more on the top side, we lag him to the top
                        if (IsMoreTopSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y - 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                        }
                        else if (IsMoreBottomSide() &&
                                 !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y + 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                        }
                    }
                    else
                    {
                        // If he is more on the top side, we lag him to the bottom
                        if (IsMoreTopSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                        }
                        else if (IsMoreBottomSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                        }
                    }
                }
                // If the player want to go to right and that there is a wall
                else if (motion.X == 1)
                {
                    if (WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                    {
                        // If he is more on the top side, we lag him to the top
                        if (IsMoreTopSide() && !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y - 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                        }
                        else if (IsMoreBottomSide() &&
                                 !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y + 1)))
                        {
                            if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                        }
                    }
                    else
                    {
                        // If he is more on the top side, we lag him to the top
                        if (IsMoreTopSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                        }
                        else if (IsMoreBottomSide())
                        {
                            Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                        }
                    }
                }
                #endregion

                //ComputeWallCollision();
            }
            else
            {
                this.IsMoving = false;
                Sprite.IsAnimating = false;
            }
            */

            //UpdatePlayerPosition();

            #region Bomb

            /*
            if ((HasBadItemEffect && BadItemEffect == BadItemEffect.BombDrop) ||
                ((Config.PlayersUsingController[Id] && InputHandler.ButtonDown(Buttons[4], PlayerIndex.One)) || InputHandler.KeyPressed(Keys[4]) &&
                (!HasBadItemEffect || (HasBadItemEffect && BadItemEffect != BadItemEffect.NoBomb))))
            {
                if (this.CurrentBombAmount > 0)
                {
                    var bo = FinalBomber.Instance.GamePlayScreen.BombList.Find(b => b.Sprite.CellPosition == this.Sprite.CellPosition);
                    if (bo == null)
                    {
                        this.CurrentBombAmount--;
                        var bomb = new Bomb(this.Id, Sprite.CellPosition, this.Power, this.BombTimer, this.Sprite.Speed);

                        FinalBomber.Instance.GamePlayScreen.AddBomb(bomb);
                    }
                }
            }
            */

            #endregion

            #endregion
        }
    }
}
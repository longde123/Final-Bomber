﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using FBLibrary.Core;
using Final_Bomber.Controls;
using Final_Bomber.Core;
using Final_Bomber.Core.Entities;
using Final_Bomber.Entities;
using Final_Bomber.Screens;
using Final_Bomber.Sprites;
using Final_Bomber.TileEngine;
using Final_Bomber.WorldEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Final_Bomber.Entities
{
    public abstract class Player : BasePlayer, IEntity
    {
        #region Field Region

        private readonly AnimatedSprite _playerDeathAnimation;
        protected TimeSpan BombTimerSaved;

        protected bool IsMoving;
        protected LookDirection PreviousLookDirection;

        // Bad item
        protected float SpeedSaved;
        private bool _cellTeleporting;
        private float _invincibleBlinkFrequency;
        private TimeSpan _invincibleBlinkTimer;
        private TimeSpan _invincibleTimer;

        public AnimatedSprite Sprite { get; protected set; }

        #endregion

        #region Property Region

        public Camera Camera { get; private set; }

        public bool InDestruction { get; private set; }

        public bool IsInvincible { get; private set; }

        public bool HasBadItemEffect { get; private set; }

        public BadItemEffect BadItemEffect { get; private set; }

        public TimeSpan BadItemTimer { get; private set; }

        public TimeSpan BadItemTimerLenght { get; private set; }

        #endregion

        #region Constructor Region

        protected Player(int id) : base(id)
        {
            const int animationFramesPerSecond = 10;
            var animations = new Dictionary<AnimationKey, Animation>();

            var animation = new Animation(4, 23, 23, 0, 0, animationFramesPerSecond);
            animations.Add(AnimationKey.Down, animation);

            animation = new Animation(4, 23, 23, 0, 23, animationFramesPerSecond);
            animations.Add(AnimationKey.Left, animation);

            animation = new Animation(4, 23, 23, 0, 46, animationFramesPerSecond);
            animations.Add(AnimationKey.Right, animation);

            animation = new Animation(4, 23, 23, 0, 69, animationFramesPerSecond);
            animations.Add(AnimationKey.Up, animation);

            var spriteTexture = FinalBomber.Instance.Content.Load<Texture2D>("Graphics/Characters/player1");

            Sprite = new AnimatedSprite(spriteTexture, animations, Vector2.Zero);
            Sprite.ChangeFramesPerSecond(animationFramesPerSecond);
            Sprite.Speed = GameConfiguration.BasePlayerSpeed;

            var playerDeathTexture = FinalBomber.Instance.Content.Load<Texture2D>("Graphics/Characters/player1Death");
            animation = new Animation(8, 23, 23, 0, 0, 4);
            _playerDeathAnimation = new AnimatedSprite(playerDeathTexture, animation, Sprite.Position)
            {
                IsAnimating = false
            };

            IsMoving = false;
            InDestruction = false;
            IsInvincible = true;

            _invincibleTimer = GameConfiguration.PlayerInvincibleTimer;
            _invincibleBlinkFrequency = Config.InvincibleBlinkFrequency;
            _invincibleBlinkTimer = TimeSpan.FromSeconds(_invincibleBlinkFrequency);
            
            PreviousLookDirection = CurrentDirection;

            // Bad item
            HasBadItemEffect = false;
            BadItemTimer = TimeSpan.Zero;
            BadItemTimerLenght = TimeSpan.Zero;
        }

        #endregion

        #region XNA Method Region

        public void Update(GameTime gameTime, Map map, int[,] hazardMap)
        {
            if (IsAlive && !InDestruction)
            {
                PreviousLookDirection = CurrentDirection;

                Sprite.Update(gameTime);

                #region Invincibility

                if (!GameConfiguration.Invincible && IsInvincible)
                {
                    if (_invincibleTimer >= TimeSpan.Zero)
                    {
                        _invincibleTimer -= gameTime.ElapsedGameTime;
                        if (_invincibleBlinkTimer >= TimeSpan.Zero)
                            _invincibleBlinkTimer -= gameTime.ElapsedGameTime;
                        else
                            _invincibleBlinkTimer = TimeSpan.FromSeconds(_invincibleBlinkFrequency);
                    }
                    else
                    {
                        _invincibleTimer = GameConfiguration.PlayerInvincibleTimer;
                        IsInvincible = false;
                    }
                }

                #endregion

                #region Moving

                Move(gameTime, map, hazardMap);

                #endregion

                #region Bomb

                #region Push a bomb

                if (Config.PlayerCanPush)
                {
                    if (CurrentDirection != LookDirection.Idle)
                    {
                        Point direction = Point.Zero;
                        switch (CurrentDirection)
                        {
                            case LookDirection.Up:
                                direction = new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1);
                                break;
                            case LookDirection.Down:
                                direction = new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1);
                                break;
                            case LookDirection.Left:
                                direction = new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y);
                                break;
                            case LookDirection.Right:
                                direction = new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y);
                                break;
                        }
                        Bomb bomb = BombAt(direction);
                        if (bomb != null)
                            bomb.ChangeDirection(CurrentDirection, Id);
                    }
                }

                #endregion

                #endregion

                #region Item

                /*
                if (FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Item)
                {
                    var item = (Item)(FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y]);
                    if (!item.InDestruction)
                    {
                        if (!HasBadItemEffect || (HasBadItemEffect && item.Type != ItemType.BadItem))
                        {
                            item.ApplyItem(this);
                            FinalBomber.Instance.GamePlayScreen.ItemPickUpSound.Play();
                            item.Remove();
                        }
                    }
                }
                */
                // Have caught a bad item
                if (HasBadItemEffect)
                {
                    BadItemTimer += gameTime.ElapsedGameTime;
                    if (BadItemTimer >= BadItemTimerLenght)
                    {
                        RemoveBadItem();
                    }
                }

                #endregion

                #region Teleporter

                /*
                if (!_cellTeleporting && FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Teleporter)
                {
                    var teleporter = (Teleporter)(FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y]);

                    teleporter.ChangePosition(this);
                    _cellTeleporting = true;
                }
                */

                #endregion
            }

                #region Death

            else if (_playerDeathAnimation.IsAnimating)
            {
                _playerDeathAnimation.Update(gameTime);

                if (_playerDeathAnimation.Animation.CurrentFrame == _playerDeathAnimation.Animation.FrameCount - 1)
                    Remove();
            }
                #endregion

                #region Edge wall gameplay

            else if (OnEdge &&
                     (!Config.ActiveSuddenDeath ||
                      (Config.ActiveSuddenDeath && !FinalBomber.Instance.GamePlayScreen.SuddenDeath.HasStarted)))
            {
                Sprite.Update(gameTime);

                MoveFromEdgeWall();
            }

            #endregion

            #region Camera

            /*
            Camera.Update(gameTime);

            if (Config.Debug)
            {
                if (InputHandler.KeyDown(Microsoft.Xna.Framework.Input.Keys.PageUp) ||
                    InputHandler.ButtonReleased(Buttons.LeftShoulder, PlayerIndex.One))
                {
                    Camera.ZoomIn();
                    if (Camera.CameraMode == CameraMode.Follow)
                        Camera.LockToSprite(Sprite);
                }
                else if (InputHandler.KeyDown(Microsoft.Xna.Framework.Input.Keys.PageDown))
                {
                    Camera.ZoomOut();
                    if (Camera.CameraMode == CameraMode.Follow)
                        Camera.LockToSprite(Sprite);
                }
                else if (InputHandler.KeyDown(Microsoft.Xna.Framework.Input.Keys.End))
                {
                    Camera.ZoomReset();
                }

                if (InputHandler.KeyReleased(Microsoft.Xna.Framework.Input.Keys.F))
                {
                    Camera.ToggleCameraMode();
                    if (Camera.CameraMode == CameraMode.Follow)
                        Camera.LockToSprite(Sprite);
                }

                if (Camera.CameraMode != CameraMode.Follow)
                {
                    if (InputHandler.KeyReleased(Microsoft.Xna.Framework.Input.Keys.L))
                    {
                        Camera.LockToSprite(Sprite);
                    }
                }
            }

            if (IsMoving && Camera.CameraMode == CameraMode.Follow)
                Camera.LockToSprite(Sprite);
            */

            #endregion
        }

        public void Draw(GameTime gameTime)
        {
            var playerNamePosition = new Vector2(
                Sprite.Position.X + Engine.Origin.X + Sprite.Width/2f -
                ControlManager.SpriteFont.MeasureString(Name).X/2 + 5,
                Sprite.Position.Y + Engine.Origin.Y - 25 -
                ControlManager.SpriteFont.MeasureString(Name).Y/2);

            if ((IsAlive && !InDestruction) || OnEdge)
            {
                if (IsInvincible)
                {
                    if (_invincibleBlinkTimer > TimeSpan.FromSeconds(_invincibleBlinkFrequency*0.5f))
                    {
                        Sprite.Draw(gameTime, FinalBomber.Instance.SpriteBatch);
                        FinalBomber.Instance.SpriteBatch.DrawString(ControlManager.SpriteFont, Name, playerNamePosition,
                            Color.Black);
                    }
                }
                else
                {
                    Sprite.Draw(gameTime, FinalBomber.Instance.SpriteBatch);
                    FinalBomber.Instance.SpriteBatch.DrawString(ControlManager.SpriteFont, Name, playerNamePosition,
                        Color.Black);
                }
            }
            else
            {
                _playerDeathAnimation.Draw(gameTime, FinalBomber.Instance.SpriteBatch);
                FinalBomber.Instance.SpriteBatch.DrawString(ControlManager.SpriteFont, Name, playerNamePosition,
                    Color.Black);
            }
        }

        #endregion

        #region Method Region

        #region Private Method Region

        private Bomb BombAt(Point cell)
        {
            if (cell.X >= 0 && cell.Y >= 0 &&
                cell.X <
                FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel]
                    .Size.X &&
                cell.Y <
                FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel]
                    .Size.Y)
            {
                Bomb bomb = FinalBomber.Instance.GamePlayScreen.BombList.Find(b => b.Sprite.CellPosition == cell);
                return (bomb);
            }
            return null;
        }

        private void Invincibility()
        {
            IsInvincible = true;
            _invincibleTimer = GameConfiguration.PlayerInvincibleTimer;
            _invincibleBlinkFrequency = Config.InvincibleBlinkFrequency;
            _invincibleBlinkTimer = TimeSpan.FromSeconds(_invincibleBlinkFrequency);
        }

        #endregion

        #region Protected Method Region

        protected bool WallAt(Point cell)
        {
            return false;
            /*
            if (cell.X >= 0 && cell.Y >= 0 && cell.X < FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].Size.X &&
                cell.Y < FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].Size.Y)
                return (this.FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].CollisionLayer[cell.X, cell.Y]);
            else
                return false;
            */
        }

        protected bool IsMoreTopSide()
        {
            return Sprite.Position.Y < ((Sprite.CellPosition.Y*Engine.TileHeight) - (Sprite.Speed/2));
        }

        protected bool IsMoreBottomSide()
        {
            return Sprite.Position.Y > ((Sprite.CellPosition.Y*Engine.TileHeight) + (Sprite.Speed/2));
        }

        protected bool IsMoreLeftSide()
        {
            return Sprite.Position.X < ((Sprite.CellPosition.X*Engine.TileWidth) - (Sprite.Speed/2));
        }

        protected bool IsMoreRightSide()
        {
            return Sprite.Position.X > ((Sprite.CellPosition.X*Engine.TileWidth) + (Sprite.Speed/2));
        }


        protected virtual void Move(GameTime gameTime, Map map, int[,] hazardMap)
        {
        }

        protected void ComputeWallCollision()
        {
            #region Wall collisions

            Sprite.LockToMap();

            // -- Vertical check -- //
            // Is there a wall on the top ?
            if (WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
            {
                // Is there a wall on the bottom ?
                if (WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                {
                    // Top collision and Bottom collision
                    if ((CurrentDirection == LookDirection.Up && IsMoreTopSide()) ||
                        (CurrentDirection == LookDirection.Down && IsMoreBottomSide()))
                        Sprite.PositionY = Sprite.CellPosition.Y*Engine.TileHeight;
                }
                    // No wall at the bottom
                else
                {
                    // Top collision
                    if (CurrentDirection == LookDirection.Up && IsMoreTopSide())
                        Sprite.PositionY = Sprite.CellPosition.Y*Engine.TileHeight;
                }
            }
                // Wall only at the bottom
            else if (WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
            {
                // Bottom collision
                if (CurrentDirection == LookDirection.Down && IsMoreBottomSide())
                    Sprite.PositionY = Sprite.CellPosition.Y*Engine.TileHeight;
                    // To lag him
                else if (CurrentDirection == LookDirection.Down)
                {
                    if (IsMoreLeftSide())
                        Sprite.PositionX += Sprite.Speed;
                    else if (IsMoreRightSide())
                        Sprite.PositionX -= Sprite.Speed;
                }
            }

            // -- Horizontal check -- //
            // Is there a wall on the left ?
            if (WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
            {
                // Is there a wall on the right ?
                if (WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                {
                    // Left and right collisions
                    if ((CurrentDirection == LookDirection.Left && IsMoreLeftSide()) ||
                        (CurrentDirection == LookDirection.Right && IsMoreRightSide()))
                        Sprite.PositionX = Sprite.CellPosition.X*Engine.TileWidth - Engine.TileWidth/2 +
                                           Engine.TileWidth/2;
                }
                    // Wall only at the left
                else
                {
                    // Left collision
                    if (CurrentDirection == LookDirection.Left && IsMoreLeftSide())
                        Sprite.PositionX = Sprite.CellPosition.X*Engine.TileWidth - Engine.TileWidth/2 +
                                           Engine.TileWidth/2;
                }
            }
                // Wall only at the right
            else if (WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
            {
                // Right collision
                if (CurrentDirection == LookDirection.Right && IsMoreRightSide())
                    Sprite.PositionX = Sprite.CellPosition.X*Engine.TileWidth - Engine.TileWidth/2 + Engine.TileWidth/2;
            }


            // The player must stay in the map
            Sprite.PositionX = MathHelper.Clamp(Sprite.Position.X, Engine.TileWidth,
                (FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel
                    ].Size.X*Engine.TileWidth) - 2*Engine.TileWidth);
            Sprite.PositionY = MathHelper.Clamp(Sprite.Position.Y, Engine.TileHeight,
                (FinalBomber.Instance.GamePlayScreen.World.Levels[FinalBomber.Instance.GamePlayScreen.World.CurrentLevel
                    ].Size.Y*Engine.TileHeight) - 2*Engine.TileHeight);

            #endregion
        }

        protected void UpdatePlayerPosition()
        {
            #region Update player's position

            if (IsChangingCell())
            {
                if (FinalBomber.Instance.GamePlayScreen.World.Levels[
                    FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                    Board[PreviousCellPosition.X, PreviousCellPosition.Y] == this)
                {
                    FinalBomber.Instance.GamePlayScreen.World.Levels[
                        FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                        Board[PreviousCellPosition.X, PreviousCellPosition.Y] = null;
                }

                if (_cellTeleporting)
                    _cellTeleporting = false;
            }
            else
            {
                if (FinalBomber.Instance.GamePlayScreen.World.Levels[
                    FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                    Board[Sprite.CellPosition.X, Sprite.CellPosition.Y] == null)
                {
                    FinalBomber.Instance.GamePlayScreen.World.Levels[
                        FinalBomber.Instance.GamePlayScreen.World.CurrentLevel].
                        Board[Sprite.CellPosition.X, Sprite.CellPosition.Y] = this;
                }
            }

            #endregion
        }

        #endregion

        #region Public Method Region

        public void IncreaseTotalBombNumber(int incr)
        {
            if (TotalBombAmount + incr > Config.MaxBombNumber)
            {
                TotalBombAmount = Config.MaxBombNumber;
                CurrentBombAmount = TotalBombAmount;
            }
            else if (TotalBombAmount + incr < Config.MinBombNumber)
            {
                TotalBombAmount = Config.MinBombNumber;
                CurrentBombAmount = TotalBombAmount;
            }
            else
            {
                TotalBombAmount += incr;
                CurrentBombAmount += incr;
            }
        }

        public void IncreasePower(int incr)
        {
            if (BombPower + incr > Config.MaxBombPower)
                BombPower = Config.MaxBombPower;
            else if (BombPower + incr < Config.MinBombPower)
                BombPower = Config.MinBombPower;
            else
                BombPower += incr;
        }

        public void IncreaseSpeed(float incr)
        {
            Sprite.Speed += incr;
        }

        public virtual void ApplyBadItem(BadItemEffect effect)
        {
            HasBadItemEffect = true;
            BadItemEffect = effect;
            BadItemTimerLenght =
                TimeSpan.FromSeconds(GamePlayScreen.Random.Next(Config.BadItemTimerMin, Config.BadItemTimerMax));
        }

        protected virtual void RemoveBadItem()
        {
            HasBadItemEffect = false;
            BadItemTimer = TimeSpan.Zero;
            BadItemTimerLenght = TimeSpan.Zero;
        }

        public void Rebirth(Vector2 position)
        {
            IsAlive = true;
            Sprite.IsAnimating = true;
            InDestruction = false;
            Sprite.Position = position;
            Sprite.CurrentAnimation = AnimationKey.Down;
            _playerDeathAnimation.IsAnimating = false;

            Invincibility();
        }

        public virtual void ChangeLookDirection(byte newLookDirection)
        {
            PreviousLookDirection = CurrentDirection;
            CurrentDirection = (LookDirection) newLookDirection;
            Debug.Print("New look direction: " + (LookDirection) newLookDirection);
        }

        #endregion

        #region Override Method Region

        public void Destroy()
        {
            if (!InDestruction)
            {
                Sprite.IsAnimating = false;
                InDestruction = true;
                FinalBomber.Instance.GamePlayScreen.PlayerDeathSound.Play();
                _playerDeathAnimation.Position = Sprite.Position;
                _playerDeathAnimation.IsAnimating = true;
            }
        }

        public void Remove()
        {
            _playerDeathAnimation.IsAnimating = false;
            InDestruction = false;
            IsAlive = false;

            // Replacing for the gameplay on the edges
            // Right side
            if (Config.MapSize.X - Sprite.CellPosition.X < Config.MapSize.X/2)
            {
                Sprite.CurrentAnimation = AnimationKey.Left;
                Sprite.Position = new Vector2((Config.MapSize.X*Engine.TileWidth) - Engine.TileWidth, Sprite.Position.Y);
            }
                // Left side
            else
            {
                Sprite.CurrentAnimation = AnimationKey.Right;
                Sprite.Position = new Vector2(0, Sprite.Position.Y);
            }
        }

        protected virtual void MoveFromEdgeWall()
        {
        }

        #endregion

        #endregion
    }
}

public class PlayerCollection : List<Player>
{
    public Player GetPlayerByID(int playerID)
    {
        foreach (Player player in this)
        {
            if (player.Id == playerID)
                return player;
        }
        return null;
    }
}
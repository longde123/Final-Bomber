﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Final_Bomber.Sprites;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Final_Bomber.Controls;
using Final_Bomber.TileEngine;
using Final_Bomber.WorldEngine;
using Microsoft.Xna.Framework.Input;

namespace Final_Bomber.Components
{
    public enum LookDirection { Down, Left, Right, Up, Idle }

    public class Player : MapItem
    {
        #region Field Region
        private int id;

        public override AnimatedSprite Sprite { get; protected set; }

        private AnimatedSprite playerDeathAnimation;

        private bool isAlive;
        private bool inDestruction;
        private bool isMoving;
        private bool onEdge;
        private bool isInvincible;

        private TimeSpan invincibleTimer;
        private TimeSpan invincibleBlinkTimer;
        private float invincibleBlinkFrequency;
        
        private Point previousCellPosition;
        private bool cellChanging;
        private bool cellTeleporting;

        private Keys[] keys;
        private TimeSpan bombTimer;

        private LookDirection lookDirection;

        private Camera camera;

        private FinalBomber gameRef;
        private Point mapSize;

        // Characteristics
        private int power;
        private int totalBombNumber;
        private int currentBombNumber;

        // Bad item
        private bool hasBadItemEffect;
        private TimeSpan badItemTimer;
        private TimeSpan badItemTimerLenght;
        private BadItemEffect badItemEffect;
        private TimeSpan bombTimerSaved;
        private float speedSaved;
        private Keys[] keysSaved;

        // Artificial Intelligence
        Vector2 aiNextPosition;
        public List<Point> aiWay;

        #endregion

        #region Property Region

        public int Id
        {
            get { return id; }
        }

        public Camera Camera
        {
            get { return camera; }
        }

        public bool IsAlive
        {
            get { return isAlive; }
        }

        public bool InDestruction
        {
            get { return inDestruction; }
        }

        public bool IsInvincible
        {
            get { return isInvincible; }
        }

        public int Power
        {
            get { return power; }
        }

        public int CurrentBombNumber
        {
            get { return currentBombNumber; }
            set { currentBombNumber = value; }
        }

        public int TotalBombNumber
        {
            get { return totalBombNumber; }
        }

        public bool OnEdge
        {
            get { return onEdge; }
            set { onEdge = value; }
        }

        public bool HasBadItemEffect
        {
            get { return hasBadItemEffect; }
        }

        public BadItemEffect BadItemEffect
        {
            get { return badItemEffect; }
        }

        public TimeSpan BadItemTimer
        {
            get { return badItemTimer; }
        }

        public TimeSpan BadItemTimerLenght
        {
            get { return badItemTimerLenght; }
        }

        public TimeSpan BombTimer
        {
            get { return bombTimer; }
        }

        #endregion

        #region Constructor Region
        public Player(int id, FinalBomber game, Vector2 position)
        {
            this.id = id;
            this.gameRef = game;

            this.mapSize = game.GamePlayScreen.World.Levels[game.GamePlayScreen.World.CurrentLevel].Size;

            this.camera = new Camera(gameRef.ScreenRectangle);

            int animationFramesPerSecond = 10;
            Dictionary<AnimationKey, Animation> animations = new Dictionary<AnimationKey, Animation>();

            Animation animation = new Animation(4, 23, 23, 0, 0, animationFramesPerSecond);
            animations.Add(AnimationKey.Down, animation);

            animation = new Animation(4, 23, 23, 0, 23, animationFramesPerSecond);
            animations.Add(AnimationKey.Left, animation);

            animation = new Animation(4, 23, 23, 0, 46, animationFramesPerSecond);
            animations.Add(AnimationKey.Right, animation);

            animation = new Animation(4, 23, 23, 0, 69, animationFramesPerSecond);
            animations.Add(AnimationKey.Up, animation);

            Texture2D spriteTexture = gameRef.Content.Load<Texture2D>("Graphics/Characters/player1");
            

            this.Sprite = new AnimatedSprite(spriteTexture, animations, position);
            this.Sprite.ChangeFramesPerSecond(10);
            this.Sprite.Speed = Config.BasePlayerSpeed;

            this.previousCellPosition = this.Sprite.CellPosition; 

            Texture2D playerDeathTexture = gameRef.Content.Load<Texture2D>("Graphics/Characters/player1Death");
            animation = new Animation(8, 23, 23, 0, 0, 4);
            playerDeathAnimation = new AnimatedSprite(playerDeathTexture, animation, Sprite.Position);
            playerDeathAnimation.IsAnimating = false;

            this.isMoving = false;
            this.isAlive = true;
            this.inDestruction = false;
            this.onEdge = false;
            this.isInvincible = true;

            this.invincibleTimer = Config.PlayerInvincibleTimer;
            this.invincibleBlinkFrequency = Config.InvincibleBlinkFrequency;
            this.invincibleBlinkTimer = TimeSpan.FromSeconds(this.invincibleBlinkFrequency);

            power = Config.BasePlayerBombPower;
            totalBombNumber = Config.BasePlayerBombNumber;
            currentBombNumber = totalBombNumber;

            keys = Config.PlayersKeys[id - 1];
            bombTimer = Config.BombTimer;

            lookDirection = LookDirection.Down;

            // Bad item
            hasBadItemEffect = false;
            badItemTimer = TimeSpan.Zero;
            badItemTimerLenght = TimeSpan.Zero;

            // AI
            aiNextPosition = new Vector2(-1, -1);
            aiWay = new List<Point>();
        }
        #endregion

        #region XNA Method Region
        public override void Update(GameTime gameTime)
        {
            if (isAlive && !inDestruction)
            {
                Sprite.Update(gameTime);

                #region Invincibility

                if (!Config.Invincible && isInvincible)
                {
                    if (invincibleTimer >= TimeSpan.Zero)
                    {
                        invincibleTimer -= gameTime.ElapsedGameTime;
                        if (invincibleBlinkTimer >= TimeSpan.Zero)
                            invincibleBlinkTimer -= gameTime.ElapsedGameTime;
                        else
                            invincibleBlinkTimer = TimeSpan.FromSeconds(invincibleBlinkFrequency);
                    }
                    else
                    {
                        invincibleTimer = Config.PlayerInvincibleTimer;
                        isInvincible = false;
                    }
                }

                #endregion

                #region Movement

                if (previousCellPosition != Sprite.CellPosition)
                    cellChanging = true;
                else
                    cellChanging = false;

                #region Human's player part
                Vector2 motion = new Vector2();
                if (!Config.AIPlayers[id - 1])
                {
                    // Up
                    if (InputHandler.KeyDown(keys[0]))
                    {
                        Sprite.CurrentAnimation = AnimationKey.Up;
                        lookDirection = LookDirection.Up;
                        motion.Y = -1;
                    }
                    // Down
                    else if (InputHandler.KeyDown(keys[1]))
                    {
                        Sprite.CurrentAnimation = AnimationKey.Down;
                        lookDirection = LookDirection.Down;
                        motion.Y = 1;
                    }
                    // Left
                    else if (InputHandler.KeyDown(keys[2]))
                    {
                        Sprite.CurrentAnimation = AnimationKey.Left;
                        lookDirection = LookDirection.Left;
                        motion.X = -1;
                    }
                    // Right
                    else if (InputHandler.KeyDown(keys[3]))
                    {
                        Sprite.CurrentAnimation = AnimationKey.Right;
                        lookDirection = LookDirection.Right;
                        motion.X = 1;
                    }
                    else
                        lookDirection = LookDirection.Idle;
                }
                #endregion

                if (motion != Vector2.Zero || Config.AIPlayers[id - 1])
                {
                    if (!Config.AIPlayers[id - 1])
                    {
                        #region Human's player part
                        this.isMoving = true;
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
                                if (MoreLeftSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y - 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
                                        Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                                }
                                else if (MoreRightSide() && !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y - 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                                        Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                                }
                            }
                            // ... and that there is no wall
                            else
                            {
                                // If he is more on the left side
                                if (MoreLeftSide())
                                {
                                    Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                                }
                                // If he is more on the right side
                                else if (MoreRightSide())
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
                                if (MoreLeftSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y + 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y)))
                                        Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                                }
                                else if (MoreRightSide() && !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y + 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y)))
                                        Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                                }
                            }
                            else
                            {
                                // If he is more on the left side
                                if (MoreLeftSide())
                                {
                                    Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                                }
                                // If he is more on the right side
                                else if (MoreRightSide())
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
                                if (MoreTopSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y - 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
                                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                                }
                                else if (MoreBottomSide() && !WallAt(new Point(Sprite.CellPosition.X - 1, Sprite.CellPosition.Y + 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                                }
                            }
                            else
                            {
                                // If he is more on the top side, we lag him to the bottom
                                if (MoreTopSide())
                                {
                                    Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                                }
                                else if (MoreBottomSide())
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
                                if (MoreTopSide() && !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y - 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y - 1)))
                                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                                }
                                else if (MoreBottomSide() && !WallAt(new Point(Sprite.CellPosition.X + 1, Sprite.CellPosition.Y + 1)))
                                {
                                    if (!WallAt(new Point(Sprite.CellPosition.X, Sprite.CellPosition.Y + 1)))
                                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                                }
                            }
                            else
                            {
                                // If he is more on the top side, we lag him to the top
                                if (MoreTopSide())
                                {
                                    Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                                }
                                else if (MoreBottomSide())
                                {
                                    Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                                }
                            }
                        }
                        #endregion

                        #endregion
                    }
                    else
                    {
                        #region IA part

                        #region Walk
                        // If he hasn't reach his goal => we walk to this goal
                        if ((aiNextPosition.X != -1 && aiNextPosition.Y != -1) && !AI.HasReachNextPosition(Sprite.Position, Sprite.Speed, aiNextPosition))
                        {
                            this.isMoving = true;
                            Sprite.IsAnimating = true;

                            // If the AI is blocked
                            Level level = gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel];
                            if (level.CollisionLayer[Engine.VectorToCell(aiNextPosition).X, Engine.VectorToCell(aiNextPosition).Y] ||
                                level.HazardMap[Engine.VectorToCell(aiNextPosition).X, Engine.VectorToCell(aiNextPosition).Y] >= 2)
                            {
                                Sprite.IsAnimating = false;
                                this.isMoving = false;
                                // We define a new goal
                                bool[,] collisionLayer = level.CollisionLayer;
                                int[,] hazardMap = level.HazardMap;
                                MapItem[,] map = level.Map;
                                aiWay = AI.MakeAWay(
                                    Sprite.CellPosition,
                                    AI.SetNewGoal(Sprite.CellPosition, map, collisionLayer, hazardMap, mapSize),
                                    collisionLayer, hazardMap, mapSize);
                            }
                            
                            // Up
                            if (Sprite.Position.Y > aiNextPosition.Y)
                            {
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                                Sprite.CurrentAnimation = AnimationKey.Up;
                                lookDirection = LookDirection.Up;
                            }
                            // Down
                            else if (Sprite.Position.Y < aiNextPosition.Y)
                            {
                                Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                                Sprite.CurrentAnimation = AnimationKey.Down;
                                lookDirection = LookDirection.Down;
                            }
                            // Right
                            else if (Sprite.Position.X < aiNextPosition.X)
                            {
                                Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                                Sprite.CurrentAnimation = AnimationKey.Right;
                                lookDirection = LookDirection.Right;
                            }
                            // Left
                            else if (Sprite.Position.X > aiNextPosition.X)
                            {
                                Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                                Sprite.CurrentAnimation = AnimationKey.Left;
                                lookDirection = LookDirection.Left;
                            }
                        }
                        #endregion

                        #region Search a goal
                        // Otherwise => we find another goal
                        else
                        {
                            // We place the player at the center of its cell
                            Sprite.Position = Engine.CellToVector(Sprite.CellPosition);

                            Level level = gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel];

                            #region Bomb => AI
                            // Try to put a bomb
                                // Put a bomb
                            if (!hasBadItemEffect || (hasBadItemEffect && badItemEffect != BadItemEffect.NoBomb))
                            {
                                if (AI.TryToPutBomb(Sprite.CellPosition, power, level.Map, level.CollisionLayer, level.HazardMap, mapSize))
                                {
                                    if (this.currentBombNumber > 0)
                                    {
                                        Bomb bo = gameRef.GamePlayScreen.BombList.Find(b => b.Sprite.CellPosition == this.Sprite.CellPosition);
                                        if (bo == null)
                                        {
                                            this.currentBombNumber--;
                                            Bomb bomb = new Bomb(gameRef, this.id, Sprite.CellPosition, this.power, this.bombTimer, this.Sprite.Speed);

                                            if (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                                Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Player)
                                            {
                                                gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] = bomb;
                                                gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                                CollisionLayer[bomb.Sprite.CellPosition.X, bomb.Sprite.CellPosition.Y] = true;

                                                // We define a new way (to escape the bomb)
                                                aiWay = AI.MakeAWay(
                                                    Sprite.CellPosition,
                                                    AI.SetNewDefenseGoal(Sprite.CellPosition, level.CollisionLayer, level.HazardMap, mapSize),
                                                    level.CollisionLayer, level.HazardMap, mapSize);
                                            }
                                            gameRef.GamePlayScreen.BombList.Add(bomb);
                                        }
                                    }
                                }
                            }
                            #endregion

                            if (aiWay == null || aiWay.Count == 0)
                            {
                                Sprite.IsAnimating = false;
                                this.isMoving = false;
                                // We define a new goal
                                aiWay = AI.MakeAWay(
                                    Sprite.CellPosition,
                                    AI.SetNewGoal(Sprite.CellPosition, level.Map, level.CollisionLayer, level.HazardMap, mapSize),
                                    level.CollisionLayer, level.HazardMap, mapSize);

                                if (aiWay != null)
                                {
                                    aiNextPosition = Engine.CellToVector(aiWay[aiWay.Count - 1]);
                                    aiWay.Remove(aiWay[aiWay.Count - 1]);

                                    // If the AI is blocked
                                    if (level.CollisionLayer[Engine.VectorToCell(aiNextPosition).X, Engine.VectorToCell(aiNextPosition).Y] ||
                                        level.HazardMap[Engine.VectorToCell(aiNextPosition).X, Engine.VectorToCell(aiNextPosition).Y] >= 2)
                                    {
                                        Sprite.IsAnimating = false;
                                        this.isMoving = false;
                                        // We define a new goal
                                        bool[,] collisionLayer = level.CollisionLayer;
                                        int[,] hazardMap = level.HazardMap;
                                        MapItem[,] map = level.Map;
                                        aiWay = AI.MakeAWay(
                                            Sprite.CellPosition,
                                            AI.SetNewGoal(Sprite.CellPosition, map, collisionLayer, hazardMap, mapSize),
                                            collisionLayer, hazardMap, mapSize);
                                    }
                                }
                            }
                            else
                            {
                                // We finish the current way
                                aiNextPosition = Engine.CellToVector(aiWay[aiWay.Count - 1]);
                                aiWay.Remove(aiWay[aiWay.Count - 1]);
                                /*
                                // Update the way of the AI each time it changes of cell => usefull to battle against players (little bug here)
                                aiWay = AI.MakeAWay(
                                    Sprite.CellPosition,
                                    AI.SetNewGoal(Sprite.CellPosition, level.Map, level.CollisionLayer, level.HazardMap), 
                                    level.CollisionLayer, level.HazardMap);
                                */
                            }
                        }
                        #endregion

                        #endregion
                    }

                    Sprite.LockToMap();
                    
                    #region Wall collisions
                    // -- Vertical check -- //
                    // Is there a wall on the top ?
                    if (WallAt(new Point(this.Sprite.CellPosition.X, this.Sprite.CellPosition.Y - 1)))
                    {
                        // Is there a wall on the bottom ?
                        if (WallAt(new Point(this.Sprite.CellPosition.X, this.Sprite.CellPosition.Y + 1)))
                        {
                            // Top collision and Bottom collision
                            if ((lookDirection == LookDirection.Up && MoreTopSide()) || (lookDirection == LookDirection.Down && MoreBottomSide()))
                                this.Sprite.PositionY = this.Sprite.CellPosition.Y * Engine.TileHeight;
                        }
                        // No wall at the bottom
                        else
                        {
                            // Top collision
                            if (lookDirection == LookDirection.Up && MoreTopSide())
                                this.Sprite.PositionY = this.Sprite.CellPosition.Y * Engine.TileHeight;
                        }
                    }
                    // Wall only at the bottom
                    else if (WallAt(new Point(this.Sprite.CellPosition.X, this.Sprite.CellPosition.Y + 1)))
                    {
                        // Bottom collision
                        if (lookDirection == LookDirection.Down && MoreBottomSide())
                            this.Sprite.PositionY = this.Sprite.CellPosition.Y * Engine.TileHeight;
                        // To lag him
                        else if (lookDirection == LookDirection.Down)
                        {
                            if(MoreLeftSide())
                                this.Sprite.PositionX += this.Sprite.Speed;
                            else if(MoreRightSide())
                                this.Sprite.PositionX -= this.Sprite.Speed;
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
                            if ((this.lookDirection == LookDirection.Left && MoreLeftSide()) || (this.lookDirection == LookDirection.Right && MoreRightSide()))
                                this.Sprite.PositionX = this.Sprite.CellPosition.X * Engine.TileWidth - Engine.TileWidth / 2 + Engine.TileWidth / 2;
                        }
                        // Wall only at the left
                        else
                        {
                            // Left collision
                            if (this.lookDirection == LookDirection.Left && MoreLeftSide())
                                this.Sprite.PositionX = this.Sprite.CellPosition.X * Engine.TileWidth - Engine.TileWidth / 2 + Engine.TileWidth / 2;
                        }
                    }
                    // Wall only at the right
                    else if (WallAt(new Point(this.Sprite.CellPosition.X + 1, this.Sprite.CellPosition.Y)))
                    {
                        // Right collision
                        if (this.lookDirection == LookDirection.Right && MoreRightSide())
                            this.Sprite.PositionX = this.Sprite.CellPosition.X * Engine.TileWidth - Engine.TileWidth / 2 + Engine.TileWidth / 2;
                    }

                    
                    // The player must stay in the map
                    this.Sprite.PositionX = MathHelper.Clamp(this.Sprite.Position.X, Engine.TileWidth,
                        (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.X * Engine.TileWidth) - 2 * Engine.TileWidth);
                    this.Sprite.PositionY = MathHelper.Clamp(this.Sprite.Position.Y, Engine.TileHeight,
                        (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.Y * Engine.TileHeight) - 2 * Engine.TileHeight);
                    
                    #endregion
                    
                }
                else
                {
                    this.isMoving = false;
                    Sprite.IsAnimating = false;
                }

                #region Mise à jour de la position du joueur
                if (cellChanging)
                {
                    if (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                       Map[previousCellPosition.X, previousCellPosition.Y] == this)
                    {
                        gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                            Map[previousCellPosition.X, previousCellPosition.Y] = null;
                    }

                    if (cellTeleporting)
                        cellTeleporting = false;
                }
                else
                {
                    if (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] == null)
                    {
                        gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                            Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] = this;
                    }
                }
                #endregion

                #endregion

                #region Bomb

                #region Human player's part
                if (!Config.AIPlayers[id - 1])
                {
                    if ((hasBadItemEffect && badItemEffect == BadItemEffect.BombDrop) || (InputHandler.KeyPressed(keys[4]) &&
                        (!hasBadItemEffect || (hasBadItemEffect && badItemEffect != BadItemEffect.NoBomb))))
                    {
                        if (this.currentBombNumber > 0)
                        {
                            Bomb bo = gameRef.GamePlayScreen.BombList.Find(b => b.Sprite.CellPosition == this.Sprite.CellPosition);
                            if (bo == null)
                            {
                                this.currentBombNumber--;
                                Bomb bomb = new Bomb(gameRef, this.id, Sprite.CellPosition, this.power, this.bombTimer, this.Sprite.Speed);

                                if (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Player)
                                {
                                    gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] = bomb;
                                    gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                                    CollisionLayer[bomb.Sprite.CellPosition.X, bomb.Sprite.CellPosition.Y] = true;
                                }

                                gameRef.GamePlayScreen.BombList.Add(bomb);
                            }
                        }
                    }
                }
                #endregion

                #region Push a bomb
                if (lookDirection != LookDirection.Idle)
                {
                    Point direction = Point.Zero;
                    switch (lookDirection)
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
                        bomb.ChangeDirection(lookDirection, this.id);
                }
                #endregion

                #endregion

                #region Item

                if (gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Item)
                {
                    Item item = (Item)(gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y]);
                    if (!item.InDestruction)
                    {
                        if (!hasBadItemEffect || (hasBadItemEffect && item.Type != ItemType.BadItem))
                        {
                            item.ApplyItem(this);
                            gameRef.GamePlayScreen.ItemPickUpSound.Play();
                            item.Remove();
                        }
                    }
                }

                // Have caught a bad item
                if (hasBadItemEffect)
                {
                    badItemTimer += gameTime.ElapsedGameTime;
                    if (badItemTimer >= badItemTimerLenght)
                    {
                        switch (badItemEffect)
                        {
                            case BadItemEffect.TooSlow:
                                Sprite.Speed = speedSaved;
                                break;
                            case BadItemEffect.TooSpeed:
                                Sprite.Speed = speedSaved;
                                break;
                            case BadItemEffect.KeysInversion:
                                keys = keysSaved;
                                break;
                            case BadItemEffect.BombTimerChanged:
                                bombTimer = bombTimerSaved;
                                break;
                        }
                        hasBadItemEffect = false;
                        badItemTimer = TimeSpan.Zero;
                        badItemTimerLenght = TimeSpan.Zero;
                    }
                }

                #endregion

                #region Teleporter

                if (!cellTeleporting && gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                    Map[Sprite.CellPosition.X, Sprite.CellPosition.Y] is Teleporter)
                {
                    Teleporter teleporter = (Teleporter)(gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].
                        Map[Sprite.CellPosition.X, Sprite.CellPosition.Y]);

                    teleporter.ChangePosition(this);
                    cellTeleporting = true;
                }

                #endregion
            }
            #region Death
            else if(playerDeathAnimation.IsAnimating)
            {
                playerDeathAnimation.Update(gameTime);

                if (playerDeathAnimation.Animation.CurrentFrame == playerDeathAnimation.Animation.FrameCount - 1)
                    Remove();
            }
            #endregion
            #region Edge wall gameplay
            else if (onEdge && (!Config.ActiveSuddenDeath || (Config.ActiveSuddenDeath && !gameRef.GamePlayScreen.SuddenDeath.HasStarted)))
            {
                Sprite.Update(gameTime);

                // The player is either at the top either at the bottom
                // => he can only move on the right or on the left
                if (Sprite.Position.Y <= 0 || Sprite.Position.Y >= (mapSize.Y - 1) * Engine.TileHeight)
                {
                    // If he wants to go to the left
                    if (Sprite.Position.X > 0 && InputHandler.KeyDown(keys[2]))
                        Sprite.Position = new Vector2(Sprite.Position.X - Sprite.Speed, Sprite.Position.Y);
                    // If he wants to go to the right
                    else if (Sprite.Position.X < (mapSize.X * Engine.TileWidth) - Engine.TileWidth &&
                        InputHandler.KeyDown(keys[3]))
                        Sprite.Position = new Vector2(Sprite.Position.X + Sprite.Speed, Sprite.Position.Y);
                }
                // The player is either on the left either on the right
                if (Sprite.Position.X <= 0 || Sprite.Position.X >= (mapSize.X - 1) * Engine.TileWidth)
                {
                    // If he wants to go to the top
                    if (Sprite.Position.Y > 0 && InputHandler.KeyDown(keys[0]))
                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y - Sprite.Speed);
                    // If he wants to go to the bottom
                    else if (Sprite.Position.Y < (mapSize.Y * Engine.TileHeight) - Engine.TileHeight &&
                        InputHandler.KeyDown(keys[1]))
                        Sprite.Position = new Vector2(Sprite.Position.X, Sprite.Position.Y + Sprite.Speed);
                }

                if (Sprite.Position.Y <= 0)
                    Sprite.CurrentAnimation = AnimationKey.Down;
                else if (Sprite.Position.Y >= (mapSize.Y - 1) * Engine.TileHeight)
                    Sprite.CurrentAnimation = AnimationKey.Up;
                else if (Sprite.Position.X <= 0)
                        Sprite.CurrentAnimation = AnimationKey.Right;
                else if (Sprite.Position.X >= (mapSize.X - 1) * Engine.TileWidth)
                        Sprite.CurrentAnimation = AnimationKey.Left;

                #region Bombs => Edge gameplay

                if (InputHandler.KeyDown(keys[4]) && this.currentBombNumber > 0)
                {
                    // He can't put a bomb when he is on a corner
                    if (!((Sprite.CellPosition.Y == 0 && (Sprite.CellPosition.X == 0 || Sprite.CellPosition.X == mapSize.X - 1)) ||
                        (Sprite.CellPosition.Y == mapSize.Y - 1 && (Sprite.CellPosition.X == 0 || (Sprite.CellPosition.X == mapSize.X - 1)))))
                    {
                        Level level = gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel];
                        int lag = 0;
                        Point bombPosition = Sprite.CellPosition;
                        // Up
                        if (Sprite.CellPosition.Y == 0)
                        {
                            while (Sprite.CellPosition.Y + lag + 3 < mapSize.Y &&
                                    level.CollisionLayer[Sprite.CellPosition.X, Sprite.CellPosition.Y + lag + 3])
                            {
                                lag++;
                            }
                            bombPosition.Y = Sprite.CellPosition.Y + lag + 3;
                            if (bombPosition.Y < mapSize.Y)
                            {
                                Bomb bomb = new Bomb(gameRef, id, bombPosition, power, bombTimer, Config.BaseBombSpeed + Sprite.Speed);
                                level.CollisionLayer[bombPosition.X, bombPosition.Y] = true;
                                gameRef.GamePlayScreen.BombList.Add(bomb);
                                level.Map[bombPosition.X, bombPosition.Y] = bomb;
                                this.currentBombNumber--;
                            }
                        }
                        // Down
                        if (Sprite.CellPosition.Y == mapSize.Y - 1)
                        {
                            while (Sprite.CellPosition.Y - lag - 3 >= 0 &&
                                    level.CollisionLayer[Sprite.CellPosition.X, Sprite.CellPosition.Y - lag - 3])
                            {
                                lag++;
                            }
                            bombPosition.Y = Sprite.CellPosition.Y - lag - 3;
                            if (bombPosition.Y >= 0)
                            {
                                Bomb bomb = new Bomb(gameRef, id, bombPosition, power, bombTimer, Config.BaseBombSpeed + Sprite.Speed);
                                level.CollisionLayer[bombPosition.X, bombPosition.Y] = true;
                                gameRef.GamePlayScreen.BombList.Add(bomb);
                                level.Map[bombPosition.X, bombPosition.Y] = bomb;
                                this.currentBombNumber--;
                            }
                        }
                        // Left
                        if (Sprite.CellPosition.X == 0)
                        {
                            while (Sprite.CellPosition.X + lag + 3 < mapSize.X &&
                                    level.CollisionLayer[Sprite.CellPosition.X + lag + 3, Sprite.CellPosition.Y])
                            {
                                lag++;
                            }
                            bombPosition.X = Sprite.CellPosition.X + lag + 3;
                            if (bombPosition.X < mapSize.X)
                            {
                                Bomb bomb = new Bomb(gameRef, id, bombPosition, power, bombTimer, Config.BaseBombSpeed + Sprite.Speed);
                                level.CollisionLayer[bombPosition.X, bombPosition.Y] = true;
                                gameRef.GamePlayScreen.BombList.Add(bomb);
                                level.Map[bombPosition.X, bombPosition.Y] = bomb;
                                this.currentBombNumber--;
                            }
                        }
                        // Right
                        if (Sprite.CellPosition.X == mapSize.X - 1)
                        {
                            while (Sprite.CellPosition.X - lag - 3 >= 0 &&
                                    level.CollisionLayer[Sprite.CellPosition.X - lag - 3, Sprite.CellPosition.Y])
                            {
                                lag++;
                            }
                            bombPosition.X = Sprite.CellPosition.X - lag - 3;
                            if (bombPosition.X >= 0)
                            {
                                Bomb bomb = new Bomb(gameRef, id, bombPosition, power, bombTimer, Config.BaseBombSpeed + Sprite.Speed);
                                level.CollisionLayer[bombPosition.X, bombPosition.Y] = true;
                                gameRef.GamePlayScreen.BombList.Add(bomb);
                                level.Map[bombPosition.X, bombPosition.Y] = bomb;
                                this.currentBombNumber--;
                            }
                        }
                    }
                }

                #endregion
            }
            #endregion

            #region Camera
            camera.Update(gameTime);

            if (Config.Debug)
            {
                if (InputHandler.KeyDown(Keys.PageUp) ||
                    InputHandler.ButtonReleased(Buttons.LeftShoulder, PlayerIndex.One))
                {
                    camera.ZoomIn();
                    if (camera.CameraMode == CameraMode.Follow)
                        camera.LockToSprite(Sprite);
                }
                else if (InputHandler.KeyDown(Keys.PageDown))
                {
                    camera.ZoomOut();
                    if (camera.CameraMode == CameraMode.Follow)
                        camera.LockToSprite(Sprite);
                }
                else if (InputHandler.KeyDown(Keys.End))
                {
                    camera.ZoomReset();
                }

                if (InputHandler.KeyReleased(Keys.F))
                {
                    camera.ToggleCameraMode();
                    if (camera.CameraMode == CameraMode.Follow)
                        camera.LockToSprite(Sprite);
                }

                if (camera.CameraMode != CameraMode.Follow)
                {
                    if (InputHandler.KeyReleased(Keys.L))
                    {
                        camera.LockToSprite(Sprite);
                    }
                }
            }

            if (isMoving && camera.CameraMode == CameraMode.Follow)
                camera.LockToSprite(Sprite);
            #endregion

            previousCellPosition = Sprite.CellPosition;
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 playerNamePosition = new Vector2(
                Sprite.Position.X + Engine.Origin.X + Sprite.Width/2 - 
                ControlManager.SpriteFont.MeasureString(Config.PlayersName[id - 1]).X / 2 + 5,
                Sprite.Position.Y + Engine.Origin.Y - 25 - 
                ControlManager.SpriteFont.MeasureString(Config.PlayersName[id - 1]).Y / 2);

            if ((isAlive && !inDestruction) || onEdge)
            {
                if (isInvincible)
                {
                    if (invincibleBlinkTimer > TimeSpan.FromSeconds(invincibleBlinkFrequency * 0.5f))
                    {
                        Sprite.Draw(gameTime, gameRef.SpriteBatch);
                        gameRef.SpriteBatch.DrawString(ControlManager.SpriteFont, Config.PlayersName[id - 1], playerNamePosition, Color.Black);
                    }
                }
                else
                {
                    Sprite.Draw(gameTime, gameRef.SpriteBatch);
                    gameRef.SpriteBatch.DrawString(ControlManager.SpriteFont, Config.PlayersName[id - 1], playerNamePosition, Color.Black);
                }
            }
            else
            {
                playerDeathAnimation.Draw(gameTime, gameRef.SpriteBatch);
                gameRef.SpriteBatch.DrawString(ControlManager.SpriteFont, Config.PlayersName[id - 1], playerNamePosition, Color.Black);
            }
        }
        #endregion

        #region Method Region

        #region Private Method Region

        private Bomb BombAt(Point cell)
        {
            if (cell.X >= 0 && cell.Y >= 0 && cell.X < gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.X &&
                cell.Y < gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.Y)
            {
                Bomb bomb = gameRef.GamePlayScreen.BombList.Find(b => b.Sprite.CellPosition == cell);
                return (bomb);
            }
            else
                return null;
        }

        private bool WallAt(Point cell)
        {
            if (cell.X >= 0 && cell.Y >= 0 && cell.X < gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.X &&
                cell.Y < gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].Size.Y)
                return (this.gameRef.GamePlayScreen.World.Levels[gameRef.GamePlayScreen.World.CurrentLevel].CollisionLayer[cell.X, cell.Y]);
            else
                return false;
        }

        private bool MoreTopSide()
        {
            return this.Sprite.Position.Y < ((this.Sprite.CellPosition.Y * Engine.TileHeight) - (this.Sprite.Speed / 2));
        }

        private bool MoreBottomSide()
        {
            return this.Sprite.Position.Y > ((Sprite.CellPosition.Y * Engine.TileHeight) + (Sprite.Speed / 2));
        }

        private bool MoreLeftSide()
        {
            return this.Sprite.Position.X < ((this.Sprite.CellPosition.X * Engine.TileWidth) - (this.Sprite.Speed / 2));
        }

        private bool MoreRightSide()
        {
            return this.Sprite.Position.X > ((this.Sprite.CellPosition.X * Engine.TileWidth) + (this.Sprite.Speed / 2));
        }

        private void Invincibility()
        {
            this.isInvincible = true;
            this.invincibleTimer = Config.PlayerInvincibleTimer;
            this.invincibleBlinkFrequency = Config.InvincibleBlinkFrequency;
            this.invincibleBlinkTimer = TimeSpan.FromSeconds(this.invincibleBlinkFrequency);
        }

        #endregion

        #region Public Method Region

        public void IncreaseTotalBombNumber(int incr)
        {
            if (this.totalBombNumber + incr > Config.MaxBombNumber)
            {
                this.totalBombNumber = Config.MaxBombNumber;
                this.currentBombNumber = totalBombNumber;
            }
            else if (this.totalBombNumber + incr < Config.MinBombNumber)
            {
                this.totalBombNumber = Config.MinBombNumber;
                this.currentBombNumber = totalBombNumber;
            }
            else
            {
                this.totalBombNumber += incr;
                this.currentBombNumber += incr;
            }
        }

        public void IncreasePower(int incr)
        {
            if (this.power + incr > Config.MaxBombPower)
                this.power = Config.MaxBombPower;
            else if (this.power + incr < Config.MinBombPower)
                this.power = Config.MinBombPower;
            else
                this.power += incr;
        }

        public void IncreaseSpeed(float incr)
        {
            this.Sprite.Speed += incr;
        }

        public void ApplyBadItem(BadItemEffect effect)
        {
            this.hasBadItemEffect = true;
            this.badItemEffect = effect;
            this.badItemTimerLenght = TimeSpan.FromSeconds(gameRef.GamePlayScreen.Random.Next(Config.BadItemTimerMin, Config.BadItemTimerMax));
            switch (effect)
            {
                case BadItemEffect.TooSlow:
                    this.speedSaved = this.Sprite.Speed;
                    this.Sprite.Speed = Config.MinSpeed;
                    break;
                case BadItemEffect.TooSpeed:
                    speedSaved = this.Sprite.Speed;
                    this.Sprite.Speed = Config.MaxSpeed;
                    break;
                case BadItemEffect.KeysInversion:
                    this.keysSaved = (Keys[])this.keys.Clone();
                    int[] inversedKeysArray = new int[] { 1, 0, 3, 2 };
                    for (int i = 0; i < inversedKeysArray.Length; i++)
                        this.keys[i] = this.keysSaved[inversedKeysArray[i]];
                    break;
                case BadItemEffect.BombTimerChanged:
                    this.bombTimerSaved = this.bombTimer;
                    int randomBombTimer = gameRef.GamePlayScreen.Random.Next(Config.BadItemTimerChangedMin, Config.BadItemTimerChangedMax);
                    this.bombTimer = TimeSpan.FromSeconds(randomBombTimer);
                    break;
            }
        }

        public void Rebirth(Vector2 position)
        {
            this.isAlive = true;
            this.Sprite.IsAnimating = true;
            this.inDestruction = false;
            this.Sprite.Position = position;
            this.Sprite.CurrentAnimation = AnimationKey.Down;
            this.playerDeathAnimation.IsAnimating = false;
            
            Invincibility();
        }
        
        #endregion

        #region Override Method Region

        public override void Destroy()
        {
            if (!this.inDestruction)
            {
                this.Sprite.IsAnimating = false;
                this.inDestruction = true;
                gameRef.GamePlayScreen.PlayerDeathSound.Play();
                this.playerDeathAnimation.Position = this.Sprite.Position;
                this.playerDeathAnimation.IsAnimating = true;
            }
        }

        public override void Remove()
        {
            this.playerDeathAnimation.IsAnimating = false;
            this.inDestruction = false;
            this.isAlive = false;

            // Replacing for the gameplay on the edges
            // Right side
            if (mapSize.X - this.Sprite.CellPosition.X < mapSize.X / 2)
            {
                this.Sprite.CurrentAnimation = AnimationKey.Left;
                this.Sprite.Position = new Vector2((mapSize.X * Engine.TileWidth) - Engine.TileWidth, this.Sprite.Position.Y);
            }
            // Left side
            else
            {
                this.Sprite.CurrentAnimation = AnimationKey.Right;
                this.Sprite.Position = new Vector2(0, this.Sprite.Position.Y);
            }
        }

        #endregion

        #endregion
    }
}

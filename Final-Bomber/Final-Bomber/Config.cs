﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.IO;
using Final_Bomber.Components;

namespace Final_Bomber
{
    public enum SuddenDeathTypeEnum { OnlyWall, OnlyBomb, BombAndWall, Whole };
    public enum TeleporterPositionTypeEnum { Randomly, PlusForm };
    public enum ArrowPositionTypeEnum { Randomly, SquareForm };
    static class Config
    {
        // Debug
        public static bool Debug = false;

        // Taille
        public static Point MapSize = new Point(17, 17);
        public static Point MinimumMapSize = new Point(9, 9);
        public static Point[] MaximumMapSize = new Point[]
        {
            new Point(17, 17),
            new Point(23, 23),
            new Point(33, 29),
            new Point(53, 33)
        };


        public static int PlayersNumber = 1;
        public static bool FullScreen = false;

        public static Point[] PlayersPositions = new Point[]
        {
            new Point(1, 1),
            new Point(MapSize.X - 2, MapSize.Y - 2),
            new Point(1, MapSize.Y - 2),
            new Point(MapSize.X - 2, 1),
            new Point((int)Math.Ceiling((double)(MapSize.X - 2)/(double)2), (int)Math.Ceiling((double)(MapSize.Y - 2)/(double)2))
        };

        public static Keys[][] PlayersKeys = new Keys[][]
        {
             new Keys[]{ Keys.Up, Keys.Down, Keys.Left, Keys.Right, Keys.RightControl }, 
             new Keys[]{ Keys.Z, Keys.S, Keys.Q, Keys.D, Keys.LeftControl }, 
             new Keys[]{ Keys.I, Keys.K, Keys.J, Keys.L, Keys.Space }, 
             new Keys[]{ Keys.NumPad8, Keys.NumPad5, Keys.NumPad4, Keys.NumPad6, Keys.Enter },
             new Keys[]{ Keys.T, Keys.G, Keys.F, Keys.H, Keys.Y }
        };

        public static bool isThereAIPlayer = true;
        public static bool[] AIPlayers = new bool[] { true, true, true, true, true };

        // Joueur
        public static Color[] PlayersColor = new Color[] { Color.White, Color.White, Color.White, Color.White };

        // Base characteristics
        public static int BasePlayerBombPower = 1;
        public static float BasePlayerSpeed = 2.5f;
        public static float BaseBombSpeed = 3f;
        public static int BasePlayerBombNumber = 10;

        // Characteristics minimum and maximum
        public static float MaxSpeed = 30f;
        public static float MinSpeed = 1f;
        public static int MaxBombPower = (MapSize.X + MapSize.Y) / 2;
        public static int MinBombPower = 1;
        public static int MaxBombNumber = (MapSize.X * MapSize.Y) / 2;
        public static int MinBombNumber = 1;

        public static float PlayerSpeedIncrementeur = 0.25f;
        public static float BombSpeedIncrementeur = 1f;

        public static bool PlayerCanPush = false;
        public static bool PlayerCanLaunch = false;

        public static bool DisplayName = true;
        public static string[] PlayersName = new string[] { "Rena Ryuuguu", "Mion Sonozaki", "Satoko Houjou", "Rika Fukude", "Keiichi Maebara" };

        public static int WallNumber = 100; // This is a percentage => from 0% to 100%
        public static int ItemNumber = 50; // This is a percentage => from 0% to 100%
        public static int ItemTypeNumber = 5;
        public static int TeleporterNumber = 0;
        public static int ArrowNumber = 0;

        public static bool ActiveTeleporters = false;
        public static TeleporterPositionTypeEnum TeleporterPositionType = TeleporterPositionTypeEnum.PlusForm;

        public static bool ActiveArrows = false;
        public static ArrowPositionTypeEnum ArrowPositionType = ArrowPositionTypeEnum.SquareForm;
        
        public static LookDirection[] ArrowLookDirection = new LookDirection[] 
        { LookDirection.Down, LookDirection.Left, LookDirection.Right, LookDirection.Up };

        // Invincibility
        public static bool Invincible = false;
        public static TimeSpan PlayerInvincibleTimer = TimeSpan.FromSeconds(3);
        public static float InvincibleBlinkFrequency = 0.5f;

        // Bombs
        public static bool BombCollision = true;
        public static bool ExplosionThroughWall = true;
        public static int BombLatency = 10; // Bomb's animation's frames par second
        // Initially => 2
        public static TimeSpan BombTimer = TimeSpan.FromSeconds(2);

        // Item
        public static List<ItemType> ItemTypeAvaible = new List<ItemType>() 
        { 
            ItemType.Power,
            ItemType.Bomb,
            ItemType.Speed,
            /*ItemType.BadItem,*/
            ItemType.Point 
        };
        public static ItemType[] ItemTypeArray = new ItemType[]
        {
            ItemType.Power,
            ItemType.Bomb,
            ItemType.Speed,
            ItemType.BadItem,
            ItemType.Point
        };
        // 0 => Power, 1 => Bomb, 2 => Speed, 3 => Bad item, 4 => Point
        public static Dictionary<ItemType, int> ItemTypeIndex = new Dictionary<ItemType, int>
        {
            {ItemType.Power, 0},
            {ItemType.Bomb, 1},
            {ItemType.Speed, 2},
            {ItemType.BadItem, 3},
            {ItemType.Point, 4}
        };

        // Volume
        public static float Volume = 0.0f;

        // IA
        public static bool InactiveIA = false;

        // Resolution
        public static int[,] Resolutions = new int[,] { { 800, 600 }, { 1024, 768 }, { 1280, 1024 }, { 1920, 1080 } };
        public static int IndexResolution = 0;

        public static int BonusTypeNumber = 5;
        public static bool ExitGame = false;

        // Sudden Death
        public static bool ActiveSuddenDeath = true;
        public static TimeSpan SuddenDeathTimer = TimeSpan.FromSeconds(80);
        public static SuddenDeathTypeEnum SuddenDeathType = SuddenDeathTypeEnum.OnlyWall;
        public static float SuddenDeathCounterBombs = 0.3f;
        public static float SuddenDeathCounterWalls = 0.3f;
        public static float SuddenDeathWallSpeed = (float)Math.Round(0.25f, 2);
        public static float SuddenDeathMaxWallSpeed = 1f;
        public static Dictionary<SuddenDeathTypeEnum, string> SuddenDeathTypeText = new Dictionary<SuddenDeathTypeEnum, string>
        {
            { SuddenDeathTypeEnum.BombAndWall, "Bombes et murs" },
            { SuddenDeathTypeEnum.OnlyBomb, "Bombes" },
            { SuddenDeathTypeEnum.OnlyWall, "Murs" },
            { SuddenDeathTypeEnum.Whole, "Trous" }
        };
        public static SuddenDeathTypeEnum[] SuddenDeathTypeArray = new SuddenDeathTypeEnum[]
        {
            SuddenDeathTypeEnum.BombAndWall,
            SuddenDeathTypeEnum.OnlyBomb,
            SuddenDeathTypeEnum.OnlyWall,
            SuddenDeathTypeEnum.Whole         
        };

        // Scores
        public static int[] PlayersScores = new int[5];

        // Map moving scale
        public static float MapMovingScale = 10f;

        // Bad Item
        public static int BadItemTimerMin = 10; // Seconds
        public static int BadItemTimerMax = 30; // Seconds
        public static int BadItemTimerChangedMin = 3; // Seconds
        public static int BadItemTimerChangedMax = 8; // Seconds
        public static List<BadItemEffect> BadItemEffectList = new List<BadItemEffect>()
        {
            BadItemEffect.BombDrop,
            BadItemEffect.BombTimerChanged,
            BadItemEffect.KeysInversion,
            BadItemEffect.NoBomb,
            BadItemEffect.TooSlow,
            BadItemEffect.TooSpeed
        };

        // HUD
        public static int HUDPlayerInfoSpace = 105;

        // Keyboarding
        public static float TextCursorBlinkFrequency = 0.5f;
    }
}
﻿using System;
using FBClient.Core;

namespace FBClient.Network
{
    static class GameSettings
    {
        static public Int64 currentMap = -1;
        public static string CurrentMapName = "";
        static public string CurrentVersion = "";

        public const string THISVERSION = "5";

        static public string Username, Password;

        static public long speed;
    }
}
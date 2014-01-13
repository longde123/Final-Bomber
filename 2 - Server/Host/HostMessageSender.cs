﻿using System;
using System.Collections.Generic;
using System.IO;
using FBLibrary;
using FBLibrary.Core;
using FBLibrary.Network;
using FBServer.Core;
using FBServer.Core.Entities;
using FBServer.Core.WorldEngine;
using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;
using System.Linq;

namespace FBServer.Host
{
    sealed partial class GameServer
    {
        public void SendGameInfo(Client client)
        {
            try
            {
                if (client.ClientConnection.Status == NetConnectionStatus.Connected)
                {
                    NetOutgoingMessage message = _server.CreateMessage();

                    message.Write((byte)MessageType.ServerMessage.GameStartInfo);
                    message.Write(MapLoader.MapFileDictionary.Values.First());

                    _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);

                    Program.Log.Info("Sended game info map [" + MapLoader.MapFileDictionary.Values.First() + "]");
                }
            }
            catch (NetException e)
            {
                Program.Log.Info("NET EXCEPTION: " + e.ToString());
            }
        }

        public void SendCurrentMap(Client client)
        {
            if (client.ClientConnection.Status == NetConnectionStatus.Connected)
            {
                NetOutgoingMessage message = _server.CreateMessage();
                message.Write((byte)MessageType.ServerMessage.Map);

                message.Write(Instance.GameManager.CurrentMap.Name);
                message.Write(Instance.GameManager.CurrentMap.GetMd5());

                string path = "Content/Maps/" + Instance.GameManager.CurrentMap.Name;
                byte[] mapData = File.ReadAllBytes(path);

                message.Write(mapData.Length);
                foreach (var bt in mapData)
                {
                    message.Write(bt);
                }

                _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);
                Program.Log.Info("Send the map to client #" + client.ClientId);
            }
        }

        public void SendStartGame(Client client, bool gameInProgress)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.StartGame);
            message.Write(gameInProgress);
            
            if (!gameInProgress)
            {
                message.Write(client.Player.Id);
                message.Write(client.Player.Speed);
                message.Write(GameConfiguration.SuddenDeathTimer.Milliseconds);

                List<Wall> walls = GameServer.Instance.GameManager.WallList;
                message.Write(walls.Count);
                foreach (var wall in walls)
                {
                    message.Write(wall.CellPosition);
                }
            }

            _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);
            Program.Log.Info("Send start game to client #" + client.ClientId);
        }

        // Send all players to this player
        public void SendPlayersToNew(Client client, bool sendPosition)
        {
            Program.Log.Info("Send the players to the new player (client #" + client.ClientId + ")");

            foreach (Client currentClient in Clients)
            {
                if (client != currentClient)
                {
                    NetOutgoingMessage message = GetPlayerInfo(currentClient);

                    _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);

                    if (sendPosition)
                    {
                        SendPlayerPosition(currentClient.Player, false);
                    }

                    Program.Log.Info("Send the player (client #" + currentClient.ClientId + ") to the new player (client #" + client.ClientId + ")");
                }
            }

        }

        // Send this player to all other available
        public void SendPlayerInfo(Client client, bool sendPosition)
        {
            if (client.ClientConnection.Status == NetConnectionStatus.Connected)
            {
                NetOutgoingMessage message = GetPlayerInfo(client);

                _server.SendToAll(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered, 0);

                if (sendPosition)
                {
                    SendPlayerPosition(client.Player, false);
                }

                Program.Log.Info("Send the players to the new player (client #" + client.ClientId + ")");
            }
        }

        public void SendRemovePlayer(Player removedPlayer)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.RemovePlayer);
            message.Write(removedPlayer.Id);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Send that the player #" + removedPlayer.Id + " is dead !");
        }

        #region GetPlayerInfo
        private NetOutgoingMessage GetPlayerInfo(Client client)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.PlayerInfo);

            message.Write(client.Player.Id);
            message.Write(client.Username);

            return message;
        }
        #endregion

        // Send the player's movement to all other players
        public void SendPlayerPosition(Player player, bool notDir)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.PlayerPosition);

            message.Write(player.Position.X);
            message.Write(player.Position.Y);

            message.Write((byte)player.CurrentDirection);

            message.Write(player.Id);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Send position of player #" + player.Id + " !");
        }

        // Send to all players that this player has placed a bomb
        public void SendPlayerPlacingBomb(Player player, Point position)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.PlayerPlacingBomb);
            message.Write(player.Id);
            message.Write(position);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Send that player #" + player.Id + " has planted a bomb !");
        }

        // Send to all players that a bomb has blow up
        public void SendBombExploded(Bomb bomb)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.BombExploded);
            message.Write(bomb.CellPosition);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Send that bomb exploded to everyone ! (position: " + bomb.Position + ")");
        }

        public void SendPowerUpDrop(PowerUp powerUp)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.PowerUpDrop);

            message.Write((byte)powerUp.Type);
            message.Write(powerUp.CellPosition);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Power up dropped ! (type: " + powerUp.Type + "|position: " + powerUp.CellPosition + ")");
        }

        public void SendPowerUpPick(Player player, PowerUp powerUp)
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.PowerUpPick);

            message.Write(player.Id);
            message.Write(powerUp.CellPosition);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);

            Program.Log.Info("Power up pick by player #" + player.Id + " !");
        }

        public void SendSuddenDeath()
        {
            NetOutgoingMessage message = _server.CreateMessage();

            message.Write((byte)MessageType.ServerMessage.SuddenDeath);

            _server.SendToAll(message, NetDeliveryMethod.ReliableOrdered);
            
            Program.Log.Info("SUDDEN DEATH!");
        }

        public void SendRoundEnd(Client client)
        {
            if (client.ClientConnection.Status == NetConnectionStatus.Connected)
            {
                NetOutgoingMessage message = _server.CreateMessage();

                message.Write((byte)MessageType.ServerMessage.RoundEnd);

                _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);

                Program.Log.Info("Send 'RoundEnd' to player #" + client.Player.Id);
            }
        }

        public void SendEnd(Client client)
        {
            if (client.ClientConnection.Status == NetConnectionStatus.Connected)
            {
                NetOutgoingMessage message = _server.CreateMessage();

                message.Write((byte)MessageType.ServerMessage.End);
                message.Write(client.Player.IsAlive);

                _server.SendMessage(message, client.ClientConnection, NetDeliveryMethod.ReliableOrdered);
                Program.Log.Info("Send 'End' to player #" + client.Player.Id);
            }
        }
    }
}

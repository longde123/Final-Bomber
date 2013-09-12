﻿using Final_BomberServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Final_BomberServer.Host
{
    partial class HostGame
    {
        List<Player> _alivePlayers;
        private void GameStepProccesing()
        {
            if (GameSettings.gameServer.clients.Count == 2 // TO CHANGE
                && !StartedMatch /*&& GameSettings.gameServer.clients.IsClientsReady()*/)
            {
                GameInitialize();
            }

            foreach (Client client in GameSettings.gameServer.clients)
            {
                if (client.NewClient && StartedMatch && client.isReady)
                {
                    GameSettings.gameServer.SendStartGame(client, true);
                    GameSettings.gameServer.SendPlayersToNew(client);
                    client.NewClient = false;
                }
            }

            // End of game
            _alivePlayers = GameSettings.gameServer.clients.GetAlivePlayers();
            if (StartedMatch && _alivePlayers.Count < 2)
            {
                //MainServer.SendPlayerStats();
                GameSettings.CurrentMap++;
                //MainServer.SendNextMap();
                EndGame();
                foreach (Client client in GameSettings.gameServer.clients)
                {
                    client.isReady = false;
                    GameSettings.gameServer.SendEnd(client);
                    client.Player = new Player(client.Player.PlayerId); //Återställer dens orginal värden
                    GameSettings.gameServer.SendGameInfo(client);
                }
            }
        }
    }
}

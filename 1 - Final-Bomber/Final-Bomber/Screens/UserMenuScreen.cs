﻿using System;
using Microsoft.Xna.Framework;
using Final_Bomber.Controls;
using Microsoft.Xna.Framework.Input;

namespace Final_Bomber.Screens
{
    public class UserMenuScreen : BaseGameState
    {
        #region Field region
        string[] menuString;
        int indexMenu;
        Vector2 menuPosition;

        // Net
        public const string VERSION = "1";
        public static string username, password;
        TimeSpan tick;

        public bool hasLoggedIn;
        bool hasConnected;

        #endregion

        #region Constructor region
        public UserMenuScreen(Game game, GameStateManager manager)
            : base(game, manager)
        {
            menuString = new string[] { "Se connecter", "S'inscrire", "Retour" };
            indexMenu = 0;
        }
        #endregion

        #region XNA Method region

        public override void Initialize()
        {
            menuPosition = new Vector2(Config.Resolutions[Config.IndexResolution, 0] / 2, Config.Resolutions[Config.IndexResolution, 1] / 2);
            base.Initialize();

            // Net
            /*
            if (FinalBomber.Instance.Server == null || !FinalBomber.Instance.Server.HasStarted)
            {
                hasLoggedIn = false;
                hasConnected = false;
                tick = TimeSpan.FromSeconds(5);

                FinalBomber.Instance.Server = new MainServer();

                try
                {
                    FinalBomber.Instance.Server.StartMainConnection();
                }
                catch (TypeInitializationException)
                {
                    throw new Exception("Couldn't connect to the internet, please check your connection");
                }
            }
            */
        }

        protected override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            ControlManager.Update(gameTime, PlayerIndex.One);

            // Net
            /*
            if(!hasConnected)
            {
                FinalBomber.Instance.Server.RunMainConnection();
                if (FinalBomber.Instance.Server.Connected)
                    hasConnected = true;
                else
                {
                    if (tick < TimeSpan.Zero)
                        throw new Exception("Vous ne pouvez pas vous connecter au serveur principal. \n" +
                    "Vérifiez votre connexion internet ou votre pare-feu avant de contacter un administrateur !");
                    else
                        tick -= gameTime.ElapsedGameTime;
                }
            }
            */

            if (InputHandler.KeyPressed(Keys.Enter))
            {
                switch (indexMenu)
                {
                    case 0:
                        StateManager.ChangeState(FinalBomber.Instance.UserLoginMenuScreen);
                        break;
                    case 1:
                        StateManager.ChangeState(FinalBomber.Instance.UserRegistrationMenuScreen);
                        break;
                    case 2:
                        Exit();
                        break;
                }
            }

            if (InputHandler.KeyPressed(Keys.Up))
            {
                if (indexMenu <= 0)
                    indexMenu = menuString.Length - 1;
                else
                    indexMenu--;
            }
            else if (InputHandler.KeyPressed(Keys.Down))
            {
                indexMenu = (indexMenu + 1) % menuString.Length; 
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            FinalBomber.Instance.SpriteBatch.Begin();

            base.Draw(gameTime);

            ControlManager.Draw(FinalBomber.Instance.SpriteBatch);

            if(hasConnected)
                FinalBomber.Instance.SpriteBatch.DrawString(this.BigFont, "Connecté au serveur !", Vector2.Zero, Color.Red);

            for (int i = 0; i < menuString.Length; i++)
            {
                Color textColor = Color.Black;
                if (i == indexMenu)
                    textColor = Color.Green;

                FinalBomber.Instance.SpriteBatch.DrawString(this.BigFont, menuString[i],
                    new Vector2(menuPosition.X - this.BigFont.MeasureString(menuString[i]).X / 2,
                        menuPosition.Y + this.BigFont.MeasureString(menuString[i]).Y * i - this.BigFont.MeasureString(menuString[i]).Y / 2), textColor);
            }

            FinalBomber.Instance.SpriteBatch.End();
        }

        #endregion

        #region Private Region Methods

        private void Exit()
        {
            /*
            StateManager.ChangeState(FinalBomber.Instance.MultiplayerGameModeMenuScreen);
            if (FinalBomber.Instance.Server.HasStarted)
                FinalBomber.Instance.Server.EndMainConnection("Bye");
            */
        }

        #endregion
    }
}
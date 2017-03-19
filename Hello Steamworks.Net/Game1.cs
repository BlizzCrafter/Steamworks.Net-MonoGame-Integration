using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text.RegularExpressions;
using Steamworks;

namespace Hello_Steamworks.Net
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont Font;

        string 
            WelcomeMessage = "Error: Please start your Steam Client before you run this example!", 
            WelcomeNote = "- Press [Shift + Tab] to open the Steam Overlay -";

        bool isSteamRunning = false;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        
        protected override void Initialize()
        {
            try
            {
                if (!SteamAPI.Init()) Console.WriteLine("SteamAPI.Init() failed!");
                else
                {
                    isSteamRunning = true;

                    //Get your trimmed Steam User Name
                    string SteamUserName = SteamFriends.GetPersonaName(), UserNameTrimmed;
                    SteamUserName = Regex.Replace(SteamUserName, @"[^\u0000-\u007F]", string.Empty); //Remove unsopported chars like emojis
                    UserNameTrimmed = SteamUserName.Trim(); //Remove spaces

                    WelcomeMessage = "Hello " + UserNameTrimmed + "!";

                    SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);

                    Exiting += Game1_Exiting;
                }
            }
            catch (DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it.
                Console.WriteLine(e);
            }

            base.Initialize();
        }

        //ShutDown the SteamAPI
        private void Game1_Exiting(object sender, EventArgs e)
        {
            SteamAPI.Shutdown();
        }
        
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<SpriteFont>(@"Font");
        }
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (isSteamRunning == true) SteamAPI.RunCallbacks();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            //Draw WelcomeMessage
            spriteBatch.DrawString(Font, WelcomeMessage, new Vector2(
                (graphics.PreferredBackBufferWidth / 2) - (Font.MeasureString(WelcomeMessage).X / 2),
                (graphics.PreferredBackBufferHeight / 2) - (Font.MeasureString(WelcomeMessage).Y / 2) - 
                (isSteamRunning ? 20 : 0)), Color.GreenYellow);

            if (isSteamRunning == true)
            {
                //Draw WelcomeNote
                spriteBatch.DrawString(Font, WelcomeNote, new Vector2(
                    (graphics.PreferredBackBufferWidth / 2) - (Font.MeasureString(WelcomeNote).X / 2),
                    (graphics.PreferredBackBufferHeight / 2) - (Font.MeasureString(WelcomeNote).Y / 2) + 20), Color.GreenYellow);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

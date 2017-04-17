using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Steamworks;
using AchievementHunter.Classes;

namespace AchievementHunter
{
    public class AchievementSample : Game
    {
        // Enum for possible game states on the client
        public enum EClientGameState
        {
            k_EClientGameWinner,
            k_EClientGameLoser
        };

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Rectangle ShipPosition, WinGamePosition, LoseGamePosition, ResetAllPosition;
        Texture2D ShipTexture, Pixel;

        KeyboardState oldKeys;

        // Do not use 'SteamApi.IsSteamRunning()'! It's not reliable and slow
        //see: https://github.com/rlabrecque/Steamworks.NET/issues/30
        public static bool IsSteamRunning { get; set; } = false;

        //Error Message: Steam Client not running.
        private const string STEAM_NOT_RUNNING_ERROR_MESSAGE = "Please start your steam client to receive data!";

        //Description Message: How to control the ship.
        private const string DESCRIPTION_MESSAGE = "Use [W] [A] [S] [D] to control the ship!";

        // Store screen dimensions.
        public static int ScreenWidth, ScreenHeight;

        public static SpriteFont Font { get; private set; }

        private StatsAndAchievements StatsAndAchievements { get; set; }

        public AchievementSample()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // The following lines restart your game through the Steam-client in case someone started it by double-clicking the exe.
            try
            {
                if (SteamAPI.RestartAppIfNecessary((AppId_t)480))
                {
                    Console.Out.WriteLine("Game wasn't started by Steam-client. Restarting.");
                    Exit();
                }
            }
            catch (DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it.
                Console.Out.WriteLine("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib." +
                                      " It's likely not in the correct location. Refer to the README for more details.\n" +
                                      e);
                Exit();
            }
        }
        
        protected override void Initialize()
        {
            try
            {
                if (!SteamAPI.Init())
                {
                    Console.WriteLine("SteamAPI.Init() failed!");
                }
                else
                {
                    // Set the "IsSteamRunning" flag to true
                    IsSteamRunning = true;

                    // Set overlay position
                    SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);

                    // Create new stats and achievement object
                    StatsAndAchievements = new StatsAndAchievements();

                    // Add exiting event to close the steamapi on exit
                    Exiting += Game1_Exiting;
                }
            }
            catch (DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it
                Console.WriteLine(e);
                Exit();
            }

            IsFixedTimeStep = true;
            graphics.SynchronizeWithVerticalRetrace = true;

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            ScreenWidth = graphics.PreferredBackBufferWidth;
            ScreenHeight = graphics.PreferredBackBufferHeight;

            Window.Position = new Point(GraphicsDevice.DisplayMode.Width / 2 - graphics.PreferredBackBufferWidth / 2,
                GraphicsDevice.DisplayMode.Height / 2 - graphics.PreferredBackBufferHeight / 2 - 25);

            IsMouseVisible = true;

            base.Initialize();
        }

        /// <summary>
        ///     Replaces characters not supported by your spritefont.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="input">The input string.</param>
        /// <param name="replaceString">The string to replace illegal characters with.</param>
        /// <returns></returns>
        public static string ReplaceUnsupportedChars(SpriteFont font, string input, string replaceString = "")
        {
            string result = "";
            if (input == null)
            {
                return null;
            }

            foreach (char c in input)
            {
                if (font.Characters.Contains(c) || c == '\r' || c == '\n')
                {
                    result += c;
                }
                else
                {
                    result += replaceString;
                }
            }
            return result;
        }

        private void Game1_Exiting(object sender, EventArgs e)
        {
            SteamAPI.Shutdown();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<SpriteFont>(@"Font");
            ShipTexture = Content.Load<Texture2D>(@"Ship");

            ResetShip(true);

            WinGamePosition = new Rectangle(20, ScreenHeight / 2, 150, 150);
            LoseGamePosition = new Rectangle(20, (ScreenHeight / 2) + 180, 150, 150);
            ResetAllPosition = new Rectangle((ScreenWidth / 2) - 75, 20, 150, 150);

            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            if (IsSteamRunning)
            {
                // Get your trimmed Steam User Name.
                string steamUserName = SteamFriends.GetPersonaName();
                // Remove unsupported chars like emojis or other stuff our font cannot handle.
                steamUserName = ReplaceUnsupportedChars(Font, steamUserName);
                var userNameTrimmed = steamUserName.Trim();
            }
        }
        
        protected override void UnloadContent()
        {
            
        }
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (IsSteamRunning)
            {
                #region ShipMovement

                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    ShipPosition.Y = MathHelper.Clamp(ShipPosition.Y - 5, 0, ScreenHeight);
                    StatsAndAchievements.AddDistanceTraveled(3.0f);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    ShipPosition.X = MathHelper.Clamp(ShipPosition.X - 5, 0, ScreenWidth);
                    StatsAndAchievements.AddDistanceTraveled(3.0f);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    ShipPosition.Y = MathHelper.Clamp(ShipPosition.Y + 5, 0, ScreenHeight - ShipTexture.Height);
                    StatsAndAchievements.AddDistanceTraveled(3.0f);
                }
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    ShipPosition.X = MathHelper.Clamp(ShipPosition.X + 5, 0, ScreenWidth - ShipTexture.Width);
                    StatsAndAchievements.AddDistanceTraveled(3.0f);
                }

                #endregion

                #region ShipCollisionDetection

                if (ShipPosition.Intersects(ResetAllPosition))
                {
                    ResetShip(true);
                    SteamUserStats.ResetAllStats(true);
                    SteamUserStats.RequestCurrentStats();
                    StatsAndAchievements.ResetDistanceTraveled();
                }

                if (ShipPosition.Intersects(WinGamePosition))
                {
                    ResetShip(false);
                    StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameWinner);
                }

                if (ShipPosition.Intersects(LoseGamePosition))
                {
                    ResetShip(false);
                    StatsAndAchievements.OnGameStateChange(EClientGameState.k_EClientGameLoser);
                }

                #endregion

                StatsAndAchievements.Update(gameTime);
                SteamAPI.RunCallbacks();
            }

            oldKeys = Keyboard.GetState();
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullCounterClockwise);

            if (IsSteamRunning)
            {
                // WinGame Rectangle
                spriteBatch.Draw(Pixel, WinGamePosition, Color.Blue);
                spriteBatch.DrawString(Font, "Win_Game!", new Vector2(
                    WinGamePosition.X + (WinGamePosition.Width / 2) - (Font.MeasureString("Win_Game!").X / 2),
                    WinGamePosition.Y + (WinGamePosition.Height / 2) - (Font.MeasureString("Win_Game!").Y / 2)), Color.White);

                // LoseGame Rectangle
                spriteBatch.Draw(Pixel, LoseGamePosition, Color.Red);
                spriteBatch.DrawString(Font, "Lose_Game!", new Vector2(
                    LoseGamePosition.X + (LoseGamePosition.Width / 2) - (Font.MeasureString("Lose_Game!").X / 2),
                    LoseGamePosition.Y + (LoseGamePosition.Height / 2) - (Font.MeasureString("Lose_Game!").Y / 2)), Color.White);

                // ResetAll Rectangle
                spriteBatch.Draw(Pixel, ResetAllPosition, Color.Yellow);
                spriteBatch.DrawString(Font, "Reset_ALL!", new Vector2(
                    ResetAllPosition.X + (ResetAllPosition.Width / 2) - (Font.MeasureString("Reset_ALL!").X / 2),
                    ResetAllPosition.Y + (ResetAllPosition.Height / 2) - (Font.MeasureString("Reset_ALL!").Y / 2)), Color.Black);

                // Draw the ship (MonoGame logo)
                spriteBatch.Draw(ShipTexture, ShipPosition, Color.White);

                // Description
                spriteBatch.DrawString(Font, DESCRIPTION_MESSAGE, new Vector2(
                    ScreenWidth - (Font.MeasureString(DESCRIPTION_MESSAGE).X * 2),
                    ScreenHeight - Font.MeasureString(DESCRIPTION_MESSAGE).Y - 20), Color.GreenYellow);
                
                StatsAndAchievements.Draw(spriteBatch);
            }
            else
            {
                // Error Message
                spriteBatch.DrawString(Font, STEAM_NOT_RUNNING_ERROR_MESSAGE, new Vector2(
                    (ScreenWidth / 2) - (Font.MeasureString(STEAM_NOT_RUNNING_ERROR_MESSAGE).X / 2),
                    (ScreenHeight / 2) - (Font.MeasureString(STEAM_NOT_RUNNING_ERROR_MESSAGE).Y / 2)), Color.GreenYellow);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Reset the ship to the screen center.
        /// </summary>
        private void ResetShip(bool forceScreenCenter)
        {
            ShipPosition = new Rectangle(
                (StatsAndAchievements.m_nTotalGamesPlayed > 7 && !forceScreenCenter ? 
                200 : (ScreenWidth / 2) - (ShipTexture.Width / 2)),
                (ScreenHeight / 2), ShipTexture.Width, ShipTexture.Height);
        }
    }
}

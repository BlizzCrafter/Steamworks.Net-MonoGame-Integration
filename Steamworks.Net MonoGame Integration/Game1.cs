using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text.RegularExpressions;

namespace Steamworks.Net_MonoGame_Integration
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont Font;

        int ScreenWidth, ScreenHeight;
        string SteamNotRunningErrorMessage = "Please start your steam client to receive data!";

        #region Steam Fields

        /// <summary>
        /// Hold the information if the steam client is running after calling Steam.Init()
        /// </summary>
        bool isSteamRunning { get; set; }

        //Collectible data
        string CurrentLanguage { get; set; } = "";
        string AvailableLanguages { get; set; } = "";
        string InstallDir { get; set; } = "";
        static bool SteamOverlayActive { get; set; } = false;
        static string UserStats { get; set; } = "";
        static string PersonaState { get; set; } = "";
        static string LeaderboardData { get; set; } = "";
        static string NumberOfCurrentPlayers { get; set; } = "";

        /// <summary>
        /// Get your Steam user name
        /// </summary>
        string SteamUserName { get; set; } = "";
        
        /// <summary>
        /// Get your playtime in seconds since game is active
        /// </summary>
        uint PlayTimeInSeconds()
        {
            return SteamUtils.GetSecondsSinceAppActive();
        }

        /// <summary>
        /// A Texture2D object for your Steam Avatar image
        /// </summary>
        Texture2D UserAvatar;

        /// <summary>
        /// Get your steam avatar.
        /// </summary>
        /// <param name="device">The GraphicsDevice</param>
        /// <returns>Your Steam Avatar Image as a Texture2D object</returns>
        Texture2D GetSteamUserAvatar(GraphicsDevice device)
        {
            // Get the icon type as a integer.
            int icon = SteamFriends.GetLargeFriendAvatar(SteamUser.GetSteamID());

            // Check if we got an icon type.
            if (icon != 0)
            {
                uint width = 0;
                uint height = 0;
                bool ret = SteamUtils.GetImageSize(icon, out width, out height);

                if (ret && width > 0 && height > 0)
                {
                    byte[] rgba = new byte[width * height * 4];
                    ret = SteamUtils.GetImageRGBA(icon, rgba, rgba.Length);
                    if (ret)
                    {
                        Texture2D texture = new Texture2D(device, (int) width, (int) height, false, SurfaceFormat.Color);
                        texture.SetData(rgba, 0, rgba.Length);
                        // Return the Texture2D with your Steam Avatar data.
                        return texture;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// This callback checks if the SteamOverlay was activated
        /// </summary>
        static Callback<GameOverlayActivated_t> m_GameOverlayActivated;

        /// <summary>
        /// This callback receives the current number of players
        /// </summary>
        static CallResult<NumberOfCurrentPlayers_t> m_NumberOfCurrentPlayers;

        /// <summary>
        /// This callback receives a Leaderboard
        /// </summary>
        static CallResult<LeaderboardFindResult_t> m_callResultFindLeaderboard;

        /// <summary>
        /// This callback receives the PersonaStateChange
        /// </summary>
        static Callback<PersonaStateChange_t> m_PersonaStateChange;

        /// <summary>
        /// This callback receives Stats
        /// </summary>
        static Callback<UserStatsReceived_t> m_UserStatsReceived;

        /// <summary>
        /// Initialize some Steam Callbacks
        /// </summary>
        void InitializeCallbacks()
        {
            m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
            m_NumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
            m_callResultFindLeaderboard = CallResult<LeaderboardFindResult_t>.Create(OnFindLeaderboard);
            m_PersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
            m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(
                (pCallback) => {
                    UserStats = "[" + 
                    UserStatsReceived_t.k_iCallback + " - UserStatsReceived] - " + 
                    pCallback.m_eResult + " -- " + pCallback.m_nGameID + " -- " + pCallback.m_steamIDUser;
                });
        }

        //Register some Callbacks
        static void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            if (pCallback.m_bActive == 0)
            {
                //GameOverlay is not active
                SteamOverlayActive = false;
            }
            else
            {
                //GameOverlay is active
                SteamOverlayActive = true;
            }
        }        
        static void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIOFailure)
        {
            NumberOfCurrentPlayers = ("[" + NumberOfCurrentPlayers_t.k_iCallback + 
                " - NumberOfCurrentPlayers] - " + pCallback.m_bSuccess + " -- " + pCallback.m_cPlayers).ToString();
        }
        static void OnFindLeaderboard(LeaderboardFindResult_t pCallback, bool bIOFailure)
        {
            LeaderboardData =
                "[" + LeaderboardFindResult_t.k_iCallback + " - LeaderboardFindResult] - " +
                pCallback.m_bLeaderboardFound + " -- " + pCallback.m_hSteamLeaderboard;
        }
        static void OnPersonaStateChange(PersonaStateChange_t pCallback)
        {
            PersonaState =
                "[" + PersonaStateChange_t.k_iCallback + " - PersonaStateChange] - " +
                pCallback.m_ulSteamID + " -- " + pCallback.m_nChangeFlags;
        }

        #endregion

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        private void Game1_Exiting(object sender, EventArgs e)
        {
            SteamAPI.Shutdown();
        }
        
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            IsMouseVisible = true;

            ScreenWidth = graphics.PreferredBackBufferWidth;
            ScreenHeight = graphics.PreferredBackBufferHeight;

            graphics.SynchronizeWithVerticalRetrace = true;
            IsFixedTimeStep = false;

            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            Font = Content.Load<SpriteFont>(@"Font");

            bool SteamLoadingError = false;
            try
            {
                if (!SteamAPI.Init())
                {
                    Console.WriteLine("SteamAPI.Init() failed!");
                    SteamLoadingError = true;
                }
                else
                {
                    //Steam is running
                    isSteamRunning = true;
                }
            }
            catch (DllNotFoundException e)
            { 
                // We check this here as it will be the first instance of it.
                Console.WriteLine(e);
                SteamLoadingError = true;
            }

            if (SteamLoadingError == false)
            {
                InitializeCallbacks(); // We do this after SteamAPI.Init() has occured

                SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);

                //Uncomment the next line to adjust the OverlayNotificationPosition
                //SteamUtils.SetOverlayNotificationInset(400, 0);

                //Set Collactible Data
                CurrentLanguage = "CurrentGameLanguage: " + SteamApps.GetCurrentGameLanguage();
                AvailableLanguages = "Languages: " + SteamApps.GetAvailableGameLanguages();
                UserStats = "Reqesting Current Stats - " + SteamUserStats.RequestCurrentStats();
                m_NumberOfCurrentPlayers.Set(SteamUserStats.GetNumberOfCurrentPlayers());
                SteamAPICall_t hSteamAPICall = SteamUserStats.FindLeaderboard("Quickest Win");
                m_callResultFindLeaderboard.Set(hSteamAPICall);

                string folder;
                uint length = SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out folder, 260);
                InstallDir = "AppInstallDir: " + length + " " + folder;

                //Get your Steam Avatar (Image) as a Texture2D
                UserAvatar = GetSteamUserAvatar(GraphicsDevice);

                //Get your trimmed Steam User Name
                string UntrimmedUserName = "";
                UntrimmedUserName = SteamFriends.GetPersonaName();
                UntrimmedUserName = Regex.Replace(UntrimmedUserName, @"[^\u0000-\u007F]", string.Empty); //Remove unsopported chars like emojis
                SteamUserName = UntrimmedUserName.Trim(); //Remove spaces

                Exiting += Game1_Exiting;
            }
        }
        
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            if (isSteamRunning == true) SteamAPI.RunCallbacks();

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            spriteBatch.Begin();

            if (isSteamRunning == true)
            {
                //Draw your Steam Avatar and Steam Name
                if (UserAvatar != null)
                {
                    Vector2 AvatarPosition = new Vector2(
                        (ScreenWidth / 2),
                        (ScreenHeight / 2) + (!SteamOverlayActive ? MoveUpAndDown(gameTime, 2).Y * 25 : 0));

                    spriteBatch.Draw(UserAvatar, AvatarPosition, null, Color.White, 0f,
                        //Origin
                        new Vector2(
                            UserAvatar.Width / 2,
                            UserAvatar.Height / 2), 1f, SpriteEffects.None, 0f);

                    spriteBatch.DrawString(Font, SteamUserName,
                        new Vector2(
                            AvatarPosition.X - (Font.MeasureString(SteamUserName).X / 2),
                            AvatarPosition.Y - (UserAvatar.Height / 2) - (Font.MeasureString(SteamUserName).Y * 1.5f)),
                        Color.Yellow);
                }

                //Draw Data UpLeft
                spriteBatch.DrawString(Font,
                    CurrentLanguage + "\n" +
                    AvailableLanguages + "\n" +
                    InstallDir + "\n\n" +
                    "Overlay Active: " + SteamOverlayActive.ToString() + "\n" +
                    "App PlayTime: " + PlayTimeInSeconds().ToString(),
                    new Vector2(20, 20), Color.White);

                //Draw Data DownLeft
                spriteBatch.DrawString(Font,
                    NumberOfCurrentPlayers.ToString() + "\n" +
                    PersonaState + "\n" +
                    UserStats + "\n" +
                    LeaderboardData,
                    new Vector2(20, 375), Color.White);
            }
            else
            {
                spriteBatch.DrawString(Font, SteamNotRunningErrorMessage, 
                    new Vector2(
                            (ScreenWidth / 2) - (Font.MeasureString(SteamNotRunningErrorMessage).X / 2),
                            (ScreenHeight / 2) - (Font.MeasureString(SteamNotRunningErrorMessage).Y / 2)), Color.White);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Smooth UpDown Movement
        /// </summary>
        Vector2 MoveUpAndDown(GameTime gameTime, float speed)
        {
            if (gameTime != null)
            {
                double time = gameTime.TotalGameTime.TotalSeconds * speed;

                float x = 0;
                float y = (float)Math.Sin(time);

                return new Vector2(x, y);
            }
            return Vector2.Zero;
        }
    }
}

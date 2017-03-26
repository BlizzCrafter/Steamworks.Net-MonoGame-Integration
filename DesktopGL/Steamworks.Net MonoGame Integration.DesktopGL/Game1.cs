using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// System.Drawing.Color is identical to Microsoft.Xna.Framework.Color.
// We want to use the one from the Microsoft.Xna.Framework.

namespace Steamworks.Net_MonoGame_Integration.DesktopGL
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        public SpriteFont Font { get; private set; }
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        private const string STEAM_NOT_RUNNING_ERROR_MESSAGE = "Please start your steam client to receive data!";


        /// <summary>
        ///     Hold the information if the steam client is running after calling Steam.Init().
        /// </summary>
        private bool IsSteamRunning { get; set; }

        // Collectible data.
        private string SteamUserName { get; set; } = "";
        private string CurrentLanguage { get; set; } = "";
        private string AvailableLanguages { get; set; } = "";
        private string InstallDir { get; set; } = "";
        private Texture2D UserAvatar { get; set; }

        private static bool SteamOverlayActive { get; set; }
        private static string UserStats { get; set; } = "";
        private static string PersonaState { get; set; } = "";
        private static string LeaderboardData { get; set; } = "";
        private static string NumberOfCurrentPlayers { get; set; } = "";

        private uint PlayTimeInSeconds() => SteamUtils.GetSecondsSinceAppActive();

        /// <summary>
        ///     Get your steam avatar.
        ///     Important:
        ///     The returned Texture2D object is NOT loaded using a ContentManager.
        ///     So it's your responsibility to dispose it at the end by calling <see cref="Texture2D.Dispose()" />.
        /// </summary>
        /// <param name="device">The GraphicsDevice</param>
        /// <returns>Your Steam Avatar Image as a Texture2D object</returns>
        private Texture2D GetSteamUserAvatar(GraphicsDevice device)
        {
            // Get the icon type as a integer.
            var icon = SteamFriends.GetLargeFriendAvatar(SteamUser.GetSteamID());

            // Check if we got an icon type.
            if (icon != 0)
            {
                uint width;
                uint height;
                var ret = SteamUtils.GetImageSize(icon, out width, out height);

                if (ret && width > 0 && height > 0)
                {
                    var rgba = new byte[width * height * 4];
                    ret = SteamUtils.GetImageRGBA(icon, rgba, rgba.Length);
                    if (ret)
                    {
                        var texture = new Texture2D(device, (int)width, (int)height, false, SurfaceFormat.Color);
                        texture.SetData(rgba, 0, rgba.Length);
                        return texture;
                    }
                }
            }
            return null;
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

        private static Callback<GameOverlayActivated_t> mGameOverlayActivated;
        private static CallResult<NumberOfCurrentPlayers_t> mNumberOfCurrentPlayers;
        private static CallResult<LeaderboardFindResult_t> mCallResultFindLeaderboard;
        private static Callback<PersonaStateChange_t> mPersonaStateChange;
        private static Callback<UserStatsReceived_t> mUserStatsReceived;

        /// <summary>
        ///     Initialize some Steam Callbacks.
        /// </summary>
        private void InitializeCallbacks()
        {
            mGameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
            mNumberOfCurrentPlayers = CallResult<NumberOfCurrentPlayers_t>.Create(OnNumberOfCurrentPlayers);
            mCallResultFindLeaderboard = CallResult<LeaderboardFindResult_t>.Create(OnFindLeaderboard);
            mPersonaStateChange = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
            mUserStatsReceived =
                Callback<UserStatsReceived_t>.Create(
                    pCallback =>
                    {
                        UserStats =
                            $"[{UserStatsReceived_t.k_iCallback} - UserStatsReceived] - {pCallback.m_eResult} -- {pCallback.m_nGameID} -- {pCallback.m_steamIDUser}";
                    });
        }

        private static void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            if (pCallback.m_bActive == 0)
            {
                // GameOverlay is not active.
                SteamOverlayActive = false;
            }
            else
            {
                // GameOverlay is active.
                SteamOverlayActive = true;
            }
        }

        private static void OnNumberOfCurrentPlayers(NumberOfCurrentPlayers_t pCallback, bool bIoFailure)
        {
            NumberOfCurrentPlayers =
                $"[{NumberOfCurrentPlayers_t.k_iCallback} - NumberOfCurrentPlayers] - {pCallback.m_bSuccess} -- {pCallback.m_cPlayers}";
        }

        private static void OnFindLeaderboard(LeaderboardFindResult_t pCallback, bool bIoFailure)
        {
            LeaderboardData =
                $"[{LeaderboardFindResult_t.k_iCallback} - LeaderboardFindResult] - {pCallback.m_bLeaderboardFound} -- {pCallback.m_hSteamLeaderboard}";
        }

        private static void OnPersonaStateChange(PersonaStateChange_t pCallback)
        {
            PersonaState =
                $"[{PersonaStateChange_t.k_iCallback} - PersonaStateChange] - {pCallback.m_ulSteamID} -- {pCallback.m_nChangeFlags}";
        }

        public Game1()
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

        private void Game1_Exiting(object sender, EventArgs e)
        {
            SteamAPI.Shutdown();
            UserAvatar?.Dispose();
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            IsMouseVisible = true;
            ScreenWidth = graphics.PreferredBackBufferWidth;
            ScreenHeight = graphics.PreferredBackBufferHeight;
            graphics.SynchronizeWithVerticalRetrace = true;
            IsFixedTimeStep = false;

            Window.Position = new Point(GraphicsDevice.DisplayMode.Width / 2 - graphics.PreferredBackBufferWidth / 2,
                GraphicsDevice.DisplayMode.Height / 2 - graphics.PreferredBackBufferHeight / 2 - 25);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            Font = Content.Load<SpriteFont>(@"Font");

            var steamLoadingError = false;
            if (!SteamAPI.Init())
            {
                Console.WriteLine("SteamAPI.Init() failed!");
                steamLoadingError = true;
            }
            else
            {
                // Steam is running.
                IsSteamRunning = true;
            }

            if (steamLoadingError == false)
            {
                // It's important that the next call happens AFTER the call to SteamAPI.Init().
                InitializeCallbacks();

                SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);
                // Uncomment the next line to adjust the OverlayNotificationPosition.
                //SteamUtils.SetOverlayNotificationInset(400, 0);

                // Set collactible data.
                CurrentLanguage = $"CurrentGameLanguage: {SteamApps.GetCurrentGameLanguage()}";
                AvailableLanguages = $"Languages: {SteamApps.GetAvailableGameLanguages()}";
                UserStats = $"Reqesting Current Stats - {SteamUserStats.RequestCurrentStats()}";
                mNumberOfCurrentPlayers.Set(SteamUserStats.GetNumberOfCurrentPlayers());
                var hSteamApiCall = SteamUserStats.FindLeaderboard("Quickest Win");
                mCallResultFindLeaderboard.Set(hSteamApiCall);

                string folder;
                var length = SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out folder, 260);
                InstallDir = $"AppInstallDir: {length} {folder}";

                // Get your Steam Avatar (Image) as a Texture2D.
                UserAvatar = GetSteamUserAvatar(GraphicsDevice);

                // Get your trimmed Steam User Name.
                var untrimmedUserName = SteamFriends.GetPersonaName();
                // Remove unsupported chars like emojis or other stuff our font cannot handle.
                untrimmedUserName = ReplaceUnsupportedChars(Font, untrimmedUserName);
                SteamUserName = untrimmedUserName.Trim();

                Exiting += Game1_Exiting;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // TODO: Add your update logic here

            if (IsSteamRunning) SteamAPI.RunCallbacks();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            spriteBatch.Begin();

            if (IsSteamRunning)
            {
                //Draw your Steam Avatar and Steam Name
                if (UserAvatar != null)
                {
                    var avatarPosition = new Vector2(ScreenWidth / 2f,
                        ScreenHeight / 2f + (!SteamOverlayActive ? MoveUpAndDown(gameTime, 2).Y * 25 : 0));
                    spriteBatch.Draw(UserAvatar, avatarPosition, null, Color.White, 0f,
                        new Vector2(UserAvatar.Width / 2f, UserAvatar.Height / 2f), 1f, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(Font, SteamUserName,
                        new Vector2(avatarPosition.X - Font.MeasureString(SteamUserName).X / 2f,
                            avatarPosition.Y - UserAvatar.Height / 2f - Font.MeasureString(SteamUserName).Y * 1.5f),
                        Color.Yellow);
                }

                // Draw data up/left.
                spriteBatch.DrawString(Font,
                    $"{CurrentLanguage}\n{AvailableLanguages}\n{InstallDir}\n\nOverlay Active: {SteamOverlayActive}\nApp PlayTime: {PlayTimeInSeconds()}",
                    new Vector2(20, 20), Color.White);

                // Draw data down/left.
                spriteBatch.DrawString(Font, $"{NumberOfCurrentPlayers}\n{PersonaState}\n{UserStats}\n{LeaderboardData}",
                    new Vector2(20, 375), Color.White);
            }
            else
            {
                spriteBatch.DrawString(Font, STEAM_NOT_RUNNING_ERROR_MESSAGE,
                    new Vector2(ScreenWidth / 2f - Font.MeasureString(STEAM_NOT_RUNNING_ERROR_MESSAGE).X / 2f,
                        ScreenHeight / 2f - Font.MeasureString(STEAM_NOT_RUNNING_ERROR_MESSAGE).Y / 2f), Color.White);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        /// <summary>
        ///     Smooth up/down movement.
        /// </summary>
        private Vector2 MoveUpAndDown(GameTime gameTime, float speed)
        {
            var time = gameTime.TotalGameTime.TotalSeconds * speed;
            return new Vector2(0, (float)Math.Sin(time));
        }
    }
}
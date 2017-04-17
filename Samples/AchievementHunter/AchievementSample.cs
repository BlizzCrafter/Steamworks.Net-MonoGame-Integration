using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Steamworks;
using System;

namespace AchievementHunter
{
    public class AchievementSample : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Do not use 'SteamApi.IsSteamRunning()'! It's not reliable and slow
        //see: https://github.com/rlabrecque/Steamworks.NET/issues/30
        public static bool IsSteamRunning { get; set; } = false;
        public SpriteFont Font { get; private set; }

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
                    IsSteamRunning = true;

                    SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionBottomRight);

                    Exiting += Game1_Exiting;
                }
            }
            catch (DllNotFoundException e)
            {
                // We check this here as it will be the first instance of it.
                Console.WriteLine(e);
                Exit();
            }

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

            if (IsSteamRunning) SteamAPI.RunCallbacks();

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }
    }
}

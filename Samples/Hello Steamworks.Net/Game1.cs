using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Steamworks;

namespace Hello_Steamworks.Net
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        public SpriteFont Font { get; private set; }

        public string WelcomeMessage { get; private set; } =
            "Error: Please start your Steam Client before you run this example!";

        public string WelcomeNote { get; } = "- Press [Shift + Tab] to open the Steam Overlay -";

        // Do not use 'SteamApi.IsSteamRunning()'! It's not reliable and slow
        //see: https://github.com/rlabrecque/Steamworks.NET/issues/30
        public static bool IsSteamRunning { get; set; } = false;

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
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<SpriteFont>(@"Font");

            if (IsSteamRunning)
            {
                // Get your trimmed Steam User Name.
                string steamUserName = SteamFriends.GetPersonaName();
                // Remove unsupported chars like emojis or other stuff our font cannot handle.
                steamUserName = ReplaceUnsupportedChars(Font, steamUserName);
                var userNameTrimmed = steamUserName.Trim();
                WelcomeMessage = $"Hello {userNameTrimmed}!";
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (IsSteamRunning) SteamAPI.RunCallbacks();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            // Draw WelcomeMessage.
            spriteBatch.DrawString(Font, WelcomeMessage,
                new Vector2(graphics.PreferredBackBufferWidth / 2f - Font.MeasureString(WelcomeMessage).X / 2f,
                    graphics.PreferredBackBufferHeight / 2f - Font.MeasureString(WelcomeMessage).Y / 2f -
                    (IsSteamRunning ? 20 : 0)), Color.GreenYellow);

            if (IsSteamRunning)
            {
                // Draw WelcomeNote.
                spriteBatch.DrawString(Font, WelcomeNote,
                    new Vector2(graphics.PreferredBackBufferWidth / 2f - Font.MeasureString(WelcomeNote).X / 2f,
                        graphics.PreferredBackBufferHeight / 2f - Font.MeasureString(WelcomeNote).Y / 2f + 20),
                    Color.GreenYellow);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
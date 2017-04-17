using System;

namespace Steamworks.Net_MonoGame_Integration.DesktopGL
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new SteamworksIntegration())
                game.Run();
        }
    }
}

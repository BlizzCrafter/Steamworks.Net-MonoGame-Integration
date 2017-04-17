using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Steamworks;

namespace AchievementHunter.Classes
{
    // This is a port of StatsAndAchievements.cpp from SpaceWar, the official Steamworks Example.
    public class StatsAndAchievements
    {
        private class Achievement_t
        {
            public Achievement m_eAchievementID;
            public string m_strName;
            public string m_strDescription;
            public bool m_bAchieved;

            /// <summary>
            /// Creates an Achievement. You must also mirror the data provided here in https://partner.steamgames.com/apps/achievements/yourappid
            /// </summary>
            /// <param name="achievement">The "API Name Progress Stat" used to uniquely identify the achievement.</param>
            /// <param name="name">The "Display Name" that will be shown to players in game and on the Steam Community.</param>
            /// <param name="desc">The "Description" that will be shown to players in game and on the Steam Community.</param>
            public Achievement_t(Achievement achievementID, string name, string desc)
            {
                m_eAchievementID = achievementID;
                m_strName = name;
                m_strDescription = desc;
                m_bAchieved = false;
            }
        }

        public enum Achievement : int
        {
            ACH_WIN_ONE_GAME,
            ACH_WIN_100_GAMES,
            ACH_HEAVY_FIRE,
            ACH_TRAVEL_FAR_ACCUM,
            ACH_TRAVEL_FAR_SINGLE,
        };

        private Achievement_t[] m_Achievements = new Achievement_t[] 
        {
            new Achievement_t(Achievement.ACH_WIN_ONE_GAME, "Winner", ""),
            new Achievement_t(Achievement.ACH_WIN_100_GAMES, "Champion", ""),
            new Achievement_t(Achievement.ACH_TRAVEL_FAR_ACCUM, "Interstellar", ""),
            new Achievement_t(Achievement.ACH_TRAVEL_FAR_SINGLE, "Orbiter", "")
        };

        // Our GameID
        private CGameID m_GameID;

        // Did we get the stats from Steam?
        private bool m_bRequestedStats;
        private bool m_bStatsValid;

        // Should we store stats this frame?
        private bool m_bStoreStats;

        // Current Stat details

        /// <summary>
        /// Accumulate distance traveled.
        /// </summary>
        /// <param name="flDistance">Distance to add.</param>
        public void AddDistanceTraveled(float flDistance)
        {
            m_flGameFeetTraveled += flDistance;
        }
        private float m_flGameFeetTraveled;

        // Persisted Stat details
        public int m_nTotalGamesPlayed;
        private int m_nTotalNumWins;
        private int m_nTotalNumLosses;
        private float m_flTotalFeetTraveled;
        private float m_flMaxFeetTraveled;

        protected Callback<UserStatsReceived_t> m_UserStatsReceived;
        protected Callback<UserStatsStored_t> m_UserStatsStored;
        protected Callback<UserAchievementStored_t> m_UserAchievementStored;

        /// <summary>
        /// We have stats data from Steam. It is authoritative, so update our data with those results now.
        /// </summary>
        /// <param name="pCallback"></param>
        private void OnUserStatsReceived(UserStatsReceived_t pCallback)
        {
            if (!AchievementSample.IsSteamRunning)
                return;

            // we may get callbacks for other games' stats arriving, ignore them
            if ((ulong)m_GameID == pCallback.m_nGameID)
            {
                if (EResult.k_EResultOK == pCallback.m_eResult)
                {
                     Console.WriteLine("Received stats and achievements from Steam\n");

                    m_bStatsValid = true;

                    // load achievements
                    foreach (Achievement_t ach in m_Achievements)
                    {
                        bool ret = SteamUserStats.GetAchievement(ach.m_eAchievementID.ToString(), out ach.m_bAchieved);
                        if (ret)
                        {
                            ach.m_strName = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "name");
                            ach.m_strDescription = SteamUserStats.GetAchievementDisplayAttribute(ach.m_eAchievementID.ToString(), "desc");
                        }
                        else
                        {
                            Console.WriteLine("SteamUserStats.GetAchievement failed for Achievement " + ach.m_eAchievementID + "\nIs it registered in the Steam Partner site?");
                        }
                    }

                    // load stats
                    SteamUserStats.GetStat("NumGames", out m_nTotalGamesPlayed);
                    SteamUserStats.GetStat("NumWins", out m_nTotalNumWins);
                    SteamUserStats.GetStat("NumLosses", out m_nTotalNumLosses);
                    SteamUserStats.GetStat("FeetTraveled", out m_flTotalFeetTraveled);
                    SteamUserStats.GetStat("MaxFeetTraveled", out m_flMaxFeetTraveled);
                }
                else
                {
                    Console.WriteLine("RequestStats - failed, " + pCallback.m_eResult);
                }
            }
        }

        /// <summary>
        /// Our stats data was stored!
        /// </summary>
        /// <param name="pCallback">Our callback.</param>
        private void OnUserStatsStored(UserStatsStored_t pCallback)
        {
            // we may get callbacks for other games' stats arriving, ignore them
            if ((ulong)m_GameID == pCallback.m_nGameID)
            {
                if (EResult.k_EResultOK == pCallback.m_eResult)
                {
                    Console.WriteLine("StoreStats - success");
                }
                else if (EResult.k_EResultInvalidParam == pCallback.m_eResult)
                {
                    // One or more stats we set broke a constraint. They've been reverted,
                    // and we should re-iterate the values now to keep in sync.
                    Console.WriteLine("StoreStats - some failed to validate");
                    // Fake up a callback here so that we re-load the values.
                    UserStatsReceived_t callback = new UserStatsReceived_t();
                    callback.m_eResult = EResult.k_EResultOK;
                    callback.m_nGameID = (ulong)m_GameID;
                    OnUserStatsReceived(callback);
                }
                else
                {
                    Console.WriteLine("StoreStats - failed, " + pCallback.m_eResult);
                }
            }
        }

        /// <summary>
        /// Unlock this achievement.
        /// </summary>
        /// <param name="achievement">This achievement get unlocked.</param>
        private void UnlockAchievement(Achievement_t achievement)
        {
            achievement.m_bAchieved = true;

            // the icon may change once it's unlocked
            //achievement.m_iIconImage = 0;

            // mark it down
            SteamUserStats.SetAchievement(achievement.m_eAchievementID.ToString());

            // Store stats end of frame
            m_bStoreStats = true;
        }

        /// <summary>
        /// An achievement was stored.
        /// </summary>
        /// <param name="pCallback">Our callback</param>
        private void OnAchievementStored(UserAchievementStored_t pCallback)
        {
            // We may get callbacks for other games' stats arriving, ignore them
            if ((ulong)m_GameID == pCallback.m_nGameID)
            {
                if (pCallback.m_nMaxProgress == 0)
                {
                    Console.WriteLine("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
                }
                else
                {
                    Console.WriteLine("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
                }
            }
        }

        public StatsAndAchievements()
        {
            // Cache the GameID for use in the Callbacks
            m_GameID = new CGameID(SteamUtils.GetAppID());
            
            m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
            m_UserAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

            // These need to be reset to get the stats upon an Assembly reload in the Editor.
            m_bRequestedStats = false;
            m_bStatsValid = false;
        }

        public void Update(GameTime gT)
        {
            if (!m_bRequestedStats)
            {
                // Is Steam Loaded? if no, can't get stats, done
                if (!AchievementSample.IsSteamRunning)
                {
                    m_bRequestedStats = true;
                    return;
                }

                // If yes, request our stats
                bool bSuccess = SteamUserStats.RequestCurrentStats();

                // This function should only return false if we weren't logged in, and we already checked that.
                // But handle it being false again anyway, just ask again later.
                m_bRequestedStats = bSuccess;
            }

            if (!m_bStatsValid)
                return;

            // Get info from sources
            // Evaluate achievements
            foreach (Achievement_t achievement in m_Achievements)
            {
                if (achievement.m_bAchieved)
                    continue;

                switch (achievement.m_eAchievementID)
                {
                    case Achievement.ACH_WIN_ONE_GAME:
                        if (m_nTotalNumWins != 0)
                        {
                            UnlockAchievement(achievement);
                        }
                        break;
                    case Achievement.ACH_WIN_100_GAMES:
                        if (m_nTotalNumWins >= 100)
                        {
                            UnlockAchievement(achievement);
                        }
                        break;
                    case Achievement.ACH_TRAVEL_FAR_ACCUM:
                        if (m_flTotalFeetTraveled >= 5280)
                        {
                            UnlockAchievement(achievement);
                        }
                        break;
                    case Achievement.ACH_TRAVEL_FAR_SINGLE:
                        if (m_flGameFeetTraveled >= 500)
                        {
                            UnlockAchievement(achievement);
                        }
                        break;
                }
            }

            //Store stats in the Steam database if necessary
            if (m_bStoreStats)
            {
                // already set any achievements in UnlockAchievement

                // set stats
                SteamUserStats.SetStat("NumGames", m_nTotalGamesPlayed);
                SteamUserStats.SetStat("NumWins", m_nTotalNumWins);
                SteamUserStats.SetStat("NumLosses", m_nTotalNumLosses);
                SteamUserStats.SetStat("FeetTraveled", m_flTotalFeetTraveled);
                SteamUserStats.SetStat("MaxFeetTraveled", m_flMaxFeetTraveled);

                bool bSuccess = SteamUserStats.StoreStats();
                // If this failed, we never sent anything to the server, try
                // again later.
                m_bStoreStats = !bSuccess;
            }
        }

        public void Draw(SpriteBatch sB)
        {
            if (!AchievementSample.IsSteamRunning)
            {
                string errorMessage = "Error: Please start your Steam Client before you run this example!";

                sB.DrawString(AchievementSample.Font, errorMessage, new Vector2(
                        AchievementSample.ScreenWidth / 2f - AchievementSample.Font.MeasureString(errorMessage).X / 2f,
                        AchievementSample.ScreenHeight / 2f - AchievementSample.Font.MeasureString(errorMessage).Y / 2f), 
                        Color.GreenYellow);
                return;
            }

            string stats = $@"
DistanceTraveled: {m_flGameFeetTraveled}

NumGames: {m_nTotalGamesPlayed}
NumWins: {m_nTotalNumWins}
NumLosses: {m_nTotalNumLosses}
FeetTraveled: {m_flTotalFeetTraveled}
MaxFeetTraveled: {m_flMaxFeetTraveled}";

            // Draw Stats
            sB.DrawString(AchievementSample.Font, 
                AchievementSample.ReplaceUnsupportedChars(AchievementSample.Font, stats), new Vector2(20, 20), Color.White);

            //Draw Achievements
            for (int i = 0; i < m_Achievements.Length; i++)
            {
                string achievments = $@"
ID: {m_Achievements[i].m_eAchievementID.ToString()}
Name: {m_Achievements[i].m_strName}
Description: {m_Achievements[i].m_strDescription}
Achieved: {m_Achievements[i].m_bAchieved}";

                string drawString = AchievementSample.ReplaceUnsupportedChars(AchievementSample.Font, achievments);

                sB.DrawString(AchievementSample.Font, drawString, new Vector2(AchievementSample.ScreenWidth -
                        AchievementSample.Font.MeasureString(drawString).X - 20,
                        AchievementSample.Font.MeasureString(drawString).Y * i), Color.White);
            }                    
        }

        #region DEBUGGING

        //Reset the traveled distance
        public void ResetDistanceTraveled() => m_flGameFeetTraveled = 0;

        /// <summary>
        /// Game state has changed (We use this to reset all achievements so we can test the unlocking of them again).
        /// </summary>
        /// <param name="eNewState"></param>
        public void OnGameStateChange(AchievementSample.EClientGameState eNewState)
        {
            if (!m_bStatsValid)
                return;

            if (eNewState == AchievementSample.EClientGameState.k_EClientGameWinner ||
                eNewState == AchievementSample.EClientGameState.k_EClientGameLoser)
            {
                if (eNewState == AchievementSample.EClientGameState.k_EClientGameWinner)
                {
                    m_nTotalNumWins++;
                }
                else
                {
                    m_nTotalNumLosses++;
                }

                // Tally games
                m_nTotalGamesPlayed++;

                // Accumulate distances
                m_flTotalFeetTraveled += m_flGameFeetTraveled;

                // New max?
                if (m_flGameFeetTraveled > m_flMaxFeetTraveled)
                    m_flMaxFeetTraveled = m_flGameFeetTraveled;

                ResetDistanceTraveled();

                // We want to update stats the next frame.
                m_bStoreStats = true;
            }
        }

        #endregion
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace PurrcelainPanic
{
    public static class OnlineRanking
    {
        public const string LeaderboardId = "High_Scores";
        public static string PlayerName => PlayerPrefs.GetString("Ranking.PlayerName", "");
        static Task connection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetConnection() => connection = null;

        public static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) &&
            name.Length <= 16 && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');

        public static void SaveName(string name)
        {
            if (!ValidName(name))
                throw new ArgumentException("이름은 한글·영문·숫자·_·-로 1~16자 입력하세요.");
            PlayerPrefs.SetString("Ranking.PlayerName", name);
            PlayerPrefs.Save();
        }

        static async Task ConnectAsync()
        {
            if (connection == null || connection.IsFaulted || connection.IsCanceled ||
                (connection.IsCompleted && UnityServices.State != ServicesInitializationState.Initialized))
                connection = InitializeAsync();
            await connection;
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static async Task InitializeAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        static async Task SyncNameAsync()
        {
            if (ValidName(PlayerName))
                await AuthenticationService.Instance.UpdatePlayerNameAsync(PlayerName);
        }

        public static async Task<LeaderboardScoresPage> TopAsync()
        {
            await ConnectAsync();
            return await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId);
        }

        public static async Task<(int rank, double bestScore)> SubmitAsync(int score)
        {
            await ConnectAsync();
            await SyncNameAsync();
            await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
            var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
            return (entry.Rank + 1, entry.Score);
        }
    }
}

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameLogic
{
    public enum RewardedAdState { Idle, Loading, Ready, Showing, Failed }

    /// <summary>广告管理入口。每个 adId 一个执行流，沿用 SDK 回调和统一交互锁。</summary>
    public static class AdSystem
    {
        public const float LoadingTimeoutSeconds = 15f;
        public static event Action<string> StateChanged;
        private static readonly Dictionary<string, RewardedAdFlow> RewardedAds = new Dictionary<string, RewardedAdFlow>();
        public const float CloseCallbackTimeoutSeconds = 60f;

        private static RewardedAdFlow GetFlow(string adId)
        {
            adId = adId ?? string.Empty;
            if (!RewardedAds.TryGetValue(adId, out var flow))
            {
                flow = new RewardedAdFlow(adId);
                RewardedAds.Add(adId, flow);
            }
            return flow;
        }
        public static RewardedAdState GetState(string adId) =>
            RewardedAds.TryGetValue(adId ?? string.Empty, out var flow) ? flow.State : RewardedAdState.Idle;
        public static bool IsAdAvailable(string adId) =>
            RewardedAds.TryGetValue(adId ?? string.Empty, out var flow) && flow.IsAdAvailable;
        public static bool IsLoading(string adId) => GetState(adId) == RewardedAdState.Loading;
        public static bool IsBusy(string adId) =>
            RewardedAds.TryGetValue(adId ?? string.Empty, out var flow) && flow.IsBusy;
        public static void Preload(string adId) => GetFlow(adId).Preload();
        public static void EnterPage(params string[] adIds)
        {
            foreach (var id in new HashSet<string>(adIds)) GetFlow(id).EnterPage();
        }
        public static void ExitPage(params string[] adIds)
        {
            foreach (var id in new HashSet<string>(adIds))
                if (RewardedAds.TryGetValue(id ?? string.Empty, out var flow)) flow.ExitPage();
        }
        public static UniTask<bool> ShowRewardedAdAsync(string adId) => GetFlow(adId).ShowRewardedAdAsync();
        public static void Release(string adId)
        {
            adId = adId ?? string.Empty;
            if (!RewardedAds.TryGetValue(adId, out var flow)) return;
            RewardedAds.Remove(adId);
            flow.Dispose();
        }
        public static void Dispose()
        {
            foreach (var flow in RewardedAds.Values) flow.Dispose();
            RewardedAds.Clear();
        }
        internal static void NotifyStateChanged(string adId) => StateChanged?.Invoke(adId);
    }
}

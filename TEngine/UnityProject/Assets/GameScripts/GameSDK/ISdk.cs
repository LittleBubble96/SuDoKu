namespace GameSDK
{
    public interface ISdk
    {
        void Login(System.Action<string> onSuccess, System.Action<string> onFailure);

        IRewardedVideoAd CreateRewardedVideoAd(string adId, System.Action onLoaded,
            System.Action<int ,string> onError, System.Action<bool> onClosed);

        string GetOpenId();

        void ShareAppMessage(string title);

        void NavigateToMiniGame(string appId);

        void CreateGameClubButton();

        void OpenGameClub();

        void ShowBanner(string adId);
        void HideBanner();
        void DestroyBanner();

        void PreloadInterstitial(string adId);
        bool TryShowInterstitial(System.Action onFinished);
        void DestroyInterstitial();

        void LoadRecommend();
        void ShowRecommend(System.Func<bool> canShow);

        void ReportGameStart();

        void UploadCompletedLevel(int levelId);

        void ReportEvent(string eventId, System.Collections.Generic.Dictionary<string, string> data);
    }
}

namespace GameSDK
{
    /// <summary>One instance per custom event. Initialized before SDK login.</summary>
    public static class BiManager
    {
        public static readonly Bi_SdkLogin SdkLogin = new Bi_SdkLogin();
        public static readonly Bi_StartupStep StartupStep = new Bi_StartupStep();
        public static readonly Bi_StartupComplete StartupComplete = new Bi_StartupComplete();
        public static readonly Bi_SudokuRoundStart SudokuRoundStart = new Bi_SudokuRoundStart();
        public static readonly Bi_SudokuRoundResume SudokuRoundResume = new Bi_SudokuRoundResume();
        public static readonly Bi_SudokuRoundComplete SudokuRoundComplete = new Bi_SudokuRoundComplete();
        public static readonly Bi_SudokuRoundAbandon SudokuRoundAbandon = new Bi_SudokuRoundAbandon();
        public static readonly Bi_SudokuHintUsed SudokuHintUsed = new Bi_SudokuHintUsed();
        public static readonly Bi_SudokuGuide SudokuGuide = new Bi_SudokuGuide();
        public static readonly Bi_DailyTrophyAwarded DailyTrophyAwarded = new Bi_DailyTrophyAwarded();
        public static readonly Bi_AdFeatureUnlocked AdFeatureUnlocked = new Bi_AdFeatureUnlocked();
        public static readonly Bi_BannerImpression BannerImpression = new Bi_BannerImpression();
        public static readonly Bi_BannerVisibleTime BannerVisibleTime = new Bi_BannerVisibleTime();
        public static readonly Bi_BannerError BannerError = new Bi_BannerError();
        public static readonly Bi_RewardedAdShow RewardedAdShow = new Bi_RewardedAdShow();
        public static readonly Bi_RewardedAdResult RewardedAdResult = new Bi_RewardedAdResult();
        public static readonly Bi_InterstitialAdShow InterstitialAdShow = new Bi_InterstitialAdShow();
        public static readonly Bi_InterstitialAdResult InterstitialAdResult = new Bi_InterstitialAdResult();
        public static readonly Bi_RewardClaimed RewardClaimed = new Bi_RewardClaimed();
        public static readonly Bi_HomeEntryClick HomeEntryClick = new Bi_HomeEntryClick();
        public static readonly Bi_OtherGameClick OtherGameClick = new Bi_OtherGameClick();
        public static readonly Bi_OtherGameNavigateResult OtherGameNavigateResult = new Bi_OtherGameNavigateResult();

        public static void Initialize() { }
    }
}

namespace GameSDK
{
    public sealed class Bi_SdkLogin : BiEvent
    {
        public override string EventName => "sdk_login";
        public string Status { get; private set; }
        public int Attempt { get; private set; }
        public string ErrorMsg { get; private set; }

        public Bi_SdkLogin SetData(string status, int attempt, string errorMsg)
        {
            Data.Clear();
            Status = status;
            Attempt = attempt;
            ErrorMsg = errorMsg;
            Put("status", Status);
            Put("attempt", Attempt);
            Put("error_msg", ErrorMsg);
            return this;
        }
    }

    public sealed class Bi_StartupStep : BiEvent
    {
        public override string EventName => "startup_step";
        public string StartupId { get; private set; }
        public int Index { get; private set; }
        public string Step { get; private set; }
        public string Status { get; private set; }
        public long ElapsedMs { get; private set; }
        public int Attempt { get; private set; }
        public string AppVersion { get; private set; }
        public string ResourceVersion { get; private set; }
        public string PlayMode { get; private set; }
        public long DurationMs { get; private set; }
        public string Reason { get; private set; }
        public string ErrorCode { get; private set; }
        public string ErrorMsg { get; private set; }
        public int DownloadCount { get; private set; }
        public long DownloadBytes { get; private set; }

        public Bi_StartupStep SetData(string startupId, int index, string step,
            string status, long elapsedMs, int attempt,
            string appVersion, string resourceVersion, string playMode,
            long durationMs = -1, string reason = null, string errorCode = null,
            string errorMsg = null, int downloadCount = -1, long downloadBytes = -1)
        {
            Data.Clear();
            StartupId = startupId;
            Index = index;
            Step = step;
            Status = status;
            ElapsedMs = elapsedMs;
            Attempt = attempt;
            AppVersion = appVersion;
            ResourceVersion = resourceVersion;
            PlayMode = playMode;
            DurationMs = durationMs;
            Reason = reason;
            ErrorCode = errorCode;
            ErrorMsg = errorMsg;
            DownloadCount = downloadCount;
            DownloadBytes = downloadBytes;
            Put("startup_id", StartupId);
            Put("index", Index);
            Put("step", Step);
            Put("status", Status);
            Put("elapsed_ms", ElapsedMs);
            Put("attempt", Attempt);
            Put("app_version", AppVersion);
            Put("resource_version", ResourceVersion);
            Put("play_mode", PlayMode);
            if (DurationMs >= 0) Put("duration_ms", DurationMs);
            if (Reason != null) Put("reason", Reason);
            if (ErrorCode != null) Put("error_code", ErrorCode);
            if (ErrorMsg != null) Put("error_msg", ErrorMsg);
            if (DownloadCount >= 0) Put("download_count", DownloadCount);
            if (DownloadBytes >= 0) Put("download_bytes", DownloadBytes);
            return this;
        }
    }

    public sealed class Bi_StartupComplete : BiEvent
    {
        public override string EventName => "startup_complete";
        public string StartupId { get; private set; }
        public int Index { get; private set; }
        public string Step { get; private set; }
        public string Status { get; private set; }
        public long ElapsedMs { get; private set; }
        public int Attempt { get; private set; }
        public string AppVersion { get; private set; }
        public string ResourceVersion { get; private set; }
        public string PlayMode { get; private set; }
        public long DurationMs { get; private set; }

        public Bi_StartupComplete SetData(string startupId, int index, string step,
            string status, long elapsedMs, int attempt,
            string appVersion, string resourceVersion, string playMode, long durationMs)
        {
            Data.Clear();
            StartupId = startupId;
            Index = index;
            Step = step;
            Status = status;
            ElapsedMs = elapsedMs;
            Attempt = attempt;
            AppVersion = appVersion;
            ResourceVersion = resourceVersion;
            PlayMode = playMode;
            DurationMs = durationMs;
            Put("startup_id", StartupId);
            Put("index", Index);
            Put("step", Step);
            Put("status", Status);
            Put("elapsed_ms", ElapsedMs);
            Put("attempt", Attempt);
            Put("app_version", AppVersion);
            Put("resource_version", ResourceVersion);
            Put("play_mode", PlayMode);
            Put("duration_ms", DurationMs);
            return this;
        }
    }

    public sealed class Bi_SudokuRoundStart : BiEvent
    {
        public override string EventName => "sudoku_round_start";
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public long DurationMs { get; private set; }
        public int ProgressPct { get; private set; }
        public int MistakeCount { get; private set; }
        public int HintCount { get; private set; }
        public string StartSource { get; private set; }

        public Bi_SudokuRoundStart SetData(string roundId, int difficulty, int levelId,
            string mode, string challengeDate, long durationMs,
            int progressPct, int mistakeCount, int hintCount,
            string startSource)
        {
            Data.Clear();
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            DurationMs = durationMs;
            ProgressPct = progressPct;
            MistakeCount = mistakeCount;
            HintCount = hintCount;
            StartSource = startSource;
            Put("round_id", RoundId);
            Put("difficulty", Difficulty);
            Put("level_id", LevelId);
            Put("mode", Mode);
            if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            Put("duration_ms", DurationMs);
            Put("progress_pct", ProgressPct);
            Put("mistake_count", MistakeCount);
            Put("hint_count", HintCount);
            Put("start_source", StartSource);
            return this;
        }
    }

    public sealed class Bi_SudokuRoundResume : BiEvent
    {
        public override string EventName => "sudoku_round_resume";
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public long DurationMs { get; private set; }
        public int ProgressPct { get; private set; }
        public int MistakeCount { get; private set; }
        public int HintCount { get; private set; }

        public Bi_SudokuRoundResume SetData(string roundId, int difficulty, int levelId,
            string mode, string challengeDate, long durationMs,
            int progressPct, int mistakeCount, int hintCount)
        {
            Data.Clear();
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            DurationMs = durationMs;
            ProgressPct = progressPct;
            MistakeCount = mistakeCount;
            HintCount = hintCount;
            Put("round_id", RoundId);
            Put("difficulty", Difficulty);
            Put("level_id", LevelId);
            Put("mode", Mode);
            if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            Put("duration_ms", DurationMs);
            Put("progress_pct", ProgressPct);
            Put("mistake_count", MistakeCount);
            Put("hint_count", HintCount);
            return this;
        }
    }

    public sealed class Bi_SudokuRoundComplete : BiEvent
    {
        public override string EventName => "sudoku_round_complete";
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public long DurationMs { get; private set; }
        public int ProgressPct { get; private set; }
        public int MistakeCount { get; private set; }
        public int HintCount { get; private set; }
        public int Score { get; private set; }
        public int Perfect { get; private set; }
        public int CurrentStreak { get; private set; }

        public Bi_SudokuRoundComplete SetData(string roundId, int difficulty, int levelId,
            string mode, string challengeDate, long durationMs,
            int progressPct, int mistakeCount, int hintCount,
            int score, int perfect, int currentStreak)
        {
            Data.Clear();
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            DurationMs = durationMs;
            ProgressPct = progressPct;
            MistakeCount = mistakeCount;
            HintCount = hintCount;
            Score = score;
            Perfect = perfect;
            CurrentStreak = currentStreak;
            Put("round_id", RoundId);
            Put("difficulty", Difficulty);
            Put("level_id", LevelId);
            Put("mode", Mode);
            if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            Put("duration_ms", DurationMs);
            Put("progress_pct", ProgressPct);
            Put("mistake_count", MistakeCount);
            Put("hint_count", HintCount);
            Put("score", Score);
            Put("perfect", Perfect);
            Put("current_streak", CurrentStreak);
            return this;
        }
    }

    public sealed class Bi_SudokuRoundAbandon : BiEvent
    {
        public override string EventName => "sudoku_round_abandon";
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public long DurationMs { get; private set; }
        public int ProgressPct { get; private set; }
        public int MistakeCount { get; private set; }
        public int HintCount { get; private set; }
        public string AbandonReason { get; private set; }

        public Bi_SudokuRoundAbandon SetData(string roundId, int difficulty, int levelId,
            string mode, string challengeDate, long durationMs,
            int progressPct, int mistakeCount, int hintCount,
            string abandonReason)
        {
            Data.Clear();
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            DurationMs = durationMs;
            ProgressPct = progressPct;
            MistakeCount = mistakeCount;
            HintCount = hintCount;
            AbandonReason = abandonReason;
            Put("round_id", RoundId);
            Put("difficulty", Difficulty);
            Put("level_id", LevelId);
            Put("mode", Mode);
            if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            Put("duration_ms", DurationMs);
            Put("progress_pct", ProgressPct);
            Put("mistake_count", MistakeCount);
            Put("hint_count", HintCount);
            Put("abandon_reason", AbandonReason);
            return this;
        }
    }

    public sealed class Bi_SudokuHintUsed : BiEvent
    {
        public override string EventName => "sudoku_hint_used";
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public string HintSource { get; private set; }
        public string AdRequestId { get; private set; }

        public Bi_SudokuHintUsed SetData(string roundId, int difficulty, int levelId,
            string mode, string challengeDate, string hintSource,
            string adRequestId = null)
        {
            Data.Clear();
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            HintSource = hintSource;
            AdRequestId = adRequestId;
            Put("round_id", RoundId);
            Put("difficulty", Difficulty);
            Put("level_id", LevelId);
            Put("mode", Mode);
            if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            Put("hint_source", HintSource);
            if (!string.IsNullOrEmpty(AdRequestId)) Put("ad_request_id", AdRequestId);
            return this;
        }
    }

    public sealed class Bi_SudokuGuide : BiEvent
    {
        public override string EventName => "sudoku_guide";
        public string GuideAction { get; private set; }
        public string GuideSource { get; private set; }
        public string RoundId { get; private set; }

        public Bi_SudokuGuide SetData(string guideAction, string guideSource, string roundId = null)
        {
            Data.Clear();
            GuideAction = guideAction;
            GuideSource = guideSource;
            RoundId = roundId;
            Put("guide_action", GuideAction);
            Put("guide_source", GuideSource);
            if (!string.IsNullOrEmpty(RoundId)) Put("round_id", RoundId);
            return this;
        }
    }

    public sealed class Bi_DailyTrophyAwarded : BiEvent
    {
        public override string EventName => "daily_trophy_awarded";
        public int TrophyId { get; private set; }
        public string TrophyMonth { get; private set; }
        public int CompletedDays { get; private set; }
        public int RequiredDays { get; private set; }
        public string AwardDate { get; private set; }
        public string AwardSource { get; private set; }
        public string RoundId { get; private set; }

        public Bi_DailyTrophyAwarded SetData(int trophyId, string trophyMonth, int completedDays,
            int requiredDays, string awardDate, string awardSource,
            string roundId = null)
        {
            Data.Clear();
            TrophyId = trophyId;
            TrophyMonth = trophyMonth;
            CompletedDays = completedDays;
            RequiredDays = requiredDays;
            AwardDate = awardDate;
            AwardSource = awardSource;
            RoundId = roundId;
            Put("trophy_id", TrophyId);
            Put("trophy_month", TrophyMonth);
            Put("completed_days", CompletedDays);
            Put("required_days", RequiredDays);
            Put("award_date", AwardDate);
            Put("award_source", AwardSource);
            if (!string.IsNullOrEmpty(RoundId)) Put("round_id", RoundId);
            return this;
        }
    }

    public sealed class Bi_AdFeatureUnlocked : BiEvent
    {
        public override string EventName => "ad_feature_unlocked";
        public string AdFeature { get; private set; }
        public int UnlockThreshold { get; private set; }
        public int CompletedNormalLevels { get; private set; }
        public int CompletedGames { get; private set; }
        public string RoundId { get; private set; }

        public Bi_AdFeatureUnlocked SetData(string adFeature, int unlockThreshold, int completedNormalLevels,
            int completedGames, string roundId = null)
        {
            Data.Clear();
            AdFeature = adFeature;
            UnlockThreshold = unlockThreshold;
            CompletedNormalLevels = completedNormalLevels;
            CompletedGames = completedGames;
            RoundId = roundId;
            Put("ad_feature", AdFeature);
            Put("unlock_threshold", UnlockThreshold);
            Put("completed_normal_levels", CompletedNormalLevels);
            Put("completed_games", CompletedGames);
            if (!string.IsNullOrEmpty(RoundId)) Put("round_id", RoundId);
            return this;
        }
    }

    public sealed class Bi_BannerImpression : BiEvent
    {
        public override string EventName => "banner_impression";
        public string AdUnitId { get; private set; }
        public string ExposureId { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public string AdPlacement => "board_banner";

        public Bi_BannerImpression SetData(string adUnitId, string exposureId, string roundId = null,
            int difficulty = 0, int levelId = 0, string mode = null,
            string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            ExposureId = exposureId;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("exposure_id", ExposureId);
            Put("ad_placement", AdPlacement);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_BannerVisibleTime : BiEvent
    {
        public override string EventName => "banner_visible_time";
        public string AdUnitId { get; private set; }
        public string ExposureId { get; private set; }
        public int SegmentSeq { get; private set; }
        public long VisibleDeltaMs { get; private set; }
        public string BannerReportReason { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public string AdPlacement => "board_banner";

        public Bi_BannerVisibleTime SetData(string adUnitId, string exposureId, int segmentSeq,
            long visibleDeltaMs, string bannerReportReason, string roundId = null,
            int difficulty = 0, int levelId = 0, string mode = null,
            string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            ExposureId = exposureId;
            SegmentSeq = segmentSeq;
            VisibleDeltaMs = visibleDeltaMs;
            BannerReportReason = bannerReportReason;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("exposure_id", ExposureId);
            Put("segment_seq", SegmentSeq);
            Put("visible_delta_ms", VisibleDeltaMs);
            Put("banner_report_reason", BannerReportReason);
            Put("ad_placement", AdPlacement);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_BannerError : BiEvent
    {
        public override string EventName => "banner_error";
        public string AdUnitId { get; private set; }
        public string ErrorCode { get; private set; }
        public string ErrorMsg { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }
        public string AdPlacement => "board_banner";

        public Bi_BannerError SetData(string adUnitId, string errorCode, string errorMsg,
            string roundId = null, int difficulty = 0, int levelId = 0,
            string mode = null, string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            ErrorCode = errorCode;
            ErrorMsg = errorMsg;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("error_code", ErrorCode);
            Put("error_msg", ErrorMsg);
            Put("ad_placement", AdPlacement);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_RewardedAdShow : BiEvent
    {
        public override string EventName => "rewarded_ad_show";
        public string AdUnitId { get; private set; }
        public string AdPlacement { get; private set; }
        public string AdRequestId { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }

        public Bi_RewardedAdShow SetData(string adUnitId, string adPlacement, string adRequestId,
            string roundId = null, int difficulty = 0, int levelId = 0,
            string mode = null, string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            AdPlacement = adPlacement;
            AdRequestId = adRequestId;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("ad_placement", AdPlacement);
            Put("ad_request_id", AdRequestId);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_RewardedAdResult : BiEvent
    {
        public override string EventName => "rewarded_ad_result";
        public string AdUnitId { get; private set; }
        public string AdPlacement { get; private set; }
        public string AdRequestId { get; private set; }
        public int AdWasShown { get; private set; }
        public string AdResult { get; private set; }
        public long AdElapsedMs { get; private set; }
        public string AdFailureReason { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }

        public Bi_RewardedAdResult SetData(string adUnitId, string adPlacement, string adRequestId,
            int adWasShown, string adResult, long adElapsedMs,
            string adFailureReason = null, string roundId = null, int difficulty = 0,
            int levelId = 0, string mode = null, string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            AdPlacement = adPlacement;
            AdRequestId = adRequestId;
            AdWasShown = adWasShown;
            AdResult = adResult;
            AdElapsedMs = adElapsedMs;
            AdFailureReason = adFailureReason;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("ad_placement", AdPlacement);
            Put("ad_request_id", AdRequestId);
            Put("ad_was_shown", AdWasShown);
            Put("ad_result", AdResult);
            Put("ad_elapsed_ms", AdElapsedMs);
            if (AdFailureReason != null) Put("ad_failure_reason", AdFailureReason);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_InterstitialAdShow : BiEvent
    {
        public override string EventName => "interstitial_ad_show";
        public string AdUnitId { get; private set; }
        public string AdPlacement { get; private set; }
        public string AdRequestId { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }

        public Bi_InterstitialAdShow SetData(string adUnitId, string adPlacement, string adRequestId,
            string roundId = null, int difficulty = 0, int levelId = 0,
            string mode = null, string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            AdPlacement = adPlacement;
            AdRequestId = adRequestId;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("ad_placement", AdPlacement);
            Put("ad_request_id", AdRequestId);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_InterstitialAdResult : BiEvent
    {
        public override string EventName => "interstitial_ad_result";
        public string AdUnitId { get; private set; }
        public string AdPlacement { get; private set; }
        public string AdRequestId { get; private set; }
        public int AdWasShown { get; private set; }
        public string AdResult { get; private set; }
        public long AdElapsedMs { get; private set; }
        public string AdFailureReason { get; private set; }
        public string RoundId { get; private set; }
        public int Difficulty { get; private set; }
        public int LevelId { get; private set; }
        public string Mode { get; private set; }
        public string ChallengeDate { get; private set; }

        public Bi_InterstitialAdResult SetData(string adUnitId, string adPlacement, string adRequestId,
            int adWasShown, string adResult, long adElapsedMs,
            string adFailureReason = null, string roundId = null, int difficulty = 0,
            int levelId = 0, string mode = null, string challengeDate = null)
        {
            Data.Clear();
            AdUnitId = adUnitId;
            AdPlacement = adPlacement;
            AdRequestId = adRequestId;
            AdWasShown = adWasShown;
            AdResult = adResult;
            AdElapsedMs = adElapsedMs;
            AdFailureReason = adFailureReason;
            RoundId = roundId;
            Difficulty = difficulty;
            LevelId = levelId;
            Mode = mode;
            ChallengeDate = challengeDate;
            Put("ad_unit_id", AdUnitId);
            Put("ad_placement", AdPlacement);
            Put("ad_request_id", AdRequestId);
            Put("ad_was_shown", AdWasShown);
            Put("ad_result", AdResult);
            Put("ad_elapsed_ms", AdElapsedMs);
            if (AdFailureReason != null) Put("ad_failure_reason", AdFailureReason);
            if (!string.IsNullOrEmpty(RoundId))
            {
                Put("round_id", RoundId);
                Put("difficulty", Difficulty);
                Put("level_id", LevelId);
                Put("mode", Mode);
                if (ChallengeDate != null) Put("challenge_date", ChallengeDate);
            }
            return this;
        }
    }

    public sealed class Bi_RewardClaimed : BiEvent
    {
        public override string EventName => "reward_claimed";
        public string RewardSource { get; private set; }
        public string ClaimMethod { get; private set; }
        public int RewardMultiplier { get; private set; }
        public int CoinsGranted { get; private set; }
        public int HintsGranted { get; private set; }
        public string RoundId { get; private set; }
        public string RequestId { get; private set; }

        public Bi_RewardClaimed SetData(string rewardSource, string claimMethod, int rewardMultiplier,
            int coinsGranted, int hintsGranted, string roundId = null,
            string requestId = null)
        {
            Data.Clear();
            RewardSource = rewardSource;
            ClaimMethod = claimMethod;
            RewardMultiplier = rewardMultiplier;
            CoinsGranted = coinsGranted;
            HintsGranted = hintsGranted;
            RoundId = roundId;
            RequestId = requestId;
            Put("reward_source", RewardSource);
            Put("claim_method", ClaimMethod);
            Put("reward_multiplier", RewardMultiplier);
            Put("coins_granted", CoinsGranted);
            Put("hints_granted", HintsGranted);
            if (!string.IsNullOrEmpty(RoundId)) Put("round_id", RoundId);
            if (!string.IsNullOrEmpty(RequestId)) Put("ad_request_id", RequestId);
            return this;
        }
    }

    public sealed class Bi_HomeEntryClick : BiEvent
    {
        public override string EventName => "home_entry_click";
        public string Entry { get; private set; }

        public Bi_HomeEntryClick SetData(string entry)
        {
            Data.Clear();
            Entry = entry;
            Put("entry", Entry);
            return this;
        }
    }

    public sealed class Bi_OtherGameClick : BiEvent
    {
        public override string EventName => "other_game_click";
        public string TargetGame { get; private set; }

        public Bi_OtherGameClick SetData(string targetGame)
        {
            Data.Clear();
            TargetGame = targetGame;
            Put("target_game", TargetGame);
            return this;
        }
    }

    public sealed class Bi_OtherGameNavigateResult : BiEvent
    {
        public override string EventName => "other_game_navigate_result";
        public string TargetGame { get; private set; }
        public string NavigationResult { get; private set; }
        public string NavigationReason { get; private set; }

        public Bi_OtherGameNavigateResult SetData(string targetGame, string navigationResult, string navigationReason = null)
        {
            Data.Clear();
            TargetGame = targetGame;
            NavigationResult = navigationResult;
            NavigationReason = navigationReason;
            Put("target_game", TargetGame);
            Put("navigation_result", NavigationResult);
            if (NavigationReason != null) Put("navigation_reason", NavigationReason);
            return this;
        }
    }

}

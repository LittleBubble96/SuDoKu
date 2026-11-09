using System;
using System.Collections.Generic;
using GameLogic.Sudoku;

namespace GameLogic.Data
{
    [Serializable]
    public sealed class GamePropData
    {
        public int coinCount;
        public int tipCount = 2;
    }

    [Serializable]
    public sealed class GameCacheData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public SudokuSaveData sudokuSaveData;
        public SudokuPoolCursor[] sudokuPools;
        public List<SudokuDailySaveData> dailyChallenges = new List<SudokuDailySaveData>();
        public List<SudokuTrophyAward> trophyAwards = new List<SudokuTrophyAward>();
#if !TE_RELEASE
        [Serializable]
        public sealed class TrophyProgressOverride
        {
            public int id;
            public int completedDays;
            public int dailyCompletedAtOverride;
        }

        public List<TrophyProgressOverride> trophyProgressOverrides = new List<TrophyProgressOverride>();
#endif
        public string activeDailyDate;
        public SudokuSettlementData settlement;
        public int signInDays;
        public string signInDate;
        public DailyTaskData dailyTaskData = new DailyTaskData();
        public SudokuStatisticsData[] statistics = new SudokuStatisticsData[6];
        public GamePropData gamePropData = new GamePropData();
        public GameSettingsData gameSettingsData = new GameSettingsData();

        public void InitAll()
        {
            if (statistics == null || statistics.Length != 6) Array.Resize(ref statistics, 6);
            for (int i = 0; i < statistics.Length; i++)
                if (statistics[i] == null) statistics[i] = new SudokuStatisticsData();
            if (dailyTaskData == null) dailyTaskData = new DailyTaskData();
            if (gamePropData == null) gamePropData = new GamePropData();
            if (dailyChallenges == null) dailyChallenges = new List<SudokuDailySaveData>();
            if (trophyAwards == null) trophyAwards = new List<SudokuTrophyAward>();
#if !TE_RELEASE
            if (trophyProgressOverrides == null) trophyProgressOverrides = new List<TrophyProgressOverride>();
#endif
            if (gameSettingsData == null) gameSettingsData = new GameSettingsData();
            gamePropData.coinCount = Math.Max(0, gamePropData.coinCount);
            gamePropData.tipCount = Math.Max(0, gamePropData.tipCount);
            if (gameSettingsData.themeId < 0 || gameSettingsData.themeId > 5) gameSettingsData.themeId = 0;
            if ((int)gameSettingsData.textSize < 0 || (int)gameSettingsData.textSize > 2) gameSettingsData.textSize = SudokuTextSize.Medium;
            if (float.IsNaN(gameSettingsData.MusicVolume) || float.IsInfinity(gameSettingsData.MusicVolume)) gameSettingsData.MusicVolume = 1;
            if (float.IsNaN(gameSettingsData.SoundVolume) || float.IsInfinity(gameSettingsData.SoundVolume)) gameSettingsData.SoundVolume = 1;
            gameSettingsData.MusicVolume = Math.Max(0, Math.Min(1, gameSettingsData.MusicVolume));
            gameSettingsData.SoundVolume = Math.Max(0, Math.Min(1, gameSettingsData.SoundVolume));
        }
    }
}

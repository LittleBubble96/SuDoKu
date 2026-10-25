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
    public sealed class GameSettingsData
    {
        public int themeId;
        public float MusicVolume = 1f;
        public float SoundVolume = 1f;
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
        public string activeDailyDate;
        public GamePropData gamePropData = new GamePropData();
        public GameSettingsData gameSettingsData = new GameSettingsData();

        public void InitAll()
        {
            if (gamePropData == null) gamePropData = new GamePropData();
            if (dailyChallenges == null) dailyChallenges = new List<SudokuDailySaveData>();
            if (trophyAwards == null) trophyAwards = new List<SudokuTrophyAward>();
            if (gameSettingsData == null) gameSettingsData = new GameSettingsData();
            gamePropData.coinCount = Math.Max(0, gamePropData.coinCount);
            gamePropData.tipCount = Math.Max(0, gamePropData.tipCount);
            if (gameSettingsData.themeId < 0 || gameSettingsData.themeId > 5) gameSettingsData.themeId = 0;
            if (float.IsNaN(gameSettingsData.MusicVolume) || float.IsInfinity(gameSettingsData.MusicVolume)) gameSettingsData.MusicVolume = 1;
            if (float.IsNaN(gameSettingsData.SoundVolume) || float.IsInfinity(gameSettingsData.SoundVolume)) gameSettingsData.SoundVolume = 1;
            gameSettingsData.MusicVolume = Math.Max(0, Math.Min(1, gameSettingsData.MusicVolume));
            gameSettingsData.SoundVolume = Math.Max(0, Math.Min(1, gameSettingsData.SoundVolume));
        }
    }
}

using System;
using GameLogic.Data;

namespace GameLogic.Sudoku
{
    /// <summary>普通对局和每日挑战共用六档统计；存档统一由对局管理器提交。</summary>
    public sealed class SudokuStatisticsManager
    {
        private readonly SudokuStatisticsData[] _data;

        public SudokuStatisticsManager(GameCacheData cache) { _data = cache.statistics; }
        public SudokuStatisticsData Get(SudokuDifficulty difficulty) => _data[(int)difficulty];
        public void Begin(SudokuDifficulty difficulty) => Get(difficulty).played++;
        public void AddTime(SudokuDifficulty difficulty, double seconds) => Get(difficulty).totalSeconds += seconds;
        public void Abandon(SudokuDifficulty difficulty) => Get(difficulty).currentStreak = 0;

        public void Complete(SudokuDifficulty difficulty, double seconds, bool perfect)
        {
            var data = Get(difficulty);
            if (data.won == 0 || seconds < data.bestSeconds) data.bestSeconds = seconds;
            data.won++;
            if (perfect) data.perfectWins++;
            data.winSeconds += seconds;
            data.currentStreak++;
            data.bestStreak = Math.Max(data.bestStreak, data.currentStreak);
        }

        // GM 填题会排除整局，避免改变已玩次数、胜率和累计用时。
        public void Exclude(SudokuDifficulty difficulty, double seconds)
        {
            var data = Get(difficulty);
            data.played--;
            data.totalSeconds = Math.Max(0, data.totalSeconds - seconds);
        }

        public void Clear()
        {
            for (int i = 0; i < _data.Length; i++) _data[i] = new SudokuStatisticsData();
        }
    }
}

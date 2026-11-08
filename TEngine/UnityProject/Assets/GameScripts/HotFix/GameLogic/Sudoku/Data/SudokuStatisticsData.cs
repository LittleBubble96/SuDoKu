using System;

namespace GameLogic.Data
{
    /// <summary>每个难度仅保存汇总，胜率和平均用时由累计值计算。</summary>
    [Serializable]
    public sealed class SudokuStatisticsData
    {
        public int played, won, perfectWins, currentStreak, bestStreak;
        public double bestSeconds, winSeconds, totalSeconds;
        public int bestScore;

        public double WinRate => played == 0 ? 0 : won * 100.0 / played;
        public double AverageSeconds => won == 0 ? 0 : winSeconds / won;
    }
}

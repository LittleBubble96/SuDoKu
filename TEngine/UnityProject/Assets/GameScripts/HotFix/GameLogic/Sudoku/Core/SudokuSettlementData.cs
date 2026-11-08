using System;

namespace GameLogic.Sudoku
{
    /// <summary>通关时冻结的成绩和奖励。领取标记与道具余额在同一份存档中提交。</summary>
    [Serializable]
    public sealed class SudokuSettlementData
    {
        public string id;
        public int difficulty, score, bestScore, mistakes, hints, coins, tips;
        public double seconds, bestSeconds;
        public bool daily;
        public string dailyDate;
        public int claimedMultiplier;
        public bool IsClaimed => claimedMultiplier != 0;
    }
}

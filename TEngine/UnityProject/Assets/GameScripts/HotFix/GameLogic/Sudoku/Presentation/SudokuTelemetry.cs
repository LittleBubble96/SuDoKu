using System;
using GameSDK;

namespace GameLogic.Sudoku
{
    /// <summary>业务时间换算及广告对局上下文；事件字段由各 Bi 类负责。</summary>
    public static class SudokuTelemetry
    {
        public static long Milliseconds(double seconds) => (long)Math.Max(0, seconds * 1000);
        public static void SetAdRound(SudokuGameController game) =>
            AdTelemetry.SetRound(game.RoundId, (int)game.Session.Difficulty, game.Session.LevelIndex,
                game.IsDaily, game.IsDaily ? SudokuDailyRules.DateKey(game.DailyDate) : null);
    }
}

using System;
using System.Collections.Generic;
using GameConfig.settlement;

namespace GameLogic.Sudoku
{
    public sealed class SudokuScoringRules
    {
        public readonly int BaseScore, ReferenceSeconds, SpeedPercent, MistakePercent, HintPercent, Coins, Tips;

        public SudokuScoringRules(int baseScore, int referenceSeconds, int speedPercent = 50,
            int mistakePercent = 5, int hintPercent = 10, int coins = 100, int tips = 1)
        {
            if (baseScore <= 0 || referenceSeconds <= 0 || speedPercent < 0 || speedPercent > 100 ||
                mistakePercent < 0 || mistakePercent > 100 || hintPercent < 0 || hintPercent > 100 || coins < 0 || tips < 0)
                throw new ArgumentException("结算配置的分数、用时、百分比或奖励无效。");
            BaseScore = baseScore; ReferenceSeconds = referenceSeconds; SpeedPercent = speedPercent;
            MistakePercent = mistakePercent; HintPercent = hintPercent; Coins = coins; Tips = tips;
        }

        public int Calculate(double seconds, int mistakes, int hints)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) seconds = ReferenceSeconds;
            double speed = Math.Floor(BaseScore * (SpeedPercent / 100d) * Math.Max(0, 1 - Math.Max(0, seconds) / ReferenceSeconds));
            double penalty = BaseScore * (Math.Max(0, mistakes) * (MistakePercent / 100d) + Math.Max(0, hints) * (HintPercent / 100d));
            return (int)Math.Min(int.MaxValue, Math.Max(0, Math.Floor(BaseScore + speed - penalty + 0.000001)));
        }

        internal static SudokuScoringRules[] Load(IEnumerable<ConfSettlement> configs)
        {
            // 无配置参数供离线规则验证及既有构造调用使用。正式入口总是传入 Luban 配置。
            if (configs == null) return new[] { new SudokuScoringRules(100, 180), new SudokuScoringRules(200, 300),
                new SudokuScoringRules(400, 600), new SudokuScoringRules(600, 900),
                new SudokuScoringRules(800, 1200), new SudokuScoringRules(1000, 1800) };
            var result = new SudokuScoringRules[6];
            foreach (var c in configs)
            {
                if (c.Id < 0 || c.Id >= 6 || result[c.Id] != null) throw new ArgumentException("结算配置难度重复或越界。");
                result[c.Id] = new SudokuScoringRules(c.BaseScore, c.ReferenceSeconds, c.SpeedPercent,
                    c.MistakePercent, c.HintPercent, c.Coins, c.Tips);
            }
            for (int i = 0; i < result.Length; i++) if (result[i] == null) throw new ArgumentException("结算配置必须包含六档难度。");
            return result;
        }
    }
}

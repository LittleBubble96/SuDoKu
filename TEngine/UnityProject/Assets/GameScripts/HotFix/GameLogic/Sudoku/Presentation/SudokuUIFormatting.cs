using GameLogic.Localization;

namespace GameLogic.Sudoku
{
    /// <summary>首页、难度弹窗和局内共用的显示格式。</summary>
    public static class SudokuUIFormatting
    {
        private static readonly string[] DifficultyKeys =
        {
            LanguageKey.difficulty_quick, LanguageKey.difficulty_easy,
            LanguageKey.difficulty_medium, LanguageKey.difficulty_hard,
            LanguageKey.difficulty_expert, LanguageKey.difficulty_master
        };

        public static string Difficulty(SudokuDifficulty difficulty) =>
            LocalizationHelper.GetLocalText(DifficultyKeys[(int)difficulty]);

        public static string Time(double elapsed)
        {
            long seconds = (long)System.Math.Max(0, elapsed);
            return (seconds / 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
        }
    }
}

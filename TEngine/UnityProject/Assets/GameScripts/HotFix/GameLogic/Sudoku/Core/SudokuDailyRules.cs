using System;
using System.Globalization;

namespace GameLogic.Sudoku
{
    /// <summary>2026 年起的每日挑战。日期只决定难度，首次开始才从玩家关卡池取题。</summary>
    public static class SudokuDailyRules
    {
        public static readonly DateTime FirstDate = new DateTime(2026, 1, 1);

        public static string DateKey(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static bool TryParseDate(string key, out DateTime date) =>
            DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) && date >= FirstDate;

        public static bool CanStart(DateTime date, DateTime today) => date.Date >= FirstDate && date.Date <= today.Date;

        public static SudokuDifficulty Difficulty(DateTime date)
        {
            if (date.Date < FirstDate) throw new ArgumentOutOfRangeException(nameof(date));
            return (SudokuDifficulty)((date.Date - FirstDate).Days % 6);
        }

        public static DateTime Month(DateTime date) => new DateTime(date.Year, date.Month, 1);

        public static int MondayOffset(DateTime month) => ((int)Month(month).DayOfWeek + 6) % 7;
    }
}

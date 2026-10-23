using System;

namespace GameLogic.Sudoku
{
    [Flags]
    public enum SudokuCompletedRegion
    {
        None = 0,
        Row = 1,
        Column = 2,
        Box = 4
    }

    /// <summary>完成区域判定和扩散顺序，不依赖界面或动画状态。</summary>
    public static class SudokuCompletion
    {
        public static SudokuCompletedRegion GetCompletedRegions(byte[] board, byte[] solution, int origin)
        {
            if (origin < 0 || origin >= SudokuBoard.CellCount || board == null || solution == null ||
                board.Length != SudokuBoard.CellCount || solution.Length != SudokuBoard.CellCount)
                return SudokuCompletedRegion.None;

            var regions = SudokuCompletedRegion.None;
            if (IsComplete(board, solution, origin / 9)) regions |= SudokuCompletedRegion.Row;
            if (IsComplete(board, solution, 9 + origin % 9)) regions |= SudokuCompletedRegion.Column;
            if (IsComplete(board, solution, 18 + SudokuBoard.Box(origin))) regions |= SudokuCompletedRegion.Box;
            return regions;
        }

        private static bool IsComplete(byte[] board, byte[] solution, int unit)
        {
            foreach (int cell in SudokuBoard.Units[unit])
                if (board[cell] == 0 || board[cell] != solution[cell]) return false;
            return true;
        }

        /// <summary>起点为 0，按上下左右的格子距离向外扩散；不在完成区域内返回 -1。</summary>
        public static int WaveStep(int origin, int cell, SudokuCompletedRegion regions)
        {
            if (origin < 0 || origin >= 81 || cell < 0 || cell >= 81) return -1;
            bool included =
                ((regions & SudokuCompletedRegion.Row) != 0 && cell / 9 == origin / 9) ||
                ((regions & SudokuCompletedRegion.Column) != 0 && cell % 9 == origin % 9) ||
                ((regions & SudokuCompletedRegion.Box) != 0 && SudokuBoard.Box(cell) == SudokuBoard.Box(origin));
            return included ? Math.Abs(cell / 9 - origin / 9) + Math.Abs(cell % 9 - origin % 9) : -1;
        }
    }
}

using System;

namespace GameLogic.Sudoku
{
    [Serializable]
    public sealed class SudokuPoolCursor
    {
        public string packSignature;
        public int nextIndex;
    }

    [Serializable]
    public sealed class SudokuDailySaveData
    {
        public string date;
        public SudokuSaveData game;
        public bool completed;
        public int progressPercent;
    }
}

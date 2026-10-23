using System;

namespace GameLogic.Sudoku
{
    [Serializable]
    public sealed class SudokuCompletedData
    {
        public string packSignature;
        public int[] indices = Array.Empty<int>();
    }

    /// <summary>只保存玩家填写状态；答案始终在进入题目时求解，不写入存档。</summary>
    [Serializable]
    public sealed class SudokuSaveData
    {
        public int difficulty, puzzleIndex;
        public string packSignature;
        public int[] values, notes;
        public SudokuCompletedData[] completed;
        public double elapsedSeconds;
        public int selectedCell = -1;
        public bool noteMode;
    }
}

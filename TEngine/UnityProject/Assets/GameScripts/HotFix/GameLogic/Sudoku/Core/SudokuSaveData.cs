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
        // 每日挑战保留原题面，后续更新题库仍可继续历史对局；不保存答案。
        public int[] puzzle;
        public int[] values, notes;
        public SudokuCompletedData[] completed;
        public double elapsedSeconds;
        public int selectedCell = -1;
        public bool noteMode;
    }
}

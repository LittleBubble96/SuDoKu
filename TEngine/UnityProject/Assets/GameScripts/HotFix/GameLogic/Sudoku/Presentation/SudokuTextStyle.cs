namespace GameLogic.Sudoku
{
    public enum SudokuTextSize { Large, Medium, Small }

    /// <summary>棋盘数字与笔记的三档 TMP 参数，顺序与设置下拉框一致。</summary>
    public readonly struct SudokuTextStyle
    {
        public readonly float ValueSize, NoteSize, CharacterSpacing, LineSpacing;
        private SudokuTextStyle(float valueSize, float noteSize, float characterSpacing, float lineSpacing)
        {
            ValueSize = valueSize; NoteSize = noteSize;
            CharacterSpacing = characterSpacing; LineSpacing = lineSpacing;
        }
        public static SudokuTextStyle ForSize(SudokuTextSize size)
        {
            switch (size)
            {
                case SudokuTextSize.Large: return new SudokuTextStyle(80, 30, 25, 0);
                case SudokuTextSize.Small: return new SudokuTextStyle(50, 20, 57, 45);
                default: return new SudokuTextStyle(65, 25, 47, 15);
            }
        }
    }
}

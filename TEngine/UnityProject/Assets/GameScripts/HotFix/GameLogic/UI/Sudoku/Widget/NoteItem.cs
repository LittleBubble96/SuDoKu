using GameLogic.Sudoku;
using TMPro;

namespace GameLogic
{
    /// <summary>笔记的单个固定槽位；没有候选数时只清空文字，保留布局占位。</summary>
    public sealed class NoteItem : UIWidget
    {
        private TMP_Text _text;
        private UIThemeColor _color;
        private SudokuTextSize? _textSize;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _text = FindChildComponent<TMP_Text>("m_tmp_Notes");
            _text.raycastTarget = false;
            _color = UIThemeColor.Bind(_text, SudokuThemeRole.MutedText);
        }

        public void Refresh(int digit, bool present, bool highlighted, SudokuTextSize size)
        {
            if (_textSize != size)
            {
                _textSize = size;
                var style = SudokuTextStyle.ForSize(size);
                _text.enableAutoSizing = false;
                _text.enableWordWrapping = true;
                _text.fontSize = style.NoteSize;
                _text.characterSpacing = style.CharacterSpacing;
                _text.lineSpacing = style.LineSpacing;
            }
            _text.text = present ? digit.ToString() : string.Empty;
            _color.SetRole(present && highlighted ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
        }

        public void ResetDisplay()
        {
            _text.text = string.Empty;
            _color.SetRole(SudokuThemeRole.MutedText);
        }
    }
}

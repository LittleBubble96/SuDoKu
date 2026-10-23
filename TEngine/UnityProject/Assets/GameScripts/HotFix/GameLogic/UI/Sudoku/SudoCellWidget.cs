using System;
using System.Text;
using GameLogic.Sudoku;
using TMPro;

namespace GameLogic
{
    public sealed class SudoCellWidget : UIWidget
    {
        private XYButton _button;
        private TMP_Text _value, _notes;
        private UIThemeColor _backgroundColor, _valueColor;
        private Action _clicked;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>(gameObject);
            _value = FindChildComponent<TMP_Text>("m_tmp_Value");
            _notes = FindChildComponent<TMP_Text>("m_tmp_Notes");
            _backgroundColor = gameObject.GetComponent<UIThemeColor>();
            _valueColor = _value.GetComponent<UIThemeColor>();
            _button.OnAddListener(() => _clicked?.Invoke());
        }
        public void Bind(Action clicked) => _clicked = clicked;
        public void SetCompletionFlash(float weight) => _backgroundColor.SetOverlay(SudokuThemeRole.CompletionFlash, weight);
        public void Refresh(SudokuSession session, int index, int selected, bool canInteract)
        {
            int value = session.Value(index);
            int selectedValue = selected >= 0 ? session.Value(selected) : 0;
            bool related = selected >= 0 && (index / 9 == selected / 9 || index % 9 == selected % 9 || SudokuBoard.Box(index) == SudokuBoard.Box(selected));
            _backgroundColor.SetRole(index == selected ? SudokuThemeRole.Selected : selectedValue != 0 && value == selectedValue ? SudokuThemeRole.Matching : related ? SudokuThemeRole.Related : SudokuThemeRole.Surface);
            _valueColor.SetRole(session.HasConflict(index) ? SudokuThemeRole.Error : session.IsGiven(index) ? SudokuThemeRole.Text : SudokuThemeRole.Accent);
            _value.text = value == 0 ? "" : value.ToString();
            _notes.text = value == 0 ? NotesString(session.Notes(index)) : "";
            _button.Interactable = canInteract;
        }
        private static string NotesString(int mask)
        {
            if (mask == 0) return "";
            var text = new StringBuilder(17);
            for (int d = 1; d <= 9; d++)
            {
                text.Append((mask & (1 << d)) != 0 ? (char)('0' + d) : ' ');
                if (d < 9) text.Append(d % 3 == 0 ? '\n' : ' ');
            }
            return text.ToString();
        }
    }
}

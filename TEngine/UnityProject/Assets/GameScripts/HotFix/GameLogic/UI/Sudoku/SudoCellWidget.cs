using System;
using System.Collections.Generic;
using GameLogic.Sudoku;
using TMPro;

namespace GameLogic
{
    public sealed class SudoCellWidget : UIWidget
    {
        private XYButton _button;
        private TMP_Text _value;
        private UnityEngine.Transform _noteGrid;
        private SudokuNoteItemPool _notePool;
        private readonly List<NoteItem> _noteItems = new List<NoteItem>(9);
        private UIThemeColor _backgroundColor, _valueColor;
        private Action _clicked;
        private SudokuTextSize? _appliedTextSize;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>(gameObject);
            _value = FindChildComponent<TMP_Text>("m_tmp_Value");
            _noteGrid = FindChild("m_noteGrid");
            _backgroundColor = gameObject.GetComponent<UIThemeColor>();
            _valueColor = _value.GetComponent<UIThemeColor>();
            _button.OnAddListener(() => _clicked?.Invoke());
        }
        internal void Bind(Action clicked, SudokuNoteItemPool notePool)
        {
            _clicked = clicked;
            _notePool = notePool;
        }
        public void SetCompletionFlash(float weight) => _backgroundColor.SetOverlay(SudokuThemeRole.CompletionFlash, weight);
        public void Refresh(SudokuSession session, int index, int selected, bool canInteract)
        {
            int value = session.Value(index);
            int selectedValue = selected >= 0 ? session.Value(selected) : 0;
            var settings = SudokuSettingsManager.Data;
            ApplyTextSize(settings.textSize);
            bool related = selected >= 0 && (index / 9 == selected / 9 || index % 9 == selected % 9 || SudokuBoard.Box(index) == SudokuBoard.Box(selected));
            _backgroundColor.SetRole(index == selected ? SudokuThemeRole.Selected :
                settings.highlightSameNumber && selectedValue != 0 && value == selectedValue ? SudokuThemeRole.Matching :
                settings.highlightRelated && related ? SudokuThemeRole.RelatedCell : SudokuThemeRole.Surface);
            _valueColor.SetRole(session.HasConflict(index) ? SudokuThemeRole.Error : session.IsGiven(index) ? SudokuThemeRole.Text : SudokuThemeRole.Accent);
            _value.text = value == 0 ? "" : value.ToString();
            RefreshNotes(value == 0 ? session.Notes(index) : 0, settings.highlightSameNote ? selectedValue : 0, settings.textSize);
            _button.Interactable = canInteract;
        }
        private void ApplyTextSize(SudokuTextSize size)
        {
            if (_appliedTextSize == size) return;
            _appliedTextSize = size;
            var style = SudokuTextStyle.ForSize(size);
            _value.enableAutoSizing = false;
            _value.fontSize = style.ValueSize;
        }
        private void RefreshNotes(int mask, int highlightDigit, SudokuTextSize size)
        {
            if (mask == 0)
            {
                ReleaseNotes();
                return;
            }
            // 固定九个槽位，避免现有 GridLayoutGroup 在只有一行/一列时重新居中。
            while (_noteItems.Count < 9)
            {
                var item = _notePool.Rent(_noteGrid);
                item.gameObject.name = "NoteItem" + (_noteItems.Count + 1);
                _noteItems.Add(item);
            }
            for (int i = 0; i < _noteItems.Count; i++)
            {
                int digit = i + 1;
                _noteItems[i].Refresh(digit, (mask & (1 << digit)) != 0, digit == highlightDigit, size);
                if (!_noteItems[i].Visible) _noteItems[i].Visible = true;
            }
        }
        private void ReleaseNotes()
        {
            foreach (var item in _noteItems) _notePool.Return(item);
            _noteItems.Clear();
        }
        protected override void OnClose()
        {
            ReleaseNotes();
            base.OnClose();
        }
        protected override void OnDestroy()
        {
            ReleaseNotes();
            _notePool = null;
            base.OnDestroy();
        }
    }
}

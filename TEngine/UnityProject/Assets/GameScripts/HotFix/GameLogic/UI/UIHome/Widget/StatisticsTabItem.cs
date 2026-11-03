using System;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class StatisticsTabItem : UIWidget
    {
        private TMP_Text _title;
        private SudokuDifficulty _difficulty;
        private Action<SudokuDifficulty> _select;
        private Color _normalColor;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("m_title");
            _normalColor = _title.color;
            CreateWidget<XYButton>(gameObject).OnAddListener(() => _select(_difficulty));
        }

        public void Init(SudokuDifficulty difficulty, Action<SudokuDifficulty> select)
        {
            _difficulty = difficulty;
            _select = select;
        }

        public void RefreshDisplay(SudokuDifficulty selected)
        {
            _title.text = SudokuUIFormatting.Difficulty(_difficulty);
            _title.color = _difficulty == selected ? Color.white : _normalColor;
        }
    }
}

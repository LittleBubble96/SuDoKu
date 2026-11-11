using System;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class NewGameSelectDiffItem : UIWidget
    {
        private XYButton _button;
        private TMP_Text _title, _description;
        private UIThemeColor _background, _titleColor, _descriptionColor;
        private GameObject _lock, _selectRoot, _progressRoot;
        private readonly UIThemeColor[] _progress = new UIThemeColor[SudokuSession.RequiredCompletions];
        private SudokuDifficulty _difficulty;
        private Action<SudokuDifficulty> _select;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>("bg");
            _button.OnAddListener(() => _select?.Invoke(_difficulty));
            _title = FindChildComponent<TMP_Text>("bg/m_title");
            _description = FindChildComponent<TMP_Text>("bg/m_des");
            _background = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("bg"), SudokuThemeRole.Surface);
            _titleColor = UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            _descriptionColor = UIThemeColor.Bind(_description, SudokuThemeRole.MutedText);
            _lock = FindChild("bg/Lock").gameObject;
            _selectRoot = FindChild("bg/SelectRoot").gameObject;
            UIThemeColor.Bind(_lock.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_selectRoot.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Accent);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("bg/SelectRoot/icon"), SudokuThemeRole.Surface);
            _lock.SetActive(false);
            _selectRoot.SetActive(false);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var template = FindChild("bg/Layout/ProgressItem");
            _progressRoot = template.parent.gameObject;
            template.gameObject.SetActive(false);
            for (int i = 0; i < _progress.Length; i++)
            {
                var dot = UnityEngine.Object.Instantiate(template.gameObject, template.parent);
                dot.name = "ProgressItem" + (i + 1);
                _progress[i] = UIThemeColor.Bind(dot.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Line);
                dot.SetActive(true);
            }
        }

        public void Init(SudokuDifficulty difficulty, Action<SudokuDifficulty> select)
        {
            _difficulty = difficulty;
            _select = select;
        }

        public void RefreshDisplay(SudokuSession session, SudokuDifficulty selected, int played)
        {
            bool unlocked = session.IsUnlocked(_difficulty);
            bool isSelected = unlocked && selected == _difficulty;
            int completed = unlocked ? 0 : Math.Min(SudokuSession.RequiredCompletions,
                session.CompletedCount((SudokuDifficulty)((int)_difficulty - 1)));
            _button.Interactable = unlocked;
            _lock.SetActive(!unlocked);
            _progressRoot.SetActive(!unlocked);
            _selectRoot.SetActive(isSelected);
            _title.text = SudokuUIFormatting.Difficulty(_difficulty);
            if (unlocked)
                _description.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.home_new_game_played), played);
            else
            {
                var previous = (SudokuDifficulty)((int)_difficulty - 1);
                int remaining = Math.Max(0, SudokuSession.RequiredCompletions - session.CompletedCount(previous));
                _description.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.home_new_game_diff_des),
                    SudokuUIFormatting.Difficulty(previous), remaining);
            }
            _background.SetRole(isSelected ? SudokuThemeRole.Accent : SudokuThemeRole.Matching);
            _titleColor.SetRole(!unlocked ? SudokuThemeRole.MutedText : isSelected ? SudokuThemeRole.Accent : SudokuThemeRole.Text);
            _descriptionColor.SetRole(isSelected ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
            for (int i = 0; i < _progress.Length; i++) _progress[i].SetRole(i < completed ? SudokuThemeRole.Accent : SudokuThemeRole.Line);
        }
    }
}

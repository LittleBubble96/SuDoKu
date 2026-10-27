using System;
using GameLogic.Data;
using GameLogic.Sudoku;
using TMPro;

namespace GameLogic
{
    public sealed class SudokuNumberWidget : UIWidget
    {
        private XYButton _button;
        private TMP_Text _number;
        private TMP_Text _remaining;
        private UIThemeColor _numberColor;
        private int _digit;
        private Action<int> _clicked;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>(gameObject);
            _number = FindChildComponent<TMP_Text>("Bg/m_num");
            _numberColor = UIThemeColor.Bind(_number, SudokuThemeRole.Accent);
            // 由美术后续在 Num1 下补 m_remaining；没有节点时仅保留数量逻辑。
            _remaining = FindChildComponent<TMP_Text>("m_remaining");
            if (_remaining != null) UIThemeColor.Bind(_remaining, SudokuThemeRole.MutedText);
            _button.OnAddListener(() => _clicked?.Invoke(_digit));
        }
        public void Init(int digit, Action<int> clicked)
        {
            _digit = digit; _clicked = clicked; _number.text = digit.ToString();
        }
        public void SetInteractable(bool interactable) => _button.Interactable = interactable;
        public void RefreshDisplay(bool interactable, int remaining, GameSettingsData settings)
        {
            SetInteractable(interactable);
            _numberColor.SetRole(settings.dimCompletedDigits && remaining == 0 ? SudokuThemeRole.MutedText : SudokuThemeRole.Accent);
            if (_remaining != null)
            {
                _remaining.gameObject.SetActive(settings.showRemainingCount);
                _remaining.text = remaining.ToString();
            }
        }
    }
}

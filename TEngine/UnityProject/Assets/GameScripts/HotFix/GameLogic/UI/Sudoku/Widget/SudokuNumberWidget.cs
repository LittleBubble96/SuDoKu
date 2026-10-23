using System;
using TMPro;

namespace GameLogic
{
    public sealed class SudokuNumberWidget : UIWidget
    {
        private XYButton _button;
        private TMP_Text _number;
        private int _digit;
        private Action<int> _clicked;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>(gameObject);
            _number = FindChildComponent<TMP_Text>("Bg/m_num");
            _button.OnAddListener(() => _clicked?.Invoke(_digit));
        }
        public void Init(int digit, Action<int> clicked)
        {
            _digit = digit; _clicked = clicked; _number.text = digit.ToString();
        }
        public void SetInteractable(bool interactable) => _button.Interactable = interactable;
    }
}

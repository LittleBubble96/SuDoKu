using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;

namespace GameLogic
{
    public sealed class StatisticsItem : UIWidget
    {
        private TMP_Text _title, _value;
        private UnityEngine.UI.Image _icon;
        private string _titleKey;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("m_title");
            _value = FindChildComponent<TMP_Text>("m_num");
            _icon = FindChildComponent<UnityEngine.UI.Image>("icon");
            _icon.preserveAspect = true;
            UIThemeColor.Bind(_icon, SudokuThemeRole.Accent);
        }

        public void Init(string titleKey, string icon)
        {
            _titleKey = titleKey;
            _icon.SetSprite(icon, cancellationToken: gameObject.GetCancellationTokenOnDestroy());
        }

        public void RefreshDisplay(string value)
        {
            _title.text = LocalizationHelper.GetLocalText(_titleKey);
            _value.text = value;
        }
    }
}

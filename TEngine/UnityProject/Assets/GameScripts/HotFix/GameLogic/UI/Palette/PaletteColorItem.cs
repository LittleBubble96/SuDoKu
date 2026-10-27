using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class PaletteColorItem : UIWidget
    {
        private int _themeId;
        private string _key;
        private TMP_Text _name;
        private GameObject _outline, _check;
        private UnityEngine.UI.Image _swatch;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _name = FindChildComponent<TMP_Text>("m_name");
            _swatch = FindChildComponent<UnityEngine.UI.Image>("Color");
            _outline = FindChild("SelectOutline").gameObject;
            _check = FindChild("Check").gameObject;
            UIThemeColor.Bind(_name, SudokuThemeRole.Text);
            var outline = _outline.GetComponent<UnityEngine.UI.Image>();
            var check = _check.GetComponent<UnityEngine.UI.Image>();
            UIThemeColor.Bind(outline, SudokuThemeRole.Accent);
            UIThemeColor.Bind(check, SudokuThemeRole.Surface);
            outline.raycastTarget = false;
            check.raycastTarget = false;
            // ColorItem 是无 Button 的本地模板，复用子图形作为点击目标。
            var button = gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = _swatch;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            CreateWidget<XYButton>(gameObject).OnAddListener(() => SudokuThemeManager.SetTheme(_themeId));
            _name.enableAutoSizing = true;
            _name.fontSizeMax = _name.fontSize;
            _name.fontSizeMin = _name.fontSize * .65f;
        }

        public void Init(int themeId, string key)
        {
            _themeId = themeId;
            _key = key;
            // 色板展示各主题的固定颜色，不绑定当前主题色。
            _swatch.color = SudokuThemeManager.GetAccent(themeId);
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            _name.text = LocalizationHelper.GetLocalText(_key);
            bool selected = _themeId == SudokuThemeManager.CurrentThemeId;
            _outline.SetActive(selected);
            _check.SetActive(selected);
        }
    }
}

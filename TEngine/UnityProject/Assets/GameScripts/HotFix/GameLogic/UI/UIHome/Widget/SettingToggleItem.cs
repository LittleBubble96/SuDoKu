using System;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>绑定已搭建的开关行，不生成或调整外部布局。</summary>
    public sealed class SettingToggleItem : UIWidget
    {
        private TMP_Text _title;
        private RectTransform _handle;
        private UnityEngine.UI.Image _background;
        private UIThemeColor _backgroundColor;
        private Vector2 _onPosition, _offPosition;
        private string _key;
        private Func<bool> _getValue;
        private Action<bool> _setValue;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("m_title");
            _handle = FindChildComponent<RectTransform>("toggleBtn/toggleHandle");
            _background = FindChildComponent<UnityEngine.UI.Image>("toggleBtn/toggleBg");
            _onPosition = _handle.anchoredPosition;
            _offPosition = new Vector2(2f * _background.rectTransform.anchoredPosition.x - _onPosition.x, _onPosition.y);
            _backgroundColor = UIThemeColor.Bind(_background, SudokuThemeRole.Accent);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("icon"), SudokuThemeRole.Accent);
            UIThemeColor.Bind(_handle.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Surface);
            CreateWidget<XYButton>("toggleBtn").OnAddListener(() => _setValue(!_getValue()));
        }

        public void Init(string key, Func<bool> getValue, Action<bool> setValue)
        {
            _key = key;
            _getValue = getValue;
            _setValue = setValue;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            bool enabled = _getValue();
            _title.text = LocalizationHelper.GetLocalText(_key);
            _handle.anchoredPosition = enabled ? _onPosition : _offPosition;
            _backgroundColor.SetRole(enabled ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
            _background.canvasRenderer.SetAlpha(enabled ? 1f : 0.28f);
        }
    }
}


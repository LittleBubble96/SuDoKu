using System.Collections.Generic;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;

namespace GameLogic
{
    [Window(UILayer.Top, "UIPalette")]
    public sealed class UIPalette : UIWindow
    {
        private const string Dialog = "Panel/Dialog/";
        private static readonly string[] ThemeKeys =
        {
            LanguageKey.palette_classic, LanguageKey.palette_mint, LanguageKey.palette_sky,
            LanguageKey.palette_peach, LanguageKey.palette_lavender, LanguageKey.palette_apricot
        };
        private readonly List<PaletteColorItem> _items = new List<PaletteColorItem>(6);
        private TMP_Text _title;
        private UIWindow _owner;
        private int _ownerVersion;
        private bool _resumeGame;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>(Dialog + "m_title");
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + "Bg"), SudokuThemeRole.Surface);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + "CloseBtn/icon"), SudokuThemeRole.MutedText);
            CreateWidget<XYButton>("Panel/Close_Btn").OnAddListener(() => GameModule.UI.HideUI<UIPalette>());
            CreateWidget<XYButton>(Dialog + "CloseBtn").OnAddListener(() => GameModule.UI.HideUI<UIPalette>());
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var template = FindChild(Dialog + "Layout/ColorItem");
            template.gameObject.SetActive(false);
            for (int i = 0; i < SudokuThemeManager.ThemeCount; i++)
            {
                var item = CreateWidgetByPrefab<PaletteColorItem>(template.gameObject, template.parent);
                item.gameObject.name = "ColorItem" + i;
                item.Init(i, ThemeKeys[i]);
                _items.Add(item);
            }
            SudokuThemeManager.Changed += RefreshDisplay;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _owner = (UIWindow)UserData;
            _ownerVersion = _owner.OpenVersion;
            _resumeGame = _owner is UISudoku && SudokuPresenter.Current.IsWindowActive;
            if (_owner is UISudoku) SudokuPresenter.Current.SetWindowActive(false);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.palette_title);
            foreach (var item in _items) item.RefreshDisplay();
        }

        protected override void OnClose()
        {
            if (_owner is UISudoku && _owner.IsOpen && _owner.OpenVersion == _ownerVersion)
            {
                SudokuPresenter.Current.SetWindowActive(_resumeGame);
                GameEvent.Send(EventDefine.SudokuStateChanged);
            }
            _owner = null;
            base.OnClose();
        }

        protected override void OnDestroy()
        {
            SudokuThemeManager.Changed -= RefreshDisplay;
            base.OnDestroy();
        }
    }
}

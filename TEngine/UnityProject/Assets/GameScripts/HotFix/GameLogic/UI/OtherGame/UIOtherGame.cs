using System.Collections.Generic;
using GameLogic.Localization;
using GameLogic.Sudoku;
using RTLTMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Top, "UIOtherGame")]
    public sealed class UIOtherGame : UIWindow
    {
        private const string Dialog = "Panel/Dialog/";
        private static readonly (string TitleKey, string DescriptionKey, string AppId, string IconAddress)[] Games =
        {
            // 寻字大师使用模板中已有的缩略图。新增游戏可指定自己的图集地址。
            (LanguageKey.more_games_character_hunt, LanguageKey.more_games_character_desc, "wx308052a1ea617d67", null)
        };

        private readonly List<OtherGameItem> _items = new List<OtherGameItem>(Games.Length);
        private RTLTextMeshPro _title;
        private UnityEngine.UI.ScrollRect _scroll;
        private UIPopupAnimation _animation;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(),
                FindChildComponent<RectTransform>("Panel/Dialog"));
            _title = FindChildComponent<RTLTextMeshPro>(Dialog + "m_title");
            _scroll = FindChildComponent<UnityEngine.UI.ScrollRect>(Dialog + "ScrollView");
            _scroll.horizontal = false;
            _scroll.vertical = true;

            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + "Bg"), SudokuThemeRole.Surface);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + "CloseBtn/icon"), SudokuThemeRole.MutedText);
            CreateWidget<XYButton>("Panel/Close_Btn").OnAddListener(Dismiss);
            CreateWidget<XYButton>(Dialog + "CloseBtn").OnAddListener(Dismiss);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var content = _scroll.content;
            var template = FindChild(content, "otherGameItem");
            template.gameObject.SetActive(false);
            foreach (var game in Games)
            {
                var item = CreateWidgetByPrefab<OtherGameItem>(template.gameObject, content);
                item.Init(game.TitleKey, game.DescriptionKey, game.AppId, game.IconAddress);
                _items.Add(item);
            }

            var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            RefreshLabels();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
            _scroll.StopMovement();
            _scroll.verticalNormalizedPosition = 1f;
        }

        internal override void OnLanguageChanged() => RefreshLabels();
        protected override void OnInAnimation() => _animation.PlayIn(CompleteInAnimation);
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);

        protected override void OnClose()
        {
            _animation.Reset();
            base.OnClose();
        }

        private void RefreshLabels()
        {
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.home_more_games);
            foreach (var item in _items) item.RefreshLabels();
        }

        private static void Dismiss() => GameModule.UI.HideUI<UIOtherGame>();
    }
}

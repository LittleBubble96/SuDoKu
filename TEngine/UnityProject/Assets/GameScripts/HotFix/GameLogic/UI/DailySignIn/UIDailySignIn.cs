using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Top, "UIDailySignIn")]
    public sealed class UIDailySignIn : UIWindow
    {
        private const string Dialog = "Panel/Dialog/";
        private UIPopupAnimation _popupAnimation;
        public const float HorizontalSpacing = 10f, VerticalSpacing = 0f;
        private readonly List<DailySignInItem> _items = new List<DailySignInItem>(7);
        private DailySignInManager _manager;
        private TMP_Text _title, _description, _directText, _adText;
        private XYButton _direct, _ad;
        private bool _claiming;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _popupAnimation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(),
                FindChildComponent<RectTransform>("Panel/Dialog"));
            _title = FindChildComponent<TMP_Text>(Dialog + "m_title");
            _description = FindChildComponent<TMP_Text>(Dialog + "desBg/m_des");
            _directText = FindChildComponent<TMP_Text>(Dialog + "DirectClaimBtn/m_text");
            _adText = FindChildComponent<TMP_Text>(Dialog + "AdClaimBtn/m_text");
            _direct = CreateWidget<XYButton>(Dialog + "DirectClaimBtn");
            _ad = CreateWidget<XYButton>(Dialog + "AdClaimBtn");
            foreach (var text in new[] { _title, _directText, _adText })
            {
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = text.fontSize * .65f;
            }
            _direct.OnAddListener(ClaimDirect);
            _ad.OnAddListener(() => ClaimAdAsync().Forget());
            CreateWidget<XYButton>("Panel/Close_Btn").OnAddListener(ClaimDirect);
            CreateWidget<XYButton>(Dialog + "CloseBtn").OnAddListener(ClaimDirect);
            // 白色面板内的空白与外部遮罩使用相同的直接领取行为。
            var background = FindChildComponent<UnityEngine.UI.Image>(Dialog + "Bg");
            var blankButton = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            blankButton.targetGraphic = background;
            blankButton.transition = UnityEngine.UI.Selectable.Transition.None;
            CreateWidget<XYButton>(background.gameObject).OnAddListener(ClaimDirect);
            _title.raycastTarget = _description.raycastTarget = false;
            FindChildComponent<UnityEngine.UI.Image>(Dialog + "desBg").raycastTarget = false;
            UIThemeColor.Bind(background, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_description, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_directText, SudokuThemeRole.Accent);
            UIThemeColor.Bind(_adText, SudokuThemeRole.Surface);
            Theme("CloseBtn/icon", SudokuThemeRole.MutedText);
            Theme("desBg", SudokuThemeRole.Related);
            Theme("DirectClaimBtn/bg", SudokuThemeRole.Accent);
            Theme("AdClaimBtn/bg", SudokuThemeRole.Accent);
            Theme("AdClaimBtn/icon", SudokuThemeRole.Surface);
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _manager = SudokuPresenter.DailySignIn;
            var template = FindChildComponent<RectTransform>(Dialog + "Layout/Item");
            Vector2 size = template.sizeDelta, start = template.anchoredPosition;
            template.gameObject.SetActive(false);
            for (int i = 0; i < DailySignInManager.CycleDays; i++)
            {
                var item = CreateWidgetByPrefab<DailySignInItem>(template.gameObject, template.parent);
                item.gameObject.name = "Item" + (i + 1);
                float width = i == 6 ? size.x * 2f + HorizontalSpacing : size.x;
                item.rectTransform.sizeDelta = new Vector2(width, size.y);
                item.rectTransform.anchoredPosition = start + new Vector2(
                    i % 4 * (size.x + HorizontalSpacing) + (width - size.x) * .5f,
                    -(i / 4) * (size.y + VerticalSpacing));
                item.Init(i + 1, ClaimDirect);
                _items.Add(item);
            }
            AdSystem.StateChanged += OnAdStateChanged;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _claiming = false;
            AdSystem.EnterPage(AdIds.DailySignIn);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.daily_sign_in_title);
            _description.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_sign_in_streak), _manager.TotalDays);
            _directText.text = LocalizationHelper.GetLocalText(
                _manager.ClaimedToday ? LanguageKey.daily_sign_in_claimed : LanguageKey.daily_sign_in_claim);
            _adText.text = LocalizationHelper.GetLocalText(LanguageKey.daily_reward_ad_double);
            foreach (var item in _items) item.RefreshDisplay(_manager, _claiming);
            _direct.Interactable = !_claiming && !_manager.ClaimedToday;
            _ad.Interactable = !_claiming && !_manager.ClaimedToday && AdSystem.IsAdAvailable(AdIds.DailySignIn);
        }

        private void OnAdStateChanged(string id)
        {
            if (IsOpen && gameObject != null && id == AdIds.DailySignIn) RefreshDisplay();
        }

        private void ClaimDirect()
        {
            if (_claiming) return;
            _claiming = true;
            GrantReward(false);
            GameModule.UI.HideUI<UIDailySignIn>();
        }

        private async UniTaskVoid ClaimAdAsync()
        {
            if (_claiming || _manager.ClaimedToday || !AdSystem.IsAdAvailable(AdIds.DailySignIn)) return;
            _claiming = true;
            int version = OpenVersion;
            var game = SudokuPresenter.Current;
            game.SetAdShowing(true);
            RefreshDisplay();
            try
            {
                bool rewarded = await AdSystem.ShowRewardedAdAsync(AdIds.DailySignIn);
                if (!IsOpen || OpenVersion != version) return;
                if (rewarded)
                {
                    GrantReward(true);
                    GameModule.UI.HideUI<UIDailySignIn>();
                }
            }
            finally
            {
                game.SetAdShowing(false);
                if (IsOpen && OpenVersion == version)
                {
                    _claiming = false;
                    RefreshDisplay();
                }
            }
        }

        private void GrantReward(bool doubled)
        {
            Vector3 origin = _items[_manager.DisplayDay - 1].RewardPosition;
            if (!_manager.TryClaim(doubled, out var reward)) return;
            RewardEffectPlayer.PlayAsync(reward.IsCoin, reward.Amount, origin).Forget();
        }

        protected override void OnInAnimation() => _popupAnimation.PlayIn(CompleteInAnimation);
        protected override void OnOutAnimation() => _popupAnimation.PlayOut(CompleteOutAnimation);

        protected override void OnClose()
        {
            _popupAnimation.Reset();
            // 框架关闭也算普通领取；管理类的每日标记避免与按钮路径重复发奖。
            if (SudokuPresenter.DailySignIn != null && gameObject != null) GrantReward(false);
            AdSystem.ExitPage(AdIds.DailySignIn);
            base.OnClose();
        }

        protected override void OnDestroy()
        {
            _popupAnimation.Reset();
            AdSystem.StateChanged -= OnAdStateChanged;
            base.OnDestroy();
        }

        private void Theme(string path, SudokuThemeRole role) =>
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + path), role);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Tips, "UIReward")]
    public sealed class UIReward : UIWindow
    {
        private const string Content = "Center/Content/";
        private readonly List<RewardTokenItem> _items = new List<RewardTokenItem>(2);
        private SettlementContext _context;
        private XYButton _claim, _double, _close;
        private TMP_Text _description;
        private RectTransform _gift, _area;
        private UIPopupAnimation _animation;
        private bool _claiming, _revealing, _adFailed;
        public bool IsRevealComplete => !_revealing;
        public IReadOnlyList<RewardTokenItem> Items => _items;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(), FindChildComponent<RectTransform>("Center"));
            _area = FindChildComponent<RectTransform>(Content + "rewardArea");
            _gift = FindChildComponent<RectTransform>(Content + "gift/bg");
            _description = FindChildComponent<TMP_Text>(Content + "ActionBtn/GetDoubleBtn/m_textTip");
            _claim = CreateWidget<XYButton>(Content + "ActionBtn/GetBtn");
            _double = CreateWidget<XYButton>(Content + "ActionBtn/GetDoubleBtn");
            _close = CreateWidget<XYButton>("CloseBtn");
            _claim.OnAddListener(ClaimDirect); _double.OnAddListener(() => ClaimDoubleAsync().Forget());
            _close.OnAddListener(ClaimDirect);
            foreach (var text in gameObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false; text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize; text.fontSizeMin = text.fontSize * .55f;
                UIThemeColor.Bind(text, SudokuThemeRole.Surface);
            }
            UIThemeColor.Bind(FindChildComponent<TMP_Text>(Content + "ActionBtn/GetBtn/m_text"), SudokuThemeRole.Accent);
            Theme(Content + "ActionBtn/GetBtn/bg", SudokuThemeRole.Surface);
            Theme(Content + "ActionBtn/GetBtn/bg_frame", SudokuThemeRole.Accent);
            Theme(Content + "ActionBtn/GetDoubleBtn/bg", SudokuThemeRole.Accent);
            Theme(Content + "ActionBtn/GetDoubleBtn/bg_frame", SudokuThemeRole.Selected);
            Theme(Content + "ActionBtn/GetDoubleBtn/Icon", SudokuThemeRole.Surface);
            Theme("CloseBtn/Icon", SudokuThemeRole.Surface);
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            var template = FindChild(Content + "rewardArea/token");
            template.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++)
            {
                var item = CreateWidgetByPrefab<RewardTokenItem>(template.gameObject, template.parent);
                item.gameObject.name = "RewardToken" + i; _items.Add(item);
            }
            AdSystem.StateChanged += OnAdStateChanged;
        }
        protected override void RegisterEvent()
        {
            base.RegisterEvent(); AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }
        protected override void OnRefresh()
        {
            base.OnRefresh(); _context = UserData as SettlementContext;
            _claiming = _adFailed = false; _revealing = true;
            _items[0].Init(true, _context?.Result.coins ?? 0); _items[1].Init(false, _context?.Result.tips ?? 0);
            _items[0].Visible = (_context?.Result.coins ?? 0) > 0;
            _items[1].Visible = (_context?.Result.tips ?? 0) > 0;
            AdSystem.EnterPage(AdIds.Settlement); RefreshDisplay();
        }
        private void RefreshDisplay()
        {
            if (!IsOpen || _context == null) return;
            Label("Center/Top/m_text", LanguageKey.reward_title);
            Label(Content + "ActionBtn/GetBtn/m_text", LanguageKey.daily_gift_claim);
            Label(Content + "ActionBtn/GetDoubleBtn/m_text", LanguageKey.reward_double);
            foreach (var item in _items) item.RefreshLanguage();
            var state = AdSystem.GetState(AdIds.Settlement);
            _description.text = LocalizationHelper.GetLocalText(_adFailed ? LanguageKey.reward_ad_failed :
                state == RewardedAdState.Loading ? LanguageKey.reward_ad_loading :
                state == RewardedAdState.Failed ? LanguageKey.reward_ad_retry : LanguageKey.reward_double_desc);
            bool canClaim = !_claiming && !_context.Result.IsClaimed;
            _close.Interactable = !_claiming;
            _claim.Interactable = canClaim && !_revealing;
            _double.Interactable = canClaim && !_revealing && state != RewardedAdState.Loading && state != RewardedAdState.Showing;
        }
        protected override void OnInAnimation() => _animation.PlayIn(() =>
        {
            CompleteInAnimation();
            if (IsOpen) RevealAsync(CloseToken, OpenVersion).Forget();
        });
        private async UniTaskVoid RevealAsync(CancellationToken token, int version)
        {
            try
            {
                await UniTask.WhenAll(_items[0].LoadIconAsync(token), _items[1].LoadIconAsync(token));
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_area);
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                foreach (var item in _items)
                {
                    if (!item.Visible) continue;
                    Vector3 target = item.MotionRoot.position;
                    Vector3 start = _gift.TransformPoint(new Vector3(0, _gift.rect.height * .05f, 0));
                    const float duration = .48f;
                    float elapsed = 0;
                    while (elapsed < duration)
                    {
                        token.ThrowIfCancellationRequested();
                        elapsed += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(elapsed / duration), ease = 1 - Mathf.Pow(1 - t, 3);
                        item.MotionRoot.position = Vector3.Lerp(start, target, ease) +
                            _area.TransformVector(Vector3.up * (Mathf.Sin(t * Mathf.PI) * 90));
                        item.MotionRoot.localScale = Vector3.one * Mathf.Lerp(.18f, 1f, ease);
                        item.RevealAlpha = Mathf.Clamp01(t * 4);
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                    item.FinishReveal();
                    float settle = 0;
                    while (settle < .16f)
                    {
                        settle += Time.unscaledDeltaTime;
                        item.MotionRoot.localScale = Vector3.one * (1 + .1f * Mathf.Sin(Mathf.Clamp01(settle / .16f) * Mathf.PI));
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }
                    item.MotionRoot.localScale = Vector3.one;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (IsOpen && OpenVersion == version)
                {
                    foreach (var item in _items) item.FinishReveal();
                    _revealing = false; RefreshDisplay();
                }
            }
        }
        private void ClaimDirect()
        {
            if (_claiming || _context == null) return;
            _claiming = true; Grant(false); GameModule.UI.HideUI<UIReward>();
        }
        private async UniTaskVoid ClaimDoubleAsync()
        {
            if (_claiming || _revealing || _context == null || _context.Result.IsClaimed) return;
            if (!AdSystem.IsAdAvailable(AdIds.Settlement))
            {
                _adFailed = false; AdSystem.Preload(AdIds.Settlement); RefreshDisplay(); return;
            }
            _claiming = true; _adFailed = false;
            int version = OpenVersion; var context = _context;
            context.Game.SetAdShowing(true); RefreshDisplay();
            try
            {
                bool rewarded = await AdSystem.ShowRewardedAdAsync(AdIds.Settlement);
                if (!IsOpen || OpenVersion != version || !ReferenceEquals(context.Result, context.Game.Settlement)) return;
                if (rewarded) { Grant(true); GameModule.UI.HideUI<UIReward>(); }
                else _adFailed = true;
            }
            catch (Exception error) { Debug.LogException(error); _adFailed = true; }
            finally
            {
                context.Game.SetAdShowing(false);
                if (IsOpen && OpenVersion == version) { _claiming = false; RefreshDisplay(); }
            }
        }
        private void Grant(bool doubled)
        {
            if (_context?.Game.TryClaimSettlement(_context.Result, doubled) == true) SudokuPresenter.Coins?.PlayGainAnimation();
        }
        private void OnAdStateChanged(string id) { if (id == AdIds.Settlement) RefreshDisplay(); }
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);
        protected override void OnClose()
        {
            _animation.Reset(); Grant(false); AdSystem.ExitPage(AdIds.Settlement);
            foreach (var item in _items) item.ResetMotion();
            base.OnClose();
        }
        protected override void OnDestroy()
        {
            _animation.Reset(); AdSystem.StateChanged -= OnAdStateChanged; base.OnDestroy();
        }
        private void Label(string path, string key) => FindChildComponent<TMP_Text>(path).text = LocalizationHelper.GetLocalText(key);
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

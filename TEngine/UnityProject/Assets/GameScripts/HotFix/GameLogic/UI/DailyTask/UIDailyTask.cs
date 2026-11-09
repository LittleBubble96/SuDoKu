using System;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Top, "UIDailyTask")]
    public sealed class UIDailyTask : UIWindow
    {
        private const string Dialog = "Panel/Dialog/";
        private const string Free = Dialog + "FreeAndAdItem/";
        private const string Tasks = Dialog + "DailyTaskItem/";
        private UIPopupAnimation _animation;
        private DailyTaskManager _manager;
        private DailyTaskRewardItem _freeCoin, _freeTip;
        private DailyTaskItem _playOnce, _challenge;
        private XYButton _freeButton, _adButton, _close, _mask;
        private TMP_Text _freeText, _adText;
        private UnityEngine.UI.Image _adIcon;
        private bool _busy;
        private string _displayDate;

        private TMP_Text _title, _freeTitle, _tasksTitle;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>(Dialog + "m_title");
            _freeTitle = FindChildComponent<TMP_Text>(Free + "m_title");
            _tasksTitle = FindChildComponent<TMP_Text>(Tasks + "m_title");
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(),
                FindChildComponent<RectTransform>("Panel/Dialog"));
            _freeButton = CreateWidget<XYButton>(Free + "FreeBtn");
            _adButton = CreateWidget<XYButton>(Free + "AdClaimBtn");
            _close = CreateWidget<XYButton>(Dialog + "CloseBtn");
            _mask = CreateWidget<XYButton>("Panel/Close_Btn");
            _freeButton.OnAddListener(ClaimFree);
            _adButton.OnAddListener(() => ClaimAdAsync().Forget());
            _close.OnAddListener(ClosePopup);
            _mask.OnAddListener(ClosePopup);
            _freeText = FindChildComponent<TMP_Text>(Free + "FreeBtn/m_text");
            _adText = FindChildComponent<TMP_Text>(Free + "AdClaimBtn/m_text");
            _playOnce = CreateWidget<DailyTaskItem>(Tasks + "PlayOnceItem");
            _challenge = CreateWidget<DailyTaskItem>(Tasks + "CompleteChallengeItem");
            Theme(Dialog + "Bg", SudokuThemeRole.Surface);
            Theme(Dialog + "CloseBtn/icon", SudokuThemeRole.MutedText);
            Theme(Free + "bg", SudokuThemeRole.Related);
            Theme(Free + "FreeBtn/bg", SudokuThemeRole.Surface);
            Theme(Free + "FreeBtn/bg/outline", SudokuThemeRole.Accent);
            Theme(Free + "AdClaimBtn/bg", SudokuThemeRole.Accent);
            _adIcon = FindChildComponent<UnityEngine.UI.Image>(Free + "AdClaimBtn/icon");
            UIThemeColor.Bind(_adIcon, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_freeText, SudokuThemeRole.Accent);
            UIThemeColor.Bind(_adText, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_freeTitle, SudokuThemeRole.Text);
            UIThemeColor.Bind(_tasksTitle, SudokuThemeRole.Text);
            foreach (var text in new[] { _freeText, _adText })
            {
                text.fontSizeMax = text.fontSize; text.fontSizeMin = text.fontSize * .6f;
                text.enableAutoSizing = true;
            }
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
            _manager = SudokuPresenter.Current.DailyTasks;
            var template = FindChild(Free + "RewardLayout/RewardItem");
            template.gameObject.SetActive(false);
            _freeCoin = CreateWidgetByPrefab<DailyTaskRewardItem>(template.gameObject, template.parent);
            _freeTip = CreateWidgetByPrefab<DailyTaskRewardItem>(template.gameObject, template.parent);
            _freeCoin.gameObject.name = "CoinReward";
            _freeTip.gameObject.name = "TipReward";
            _freeCoin.Init(true, DailyTaskManager.FreeCoins);
            _freeTip.Init(false, DailyTaskManager.FreeTips);
            _playOnce.Init(DailyTaskType.PlayOnce, ClaimTask, Go);
            _challenge.Init(DailyTaskType.DailyChallenge, ClaimTask, Go);
            AdSystem.StateChanged += OnAdStateChanged;
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _busy = false;
            AdSystem.EnterPage(AdIds.DailyTask);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            _displayDate = _manager.TodayKey;
            bool claimed = _manager.Data.freeClaimed;
            bool busy = _busy || SudokuPresenter.Current.IsAdShowing;
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.daily_gift_title);
            _freeTitle.text = LocalizationHelper.GetLocalText(LanguageKey.daily_gift_free_reward);
            _tasksTitle.text = LocalizationHelper.GetLocalText(LanguageKey.home_daily_tasks);
            _freeText.text = LocalizationHelper.GetLocalText(claimed ? LanguageKey.daily_sign_in_claimed : LanguageKey.daily_gift_free_claim);
            _adText.text = LocalizationHelper.GetLocalText(claimed ? LanguageKey.daily_sign_in_claimed : LanguageKey.daily_reward_ad_double);
            _freeButton.Interactable = !claimed && !busy;
            _adButton.Interactable = !claimed && !busy && AdSystem.IsAdAvailable(AdIds.DailyTask);
            _adIcon.gameObject.SetActive(!claimed);
            _close.Interactable = _mask.Interactable = !busy;
            _playOnce.RefreshDisplay(_manager, busy);
            _challenge.RefreshDisplay(_manager, busy);
        }

        private void ClaimFree()
        {
            if (_busy || !_manager.TryClaimFree(false, out var reward)) return;
            PlayReward(reward, _freeCoin.RewardPosition, _freeTip.RewardPosition);
            RefreshDisplay();
        }

        private async UniTaskVoid ClaimAdAsync()
        {
            if (_busy || _manager.Data.freeClaimed || !AdSystem.IsAdAvailable(AdIds.DailyTask)) return;
            _busy = true;
            string date = _manager.TodayKey;
            int version = OpenVersion;
            var game = SudokuPresenter.Current;
            game.SetAdShowing(true);
            RefreshDisplay();
            try
            {
                bool rewarded = await AdSystem.ShowRewardedAdAsync(AdIds.DailyTask);
                if (!IsOpen || OpenVersion != version) return;
                if (rewarded && _manager.TryClaimFree(true, out var reward, date))
                    PlayReward(reward, _freeCoin.RewardPosition, _freeTip.RewardPosition);
            }
            finally
            {
                game.SetAdShowing(false);
                if (IsOpen && OpenVersion == version) { _busy = false; RefreshDisplay(); }
            }
        }

        private void ClaimTask(DailyTaskType type)
        {
            if (_busy || !_manager.TryClaimTask(type, out var reward)) return;
            var item = type == DailyTaskType.PlayOnce ? _playOnce : _challenge;
            PlayReward(reward, item.RewardPosition, item.RewardPosition);
            RefreshDisplay();
        }

        private static void PlayReward(DailyTaskReward reward, Vector3 coinOrigin, Vector3 tipOrigin)
        {
            if (reward.Coins > 0) RewardEffectPlayer.PlayCoinAsync(reward.Coins, coinOrigin).Forget();
            if (reward.Tips > 0) RewardEffectPlayer.PlayPropAsync(reward.Tips, tipOrigin).Forget();
        }

        private void Go(DailyTaskType type) => GoAsync(type).Forget();
        private async UniTaskVoid GoAsync(DailyTaskType type)
        {
            if (_busy || SudokuPresenter.IsNavigating) return;
            _busy = true; RefreshDisplay();
            var token = gameObject.GetCancellationTokenOnDestroy();
            int version = OpenVersion;
            GameModule.UI.HideUI<UIDailyTask>();
            try
            {
                await UniTask.WaitUntil(() => !IsOpen || OpenVersion != version, cancellationToken: token);
                if (IsOpen) return;
                var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                if (home == null)
                {
                    await SudokuPresenter.ShowHomeAsync();
                    home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                }
                if (home == null) return;
                if (type == DailyTaskType.DailyChallenge) home.SelectDailyDate(SudokuPresenter.Current.Today);
                else home.SelectTab(0);
                await home.WaitForContentAsync(token);
            }
            catch (OperationCanceledException) { }
            finally { if (IsOpen && OpenVersion == version) { _busy = false; RefreshDisplay(); } }
        }

        private void ClosePopup() { if (!_busy) GameModule.UI.HideUI<UIDailyTask>(); }
        private void OnAdStateChanged(string id) { if (IsOpen && id == AdIds.DailyTask) RefreshDisplay(); }
        protected override void OnUpdate() { if (IsOpen && _displayDate != _manager.TodayKey) RefreshDisplay(); }
        protected override void OnInAnimation() => _animation.PlayIn(CompleteInAnimation);
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);
        protected override void OnClose()
        {
            _animation.Reset();
            AdSystem.ExitPage(AdIds.DailyTask);
            SudokuPresenter.Current?.SaveGameProgress();
            base.OnClose();
        }
        protected override void OnDestroy()
        {
            _animation.Reset();
            AdSystem.StateChanged -= OnAdStateChanged;
            base.OnDestroy();
        }
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

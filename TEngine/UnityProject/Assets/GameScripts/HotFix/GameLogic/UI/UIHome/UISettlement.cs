using System;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class SettlementContext
    {
        public readonly SudokuGameController Game;
        public readonly SudokuSettlementData Result;
        public SettlementContext(SudokuGameController game, SudokuSettlementData result) { Game = game; Result = result; }
    }

    [Window(UILayer.Top, "UISettlement")]
    public sealed class UISettlement : UIWindow
    {
        private const string Content = "Center/Content/";
        private SettlementContext _context;
        private XYButton _gift, _next, _home, _share;
        private UIPopupAnimation _animation;
        private bool _openingReward;
        public SudokuSettlementData Result => _context?.Result;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(), FindChildComponent<RectTransform>("Center"));
            _gift = CreateWidget<XYButton>(Content + "GiftBtn");
            _next = CreateWidget<XYButton>(Content + "AgainGameBtn");
            _home = CreateWidget<XYButton>(Content + "ActionBtn/ReturnBtn");
            _share = CreateWidget<XYButton>(Content + "ActionBtn/FightBtn");
            _gift.OnAddListener(() => OpenRewardAsync().Forget());
            _next.OnAddListener(() => ContinueAsync().Forget());
            _home.OnAddListener(() => SudokuPresenter.ShowHomeAsync(true).Forget());
            _share.OnAddListener(Share);
            foreach (var text in gameObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false; text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize; text.fontSizeMin = text.fontSize * .55f;
                UIThemeColor.Bind(text, SudokuThemeRole.Surface);
            }
            Theme("Center/Top/icon", SudokuThemeRole.Accent);
            Theme("Center/Top/icon_outline", SudokuThemeRole.StrongLine);
            Theme("Center/Top/icon_outline2", SudokuThemeRole.Surface);
            foreach (string field in new[] { "scoreRound", "timeRound", "historyHighest", "fastestTime" })
            {
                Theme(Content + "DataArea/" + field + "/Icon", SudokuThemeRole.Selected);
                UIThemeColor.Bind(FindChildComponent<TMP_Text>(Content + "DataArea/" + field + "/m_text"), SudokuThemeRole.Selected);
            }
            Theme(Content + "AgainGameBtn/bg", SudokuThemeRole.Accent);
            Theme(Content + "AgainGameBtn/bg_outline", SudokuThemeRole.Selected);
            foreach (string button in new[] { "ReturnBtn", "FightBtn" })
            {
                Theme(Content + "ActionBtn/" + button + "/Icon", SudokuThemeRole.Selected);
                UIThemeColor.Bind(FindChildComponent<TMP_Text>(Content + "ActionBtn/" + button + "/m_text"), SudokuThemeRole.Selected);
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent(); AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }
        protected override void OnRefresh()
        {
            base.OnRefresh(); _context = UserData as SettlementContext;
            RefreshDisplay();
        }
        public void RefreshDisplay()
        {
            if (!IsOpen || Result == null) return;
            Label("Center/Top/m_text", LanguageKey.finish_title);
            string[] fields = { "scoreRound", "timeRound", "historyHighest", "fastestTime" };
            string[] keys = { LanguageKey.finish_score, LanguageKey.finish_time, LanguageKey.finish_best_score, LanguageKey.statistics_best_time };
            string[] values = { Result.score.ToString(), SudokuUIFormatting.Time(Result.seconds), Result.bestScore.ToString(), SudokuUIFormatting.Time(Result.bestSeconds) };
            for (int i = 0; i < fields.Length; i++)
            {
                Label(Content + "DataArea/" + fields[i] + "/m_text", keys[i]);
                FindChildComponent<TMP_Text>(Content + "DataArea/" + fields[i] + "/m_textNum").text = values[i];
            }
            Label(Content + "AgainGameBtn/m_text", LanguageKey.continue_btn);
            Label(Content + "ActionBtn/ReturnBtn/m_text", LanguageKey.back_home_btn);
            Label(Content + "ActionBtn/FightBtn/m_text", LanguageKey.finish_share);
            _gift.Interactable = !Result.IsClaimed && !_openingReward;
            FindChild(Content + "GiftBtn/bg_glow").gameObject.SetActive(!Result.IsClaimed);
        }
        private async UniTaskVoid OpenRewardAsync()
        {
            if (_openingReward || Result == null || Result.IsClaimed) return;
            _openingReward = true; int version = OpenVersion;
            RefreshDisplay();
            using (UIInteractionLock.Acquire())
            {
                try
                {
                    await GameModule.UI.ShowUIAsyncAwait<UIReward>(_context);
                    if (!IsOpen || OpenVersion != version) GameModule.UI.CloseUI<UIReward>();
                }
                catch (OperationCanceledException) { }
                catch (Exception error) { Debug.LogException(error); }
                finally { _openingReward = false; RefreshDisplay(); }
            }
        }
        private async UniTaskVoid ContinueAsync()
        {
            if (Result == null) return;
            if (Result.daily) await SudokuPresenter.ShowHomeAsync();
            else await SudokuPresenter.StartNewGameAsync((SudokuDifficulty)Result.difficulty);
        }
        private void Share()
        {
            if (Result == null) return;
            GameSDK.SDK.ShareAppMessage(string.Format(LocalizationHelper.GetLocalText(LanguageKey.finish_share_message),
                SudokuUIFormatting.Difficulty((SudokuDifficulty)Result.difficulty), Result.score, SudokuUIFormatting.Time(Result.seconds)));
        }
        protected override void OnInAnimation() => _animation.PlayIn(CompleteInAnimation);
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);
        protected override void OnClose()
        {
            _animation.Reset();
            if (_context?.Game.TryClaimSettlement(Result, false) == true) SudokuPresenter.Coins?.RefreshView();
            base.OnClose();
        }
        protected override void OnDestroy() { _animation.Reset(); base.OnDestroy(); }
        private void Label(string path, string key) => FindChildComponent<TMP_Text>(path).text = LocalizationHelper.GetLocalText(key);
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

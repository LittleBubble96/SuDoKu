using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        private const string GiftArea = Content + "GiftBtnArea/";
        private const float ProgressMaxWidth = 617f, LineLeftOffset = 10f;
        private const float ProgressGrowDuration = .6f;
        private static readonly (string Node, string Key)[] Stats =
        {
            ("scoreRound", LanguageKey.finish_score),
            ("timeRound", LanguageKey.finish_time),
            ("historyHighest", LanguageKey.finish_best_score),
            ("fastestTime", LanguageKey.statistics_best_time)
        };
        private readonly TMP_Text[] _statLabels = new TMP_Text[Stats.Length];
        private readonly TMP_Text[] _statValues = new TMP_Text[Stats.Length];
        private GameObject _giftGlow;
        private RectTransform _progressFill, _lineTemplate;
        private Animation _giftAnimation;
        private string _giftAnimationName;
        private Tween _progressTween;
        private string _progressResultId;
        private int _progressTarget;
        private bool _progressReady;
        private SettlementContext _context;
        private XYButton _gift, _next, _home, _share;
        private UIPopupAnimation _animation;
        private bool _openingReward;
        public SudokuSettlementData Result => _context?.Result;

        private TMP_Text _title, _nextLabel, _homeLabel, _shareLabel;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("Center/Top/m_text");
            _nextLabel = FindChildComponent<TMP_Text>(Content + "AgainGameBtn/m_text");
            _homeLabel = FindChildComponent<TMP_Text>(Content + "ActionBtn/ReturnBtn/m_text");
            _shareLabel = FindChildComponent<TMP_Text>(Content + "ActionBtn/FightBtn/m_text");
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(), FindChildComponent<RectTransform>("Center"));
            _gift = CreateWidget<XYButton>(GiftArea + "GiftBtn");
            _giftGlow = FindChild(GiftArea + "GiftBtn/bg_glow").gameObject;
            _giftAnimation = FindChildComponent<Animation>(GiftArea + "GiftBtn/bg");
            _giftAnimation.playAutomatically = false;
            _progressFill = FindChildComponent<RectTransform>(GiftArea + "progressBg/progressFill");
            _lineTemplate = FindChildComponent<RectTransform>(GiftArea + "progressBg/line");
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
            for (int i = 0; i < Stats.Length; i++)
            {
                string path = Content + "DataArea/" + Stats[i].Node;
                _statLabels[i] = FindChildComponent<TMP_Text>(path + "/m_text");
                _statValues[i] = FindChildComponent<TMP_Text>(path + "/m_textNum");
                Theme(path + "/Icon", SudokuThemeRole.Selected);
                UIThemeColor.Bind(_statLabels[i], SudokuThemeRole.Selected);
            }
            Theme(Content + "AgainGameBtn/bg", SudokuThemeRole.Accent);
            Theme(Content + "AgainGameBtn/bg_outline", SudokuThemeRole.Selected);
            Theme(Content + "ActionBtn/ReturnBtn/Icon", SudokuThemeRole.Selected);
            Theme(Content + "ActionBtn/FightBtn/Icon", SudokuThemeRole.Selected);
            UIThemeColor.Bind(_homeLabel, SudokuThemeRole.Selected);
            UIThemeColor.Bind(_shareLabel, SudokuThemeRole.Selected);
            Theme(GiftArea + "progressBg", SudokuThemeRole.Related);
            Theme(GiftArea + "progressBg/progressFill", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_lineTemplate.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Related);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _lineTemplate.gameObject.SetActive(false);
            Vector2 size = _lineTemplate.sizeDelta;
            float y = _lineTemplate.anchoredPosition.y;
            // 六个进度段，只在内部生成五条分隔线。
            for (int i = 1; i < SudokuGameController.SettlementRequiredWins; i++)
            {
                var line = UnityEngine.Object.Instantiate(_lineTemplate, _lineTemplate.parent);
                line.name = "Line" + i;
                line.anchorMin = line.anchorMax = new Vector2(0, .5f);
                line.sizeDelta = size;
                line.anchoredPosition = new Vector2(LineLeftOffset + ProgressMaxWidth * i /
                    SudokuGameController.SettlementRequiredWins, y);
                line.gameObject.SetActive(true);
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
            _progressTween?.Kill(false); _progressTween = null;
            _progressReady = false;
            if (Result != null)
            {
                _progressTarget = _context.Game.SettlementProgress;
                int start = _progressResultId == Result.id ? _progressTarget : _context.Game.SettlementProgressBefore;
                _progressResultId = Result.id;
                Vector2 size = _progressFill.sizeDelta;
                size.x = ProgressWidth(start);
                _progressFill.sizeDelta = size;
            }
            RefreshDisplay();
        }
        public void RefreshDisplay()
        {
            if (!IsOpen || Result == null) return;
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.finish_title);
            for (int i = 0; i < Stats.Length; i++)
                _statLabels[i].text = LocalizationHelper.GetLocalText(Stats[i].Key);
            _statValues[0].text = Result.score.ToString();
            _statValues[1].text = SudokuUIFormatting.Time(Result.seconds);
            _statValues[2].text = Result.bestScore.ToString();
            _statValues[3].text = SudokuUIFormatting.Time(Result.bestSeconds);
            _nextLabel.text = LocalizationHelper.GetLocalText(LanguageKey.continue_btn);
            _homeLabel.text = LocalizationHelper.GetLocalText(LanguageKey.back_home_btn);
            _shareLabel.text = LocalizationHelper.GetLocalText(LanguageKey.finish_share);
            if (_progressReady && _progressTarget != _context.Game.SettlementProgress)
                PlayProgressAnimation();
            RefreshGiftState();
        }

        private static float ProgressWidth(int progress) => 10f + ProgressMaxWidth *
            Mathf.Clamp01((float)progress / SudokuGameController.SettlementRequiredWins);

        private void PlayProgressAnimation()
        {
            _progressTarget = _context.Game.SettlementProgress;
            _progressTween?.Kill(false); _progressTween = null;
            Vector2 size = _progressFill.sizeDelta;
            float target = ProgressWidth(_progressTarget);
            if (target > size.x)
            {
                size.x = target;
                _progressTween = _progressFill.DOSizeDelta(size, ProgressGrowDuration)
                    .SetEase(DG.Tweening.Ease.OutCubic).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() => { _progressTween = null; RefreshGiftState(); });
            }
            else
            {
                // 领奖后直接归零；窗口重开、已满时不补播增长。
                size.x = target;
                _progressFill.sizeDelta = size;
            }
            RefreshGiftState();
        }

        private void RefreshGiftState()
        {
            bool ready = _progressReady && _progressTween == null && _context.Game.CanClaimSettlementReward;
            _gift.Interactable = ready && !_openingReward;
            _giftGlow.SetActive(ready);
            string animationName = ready ? "box_jump" : "box_idle";
            if (_giftAnimationName != animationName)
            {
                _giftAnimationName = animationName;
                _giftAnimation.Play(animationName);
            }
        }
        private async UniTaskVoid OpenRewardAsync()
        {
            if (_openingReward || Result == null || !_context.Game.CanClaimSettlementReward) return;
            _openingReward = true; int version = OpenVersion;
            RefreshDisplay();
            using (UIInteractionLock.Acquire())
            {
                try
                {
                    await GameModule.UI.ShowUIAsyncAwait<UIReward>(
                        new SettlementContext(_context.Game, _context.Game.SettlementReward));
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
            else await SudokuPresenter.StartNewGameAsync((SudokuDifficulty)Result.difficulty, "next_round");
        }
        private void Share()
        {
            if (Result == null) return;
            GameSDK.SDK.ShareAppMessage(string.Format(LocalizationHelper.GetLocalText(LanguageKey.finish_share_message),
                SudokuUIFormatting.Difficulty((SudokuDifficulty)Result.difficulty), Result.score, SudokuUIFormatting.Time(Result.seconds)));
        }
        protected override void OnInAnimation() => _animation.PlayIn(() =>
        {
            CompleteInAnimation();
            _progressReady = true;
            if (IsOpen && Result != null) PlayProgressAnimation();
        });
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);
        protected override void OnClose()
        {
            _animation.Reset();
            _progressTween?.Kill(false); _progressTween = null;
            _progressReady = false;
            _giftAnimation.Stop();
            _giftAnimationName = null;
            base.OnClose();
        }
        protected override void OnDestroy()
        {
            _progressTween?.Kill(false); _progressTween = null;
            _animation.Reset(); base.OnDestroy();
        }
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

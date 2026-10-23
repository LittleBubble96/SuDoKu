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
    [Window(UILayer.Top, "UIHomeNewGame")]
    public sealed class UIHomeNewGame : UIWindow
    {
        private const string Sheet = "Panel/Bottom/bg/";
        private const float InDuration = 0.24f, OutDuration = 0.18f;
        private readonly List<NewGameSelectDiffItem> _items = new List<NewGameSelectDiffItem>(6);
        private SudokuGameController _game;
        private RectTransform _bottom;
        private UnityEngine.UI.Image _mask;
        private float _maskAlpha;
        private Vector2 _shownPosition;
        private TMP_Text _title, _description, _startText;
        private XYButton _start;
        private CancellationTokenSource _animation;
        public SudokuDifficulty SelectedDifficulty { get; private set; }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _bottom = FindChildComponent<RectTransform>("Panel/Bottom");
            _shownPosition = _bottom.anchoredPosition;
            _mask = gameObject.GetComponent<UnityEngine.UI.Image>();
            _maskAlpha = _mask.color.a;
            _title = FindChildComponent<TMP_Text>(Sheet + "m_title");
            _description = FindChildComponent<TMP_Text>(Sheet + "m_des");
            _startText = FindChildComponent<TMP_Text>(Sheet + "NewGameBtn/m_text");
            _start = CreateWidget<XYButton>(Sheet + "NewGameBtn");
            _start.OnAddListener(() => StartGameAsync().Forget());
            CreateWidget<XYButton>("Panel/Close_Btn").OnAddListener(() => GameModule.UI.HideUI<UIHomeNewGame>());
            Theme("Panel/Bottom/bg", SudokuThemeRole.Surface);
            Theme(Sheet + "topLine", SudokuThemeRole.Line);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_description, SudokuThemeRole.MutedText);
            Theme(Sheet + "CloseIcon", SudokuThemeRole.Text);
            Theme(Sheet + "NewGameBtn/bg", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_startText, SudokuThemeRole.Surface);
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
            var template = FindChild(Sheet + "Layout/NewGameSelectDiffItem");
            template.gameObject.SetActive(false);
            for (int i = 0; i < 6; i++)
            {
                var item = CreateWidgetByPrefab<NewGameSelectDiffItem>(template.gameObject, template.parent);
                item.gameObject.name = "NewGameSelectDiffItem" + i;
                item.Init((SudokuDifficulty)i, SelectDifficulty);
                _items.Add(item);
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _game = (UserData as SudokuContext)?.Game ?? SudokuPresenter.Current;
            SelectedDifficulty = _game.Session.Difficulty;
            RefreshDisplay();
        }

        public void SelectDifficulty(SudokuDifficulty difficulty)
        {
            if (!_game.Session.IsUnlocked(difficulty) || SudokuPresenter.IsNavigating) return;
            SelectedDifficulty = difficulty;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_game == null) return;
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.home_new_game_title);
            _startText.text = LocalizationHelper.GetLocalText(LanguageKey.home_new_game_start_game);
            _description.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_text_045);
            for (int i = 0; i < 6; i++)
                if (!_game.Session.IsUnlocked((SudokuDifficulty)i))
                {
                    _description.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.home_new_game_des), SudokuUIFormatting.Difficulty((SudokuDifficulty)i));
                    break;
                }
            _start.Interactable = _game.Session.IsUnlocked(SelectedDifficulty) && !SudokuPresenter.IsNavigating;
            foreach (var item in _items) item.RefreshDisplay(_game.Session, SelectedDifficulty);
        }

        private async UniTaskVoid StartGameAsync()
        {
            _start.Interactable = false;
            try { await SudokuPresenter.StartNewGameAsync(SelectedDifficulty); }
            finally { if (IsOpen) RefreshDisplay(); }
        }

        protected override void OnInAnimation() => AnimateAsync(true).Forget();
        protected override void OnOutAnimation() => AnimateAsync(false).Forget();

        private async UniTaskVoid AnimateAsync(bool entering)
        {
            CancelAnimation();
            var source = CancellationTokenSource.CreateLinkedTokenSource(CloseToken);
            _animation = source;
            var token = source.Token;
            var hidden = _shownPosition - Vector2.up * (rectTransform.rect.height + _bottom.rect.height + 48f);
            Vector2 from = entering ? hidden : _bottom.anchoredPosition;
            Vector2 to = entering ? _shownPosition : hidden;
            float fromAlpha = entering ? 0 : _mask.color.a;
            float toAlpha = entering ? _maskAlpha : 0;
            float duration = entering ? InDuration : OutDuration;
            _bottom.anchoredPosition = from;
            SetMaskAlpha(fromAlpha);
            try
            {
                float elapsed = 0;
                while (elapsed < duration)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    elapsed += UnityEngine.Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float slide = entering ? 1 - Mathf.Pow(1 - t, 3) : t * t;
                    _bottom.anchoredPosition = Vector2.LerpUnclamped(from, to, slide);
                    SetMaskAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
                }
                if (_animation != source || !IsOpen) return;
                _bottom.anchoredPosition = to;
                SetMaskAlpha(toAlpha);
                if (entering) CompleteInAnimation();
                else CompleteOutAnimation();
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_animation == source) { _animation = null; source.Dispose(); }
            }
        }

        private void SetMaskAlpha(float alpha) { var color = _mask.color; color.a = alpha; _mask.color = color; }
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Graphic>(path), role);
        private void CancelAnimation() { _animation?.Cancel(); _animation?.Dispose(); _animation = null; }
        protected override void OnClose() { CancelAnimation(); base.OnClose(); }
        protected override void OnDestroy() { CancelAnimation(); base.OnDestroy(); }
    }
}

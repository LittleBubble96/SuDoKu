using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using GameLogic.Localization;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    [Window(UILayer.UI, "UISudoku", fullScreen: true)]
    public sealed class UISudoku : UIWindow
    {
        private const string LayoutPath = "Panel/Layout/";
        private SudokuGameController _game;
        private SudokuBoardWidget _board;
        private SudokuHintWidget _hint;
        private XYButton _undo, _clear, _notes, _pause;
        private TMP_Text _level, _progress, _time, _noteState;
        private UIThemeColor _noteStateColor;
        private UnityEngine.UI.Image _pauseIcon;
        private Sprite _pauseSprite, _playSprite;
        private bool _loadingPlayIcon, _destroyed;
        private readonly List<SudokuNumberWidget> _numbers = new List<SudokuNumberWidget>(9);
        private long _displaySecond = -1;
        private bool _bannerRequested;
        private bool _bannerUnlocked;
        private bool _openingSettings;
        private bool _openingSettlement;
        private string _shownSettlement;
        private float _retrySettlementAt;
        public bool IsBoardReady => _board?.IsReady == true;
        public SudokuSession Session => _game?.Session;
        private TMP_Text _undoLabel, _clearLabel, _notesLabel, _hintLabel;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _undoLabel = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/UnDoBtn/m_text");
            _clearLabel = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/ClearBtn/m_text");
            _notesLabel = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/NoteBtn/m_text");
            _hintLabel = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/TipsBtn/m_text");
            _level = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/Level");
            _progress = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/Progress");
            _time = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/m_time");
            _noteState = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/NoteBtn/Toggle/m_text");
            _noteStateColor = _noteState.GetComponent<UIThemeColor>();
            _pauseIcon = FindChildComponent<UnityEngine.UI.Image>(LayoutPath + "Top/StopBtn/Icon");
            _pauseIcon.preserveAspect = true;
            _pauseSprite = _pauseIcon.sprite;
            UIThemeColor.Bind(_pauseIcon, SudokuThemeRole.Text);
            _board = CreateWidget<SudokuBoardWidget>(LayoutPath + "Board/m_rect_Board");
            _hint = CreateWidget<SudokuHintWidget>(LayoutPath + "PropArea/TipsBtn");
            _undo = CreateWidget<XYButton>(LayoutPath + "PropArea/UnDoBtn");
            _clear = CreateWidget<XYButton>(LayoutPath + "PropArea/ClearBtn");
            _notes = CreateWidget<XYButton>(LayoutPath + "PropArea/NoteBtn");
            _pause = CreateWidget<XYButton>(LayoutPath + "Top/StopBtn");
            var back = CreateWidget<XYButton>(LayoutPath + "Top/BackBtn");
            var palette = CreateWidget<XYButton>(LayoutPath + "Top/ColorBtn");
            var settings = CreateWidget<XYButton>(LayoutPath + "Top/SettingBtn");
            _undo.OnAddListener(() => _game.Undo());
            _clear.OnAddListener(() => EnterDigit(0));
            _notes.OnAddListener(() => _game.ToggleNotes());
            _pause.OnAddListener(() => _game.TogglePause());
            back.OnAddListener(() => RequestPopup(EventDefine.SudokuBackRequested));
            palette.OnAddListener(() => SudokuPresenter.ShowPaletteAsync(this).Forget());
            settings.OnAddListener(() => OpenSettingsAsync().Forget());
        }
        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshState);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshLanguage);
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            _game = (UserData as SudokuContext)?.Game ?? throw new InvalidOperationException("缺少数独游戏数据。");
            var template = FindChild(LayoutPath + "Bottom/Layout/Num1");
            var objects = new GameObject[9]; objects[0] = template.gameObject;
            // 先复制未绑定事件的 Num1，保留你配置的父布局、间距和尺寸。
            for (int i = 1; i < 9; i++)
            {
                objects[i] = UnityEngine.Object.Instantiate(template.gameObject, template.parent);
                objects[i].name = "Num" + (i + 1);
            }
            for (int i = 0; i < 9; i++)
            {
                var number = CreateWidget<SudokuNumberWidget>(objects[i]);
                number.Init(i + 1, EnterDigit); _numbers.Add(number);
            }
            _hint.Init(_game);
            RefreshLabels();
        }
        protected override void OnRefresh()
        {
            base.OnRefresh();
            _game = (UserData as SudokuContext)?.Game ?? _game;
            _game.SetWindowActive(!SudokuPresenter.IsNavigating);
            _hint.Init(_game);
            AdSystem.EnterPage(AdIds.Tip);
            RefreshState();
            RefreshPauseIcon();
            LoadPlayIconAsync().Forget();
        }
        private void RefreshLanguage()
        {
            RefreshLabels(); RefreshState();
        }
        private void RefreshLabels()
        {
            _undoLabel.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_undo);
            _clearLabel.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_clear);
            _notesLabel.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_notes);
            _hintLabel.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_hint);
        }
        protected override void OnClose()
        {
            GameModule.UI.CloseUI<UIGuide>();
            GameModule.UI.CloseUI<UIReward>();
            GameModule.UI.CloseUI<UISettlement>();
            GameModule.UI.CloseUI<UIPalette>();
            GameModule.UI.CloseUI<UISetting>();
            SetBannerVisible(false);
            _game?.SetWindowActive(false);
            AdSystem.ExitPage(AdIds.Tip);
            base.OnClose();
        }
        private void RequestPopup(int eventId)
        {
            if (_game.IsAdShowing) return;
            _game.SaveGameProgress();
            // 弹窗由后续预制体接收入口事件，本窗口不生成临时 UI。
            GameEvent.Send(eventId);
            Log.Info("[UISudoku] 已触发界面入口事件: " + eventId);
        }
        private void EnterDigit(int digit)
        {
            if (IsBoardReady && _game.EnterDigit(_game.SelectedCell, digit, _game.NoteMode))
                SudokuSettingsManager.Vibrate();
        }
        private async UniTaskVoid OpenSettingsAsync()
        {
            if (_openingSettings || _game.IsAdShowing) return;
            _openingSettings = true;
            int version = OpenVersion;
            using (UIInteractionLock.Acquire())
            {
                try
                {
                    await GameModule.UI.ShowUIAsyncAwait<UISetting>();
                    if (!IsOpen || OpenVersion != version) GameModule.UI.CloseUI<UISetting>();
                }
                catch (OperationCanceledException) { }
                finally { _openingSettings = false; }
            }
        }
        private void RefreshState()
        {
            _bannerUnlocked = false;
            if (_game != null)
            {
                int wins = 0;
                for (int difficulty = 0; difficulty < 6; difficulty++)
                    wins += _game.Session.CompletedCount((SudokuDifficulty)difficulty);
                _bannerUnlocked = wins >= AdIds.BannerUnlockWins;
            }
            RefreshBanner();
            if (_game == null || !IsBoardReady) return;
            _level.text = _game.IsDaily ? string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_selected_info),
                _game.DailyDate.Month, _game.DailyDate.Day, SudokuUIFormatting.Difficulty(Session.Difficulty)) : SudokuUIFormatting.Difficulty(Session.Difficulty);
            int filled = 0;
            for (int i = 0; i < 81; i++) if (Session.Value(i) != 0) filled++;
            _progress.text = Session.IsComplete ? LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_complete) : string.Format(LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_progress), filled);
            _board.Refresh(_game);
            bool canEdit = _game.CanInteract && _game.SelectedCell >= 0 && !Session.IsGiven(_game.SelectedCell);
            for (int i = 0; i < _numbers.Count; i++)
                _numbers[i].RefreshDisplay(canEdit, Session.RemainingCount(i + 1), SudokuSettingsManager.Data);
            _undo.Interactable = _game.CanInteract && Session.CanUndo;
            _clear.Interactable = canEdit;
            _notes.Interactable = _game.CanInteract;
            _pause.Interactable = !Session.IsComplete && !_game.IsAdShowing;
            _noteState.text = LocalizationHelper.GetLocalText(_game.NoteMode ? LanguageKey.sudoku_ui_on : LanguageKey.sudoku_ui_off);
            _noteStateColor.SetRole(_game.NoteMode ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
            RefreshPauseIcon();
            _hint.RefreshDisplay();
            RefreshTime();
        }
        private void RefreshPauseIcon()
        {
            if (_pauseIcon != null)
                _pauseIcon.sprite = _game != null && _game.IsPaused && _playSprite != null ? _playSprite : _pauseSprite;
        }
        private async UniTaskVoid LoadPlayIconAsync()
        {
            if (_playSprite != null || _loadingPlayIcon || _destroyed) return;
            _loadingPlayIcon = true;
            Sprite asset = null;
            try
            {
                var token = gameObject.GetCancellationTokenOnDestroy();
                asset = await GameModule.Resource.LoadAssetAsync<Sprite>("Sudoku_icon_play", token);
                token.ThrowIfCancellationRequested();
                if (_destroyed) return;
                if (asset == null) throw new InvalidOperationException("继续游戏图标加载失败。");
                _playSprite = asset; asset = null;
                // 异步加载完成后按当前状态刷新，快速连点也不会留下旧状态的图标。
                RefreshPauseIcon();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                if (asset != null) GameModule.Resource.UnloadAsset(asset);
                _loadingPlayIcon = false;
            }
        }
        protected override void OnDestroy()
        {
            _destroyed = true;
            GameSDK.SDK.DestroyBanner();
            if (_pauseIcon != null) _pauseIcon.sprite = _pauseSprite;
            if (_playSprite != null) GameModule.Resource.UnloadAsset(_playSprite);
            _playSprite = null;
            base.OnDestroy();
        }
        private void RefreshTime()
        {
            long seconds = (long)_game.ElapsedSeconds;
            if (seconds == _displaySecond) return;
            _displaySecond = seconds;
            _time.text = SudokuUIFormatting.Time(_game.ElapsedSeconds);
        }
        private void RefreshBanner()
        {
            SetBannerVisible(_bannerUnlocked && IsOpen && Visible && IsBoardReady &&
                _game.CanInteract && !SudokuPresenter.IsNavigating && !UIInteractionLock.IsLocked &&
                GameModule.UI.GetTopWindow((int)UILayer.UI) == WindowName &&
                string.IsNullOrEmpty(GameModule.UI.GetTopWindow((int)UILayer.Top)) &&
                string.IsNullOrEmpty(GameModule.UI.GetTopWindow((int)UILayer.Tips)));
        }

        private void SetBannerVisible(bool visible)
        {
            visible &= !string.IsNullOrWhiteSpace(AdIds.Banner);
            if (_bannerRequested == visible) return;
            _bannerRequested = visible;
            if (visible) GameSDK.SDK.ShowBanner(AdIds.Banner);
            else GameSDK.SDK.HideBanner();
        }

        protected override void OnUpdate()
        {
            RefreshBanner();
            if (_game == null || !IsBoardReady) return;
            if (IsOpen && _game.Session.IsComplete && _game.Settlement != null &&
                _shownSettlement != _game.Settlement.id && !_openingSettlement && !SudokuPresenter.IsNavigating &&
                Time.unscaledTime >= _retrySettlementAt)
                OpenSettlementAsync().Forget();
#if !TE_RELEASE
            if (GMSingle.IsPanelOpen) return;
#endif
            _game.Tick(Time.unscaledDeltaTime); RefreshTime();
            if (!_game.CanInteract || UIInteractionLock.IsLocked || !Input.anyKeyDown) return;
            var focused = EventSystem.current?.currentSelectedGameObject;
            if (focused != null && focused.activeInHierarchy && focused.GetComponentInParent<TMP_InputField>() != null) return;
            for (int d = 1; d <= 9; d++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + d)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + d))) EnterDigit(d);
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) EnterDigit(0);
            if (Input.GetKeyDown(KeyCode.N)) _game.ToggleNotes();
            int next = _game.SelectedCell;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) next = Math.Max(0, next - 1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) next = Math.Min(80, next + 1);
            if (Input.GetKeyDown(KeyCode.UpArrow)) next = Math.Max(0, next - 9);
            if (Input.GetKeyDown(KeyCode.DownArrow)) next = Math.Min(80, next + 9);
            if (next != _game.SelectedCell) _game.SelectCell(next);
        }

        private async UniTaskVoid OpenSettlementAsync()
        {
            _openingSettlement = true;
            var result = _game.Settlement;
            int version = OpenVersion;
            try
            {
                // 保留最后一次行列宫完成波纹，再展示结算。
                await UniTask.Delay(650, ignoreTimeScale: true, cancellationToken: CloseToken);
                if (!IsOpen || version != OpenVersion || !ReferenceEquals(result, _game.Settlement) || SudokuPresenter.IsNavigating) return;
                using (UIInteractionLock.Acquire())
                {
                    GameModule.UI.CloseUI<UIPalette>(); GameModule.UI.CloseUI<UISetting>();
                    var popup = await GameModule.UI.ShowUIAsyncAwait<UISettlement>(new SettlementContext(_game, result));
                    if (!IsOpen || version != OpenVersion || !ReferenceEquals(result, _game.Settlement)) GameModule.UI.CloseUI<UISettlement>();
                    else if (popup != null) _shownSettlement = result.id;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error); _retrySettlementAt = Time.unscaledTime + 5; }
            finally { _openingSettlement = false; }
        }
    }
}

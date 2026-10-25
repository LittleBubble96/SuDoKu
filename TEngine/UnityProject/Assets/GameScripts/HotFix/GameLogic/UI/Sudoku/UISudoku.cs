using System;
using System.Collections.Generic;
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
        private UIThemeColor _noteStateColor, _pauseColor;
        private readonly List<SudokuNumberWidget> _numbers = new List<SudokuNumberWidget>(9);
        private long _displaySecond = -1;
        public bool IsBoardReady => _board?.IsReady == true;
        public SudokuSession Session => _game?.Session;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _level = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/Level");
            _progress = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/Progress");
            _time = FindChildComponent<TMP_Text>(LayoutPath + "Board/SudoInfoPanel/m_time");
            _noteState = FindChildComponent<TMP_Text>(LayoutPath + "PropArea/NoteBtn/Toggle/m_text");
            _noteStateColor = _noteState.GetComponent<UIThemeColor>();
            _pauseColor = FindChildComponent<UIThemeColor>(LayoutPath + "Top/StopBtn/Icon");
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
            palette.OnAddListener(() => RequestPopup(EventDefine.SudokuPaletteRequested));
            settings.OnAddListener(() => RequestPopup(EventDefine.SudokuSettingsRequested));
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
        }
        private void RefreshLanguage()
        {
            RefreshLabels(); RefreshState();
        }
        private void RefreshLabels()
        {
            FindChildComponent<TMP_Text>(LayoutPath + "PropArea/UnDoBtn/m_text").text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_undo);
            FindChildComponent<TMP_Text>(LayoutPath + "PropArea/ClearBtn/m_text").text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_clear);
            FindChildComponent<TMP_Text>(LayoutPath + "PropArea/NoteBtn/m_text").text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_notes);
            FindChildComponent<TMP_Text>(LayoutPath + "PropArea/TipsBtn/m_text").text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_hint);
        }
        protected override void OnClose()
        {
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
            if (IsBoardReady) _game.EnterDigit(_game.SelectedCell, digit, _game.NoteMode);
        }
        private void RefreshState()
        {
            if (_game == null || !IsBoardReady) return;
            _level.text = _game.IsDaily ? string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_selected_info),
                _game.DailyDate.Month, _game.DailyDate.Day, SudokuUIFormatting.Difficulty(Session.Difficulty)) : SudokuUIFormatting.Difficulty(Session.Difficulty);
            int filled = 0;
            for (int i = 0; i < 81; i++) if (Session.Value(i) != 0) filled++;
            _progress.text = Session.IsComplete ? LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_complete) : string.Format(LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_progress), filled);
            _board.Refresh(_game);
            bool canEdit = _game.CanInteract && _game.SelectedCell >= 0 && !Session.IsGiven(_game.SelectedCell);
            foreach (var number in _numbers) number.SetInteractable(canEdit);
            _undo.Interactable = _game.CanInteract && Session.CanUndo;
            _clear.Interactable = canEdit;
            _notes.Interactable = _game.CanInteract;
            _pause.Interactable = !Session.IsComplete && !_game.IsAdShowing;
            _noteState.text = LocalizationHelper.GetLocalText(_game.NoteMode ? LanguageKey.sudoku_ui_on : LanguageKey.sudoku_ui_off);
            _noteStateColor.SetRole(_game.NoteMode ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
            _pauseColor.SetRole(_game.IsPaused ? SudokuThemeRole.Accent : SudokuThemeRole.Text);
            _hint.RefreshDisplay();
            RefreshTime();
        }
        private void RefreshTime()
        {
            long seconds = (long)_game.ElapsedSeconds;
            if (seconds == _displaySecond) return;
            _displaySecond = seconds;
            _time.text = SudokuUIFormatting.Time(_game.ElapsedSeconds);
        }
        protected override void OnUpdate()
        {
            if (_game == null || !IsBoardReady) return;
#if !TE_RELEASE
            if (GMSingle.IsPanelOpen) return;
#endif
            _game.Tick(Time.unscaledDeltaTime); RefreshTime();
            if (!_game.CanInteract || UIInteractionLock.IsLocked) return;
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
    }
}

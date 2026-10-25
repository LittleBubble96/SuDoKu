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
    public sealed class UIHomeDailyWidget : UIWidget
    {
        private const string Calendar = "Center/Layout/Content/Panel/Calendar/";
        private const string Panel = "Center/Layout/Content/Panel/";
        private const string Bottom = "Center/Layout/Bottom/Panel/";
        private const string Top = "Center/Layout/Top/";
        private static readonly string[] Weekdays =
        {
            LanguageKey.daily_weekday_monday, LanguageKey.daily_weekday_tuesday, LanguageKey.daily_weekday_wednesday,
            LanguageKey.daily_weekday_thursday, LanguageKey.daily_weekday_friday, LanguageKey.daily_weekday_saturday, LanguageKey.daily_weekday_sunday
        };
        private readonly List<DailyCalendarItem> _days = new List<DailyCalendarItem>(42);
        private XYButton _previous, _next, _start;
        private TMP_Text _month, _info, _status, _startLabel, _monthProgress;
        private UnityEngine.UI.Image _monthFill;
        private GameObject _statusDot;
        private DateTime _lastToday;
        private bool _opening, _openingTrophies;
        public DateTime SelectedDate { get; private set; }
        public DateTime ViewedMonth { get; private set; }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _previous = CreateWidget<XYButton>(Panel + "Left/Btn");
            _next = CreateWidget<XYButton>(Panel + "Right/Btn");
            _previous.OnAddListener(() => MoveMonth(-1));
            _next.OnAddListener(() => MoveMonth(1));
            _start = CreateWidget<XYButton>(Bottom + "ContinueGameBtn");
            _start.OnAddListener(() => StartAsync().Forget());
            CreateWidget<XYButton>("Top/Back/Btn").OnAddListener(() => (OwnerWindow as UIHome)?.SelectTab(0));
            CreateWidget<XYButton>("Top/Trophy/Btn").OnAddListener(() => OpenTrophiesAsync().Forget());
            _month = FindChildComponent<TMP_Text>(Panel + "m_title");
            _info = FindChildComponent<TMP_Text>(Bottom + "m_title");
            _status = FindChildComponent<TMP_Text>(Bottom + "m_des");
            _startLabel = FindChildComponent<TMP_Text>(Bottom + "ContinueGameBtn/m_text");
            _monthProgress = FindChildComponent<TMP_Text>(Top + "Progress/m_progress");
            _monthFill = FindChildComponent<UnityEngine.UI.Image>(Top + "Progress/ProgressFill");
            _statusDot = FindChild(Bottom + "bubble").gameObject;
            _monthFill.type = UnityEngine.UI.Image.Type.Filled;
            _monthFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            BindThemes();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var today = SudokuPresenter.Current.Today;
            SelectedDate = today < SudokuDailyRules.FirstDate ? SudokuDailyRules.FirstDate : today;
            ViewedMonth = SudokuDailyRules.Month(SelectedDate);
            var template = FindChild(Calendar + "Layout/CalendarItem1");
            template.gameObject.SetActive(false);
            var grid = template.parent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid != null) { grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 7; }
            for (int i = 0; i < 42; i++)
            {
                var item = CreateWidgetByPrefab<DailyCalendarItem>(template.gameObject, template.parent);
                item.gameObject.name = "CalendarDay" + i;
                _days.Add(item);
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnRefresh() { base.OnRefresh(); RefreshDisplay(); }

        public void SelectDate(DateTime date)
        {
            if (!SudokuDailyRules.CanStart(date, SudokuPresenter.Current.Today)) return;
            SelectedDate = date.Date; ViewedMonth = SudokuDailyRules.Month(date);
            RefreshDisplay();
        }

        public void MoveMonth(int direction)
        {
            if (direction != -1 && direction != 1) return;
            var today = SudokuPresenter.Current.Today;
            if (direction < 0 && ViewedMonth <= SudokuDailyRules.FirstDate || direction > 0 && ViewedMonth >= SudokuDailyRules.Month(today)) return;
            ViewedMonth = ViewedMonth.AddMonths(direction);
            SelectedDate = ViewedMonth.AddDays(Math.Min(SelectedDate.Day, DateTime.DaysInMonth(ViewedMonth.Year, ViewedMonth.Month)) - 1);
            if (SelectedDate > today) SelectedDate = today;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            var game = SudokuPresenter.Current;
            if (game == null || _days.Count != 42) return;
            _lastToday = game.Today;
            var record = game.GetDaily(SelectedDate);
            bool completed = record?.completed == true;
            _month.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_calendar_month), ViewedMonth.Year, ViewedMonth.Month);
            for (int i = 0; i < 7; i++) Label(Calendar + "m_" + (i + 1), Weekdays[i]);
            int offset = SudokuDailyRules.MondayOffset(ViewedMonth);
            var first = ViewedMonth.AddDays(-offset);
            for (int i = 0; i < 42; i++)
            {
                var date = first.AddDays(i);
                _days[i].Refresh(date, date.Month == ViewedMonth.Month && date.Year == ViewedMonth.Year, _lastToday, SelectedDate, game.GetDaily(date), SelectDate);
            }
            var difficulty = record?.game != null ? (SudokuDifficulty)record.game.difficulty : SudokuDailyRules.Difficulty(SelectedDate);
            _info.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_selected_info), SelectedDate.Month, SelectedDate.Day, SudokuUIFormatting.Difficulty(difficulty));
            _status.text = completed ? LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_complete) : record == null ? "0%" :
                string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_progress), record.progressPercent);
            _statusDot.SetActive(record != null);
            _startLabel.text = LocalizationHelper.GetLocalText(completed ? LanguageKey.sudoku_ui_complete : record == null ? LanguageKey.challenge_btn :
                SelectedDate == _lastToday ? LanguageKey.daily_continue_today : LanguageKey.continue_btn);
            _start.Interactable = !_opening && !SudokuPresenter.IsNavigating && game.CanOpenDaily(SelectedDate);
            _previous.Interactable = ViewedMonth > SudokuDailyRules.FirstDate;
            _next.Interactable = ViewedMonth < SudokuDailyRules.Month(_lastToday);
            int wins = game.CompletedDays(ViewedMonth), days = DateTime.DaysInMonth(ViewedMonth.Year, ViewedMonth.Month);
            _monthProgress.text = wins + "/" + days;
            _monthFill.fillAmount = wins / (float)days;
            Label("Top/Center/m_title", LanguageKey.daily_challenge);
            Label("Top/Trophy/Btn/m_text", LanguageKey.daily_trophy_hall);
            Label(Top + "m_des1", LanguageKey.daily_monthly_trophy);
            Label(Top + "m_des2", LanguageKey.daily_trophy_unlock_desc);
            Label(Top + "m_title", LanguageKey.daily_trophy_leo);
        }

        protected override void OnUpdate()
        {
            if (!Visible || !OwnerWindow.IsOpen || SudokuPresenter.Current == null) return;
            var today = SudokuPresenter.Current.Today;
            if (today == _lastToday) return;
            if (SelectedDate == _lastToday && today >= SudokuDailyRules.FirstDate) { SelectedDate = today; ViewedMonth = SudokuDailyRules.Month(today); }
            RefreshDisplay();
        }

        private async UniTaskVoid StartAsync()
        {
            if (_opening) return;
            _opening = true; RefreshDisplay();
            try { await SudokuPresenter.StartDailyChallengeAsync(SelectedDate); }
            catch (Exception error) { Debug.LogException(error); }
            finally { _opening = false; if (OwnerWindow.IsOpen) RefreshDisplay(); }
        }

        private async UniTaskVoid OpenTrophiesAsync()
        {
            if (_openingTrophies || SudokuPresenter.IsNavigating) return;
            _openingTrophies = true; int version = OwnerWindow.OpenVersion;
            try
            {
                var gallery = await GameModule.UI.ShowUIAsyncAwait<UITrophyGallery>(new UITrophyGallery.Context(SudokuPresenter.Current, ViewedMonth.Year));
                if (!OwnerWindow.IsOpen || OwnerWindow.OpenVersion != version) GameModule.UI.CloseUI<UITrophyGallery>();
                else if (gallery != null) await gallery.WaitForContentAsync(OwnerWindow.gameObject.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { Debug.LogException(error); }
            finally { _openingTrophies = false; }
        }

        private void Label(string path, string key) => FindChildComponent<TMP_Text>(path).text = LocalizationHelper.GetLocalText(key);

        private void Theme(string path, SudokuThemeRole role)
        {
            var graphic = FindChild(path).GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null) UIThemeColor.Bind(graphic, role);
        }

        private void BindThemes()
        {
            UIThemeColor.Bind(_month, SudokuThemeRole.Text);
            UIThemeColor.Bind(_info, SudokuThemeRole.Text);
            UIThemeColor.Bind(_status, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_startLabel, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_monthProgress, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_monthFill, SudokuThemeRole.Accent);
            Theme(Top + "Progress/ProgressBg", SudokuThemeRole.Matching);
            Theme(Bottom + "ContinueGameBtn/bg", SudokuThemeRole.Accent);
            Theme(Bottom + "bubble", SudokuThemeRole.Accent);
            Theme(Panel + "Left/Btn/icon", SudokuThemeRole.Text);
            Theme(Panel + "Right/Btn/icon", SudokuThemeRole.Text);
            Theme("Top/Back/Btn/icon", SudokuThemeRole.Text);
            Theme("Top/Center/m_title", SudokuThemeRole.Text);
            Theme("Top/Trophy/Btn/icon", SudokuThemeRole.Accent);
            Theme("Top/Trophy/Btn/m_text", SudokuThemeRole.Accent);
            Theme(Top + "m_title", SudokuThemeRole.Text);
            Theme(Top + "m_des1", SudokuThemeRole.MutedText);
            Theme(Top + "m_des2", SudokuThemeRole.MutedText);
            for (int i = 1; i <= 7; i++) Theme(Calendar + "m_" + i, SudokuThemeRole.MutedText);
        }
    }
}

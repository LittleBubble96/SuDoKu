using System;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;

namespace GameLogic
{
    public sealed class UIHomeHomeWidget : UIWidget
    {
        private const string Cards = "Center/Layout/Content/";
        private TMP_Text _continueInfo, _dailyInfo;
        private XYButton _continue;
        private bool _openingDifficulty;
        private DateTime _displayDate;

        private TMP_Text _signInLabel, _tasksLabel, _title, _otherGamesLabel, _dailyLabel, _dailyGoLabel, _continueLabel, _newGameLabel;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _signInLabel = FindChildComponent<TMP_Text>("Top/Right/Layout/SignIn/Btn/m_text");
            _tasksLabel = FindChildComponent<TMP_Text>("Top/Right/Layout/DailyTask/Btn/m_text");
            _title = FindChildComponent<TMP_Text>("Center/Layout/Top/m_text");
            _otherGamesLabel = FindChildComponent<TMP_Text>(Cards + "OtherGameBtn/m_text");
            _dailyLabel = FindChildComponent<TMP_Text>(Cards + "DailyChallengeBtn/m_text");
            _dailyGoLabel = FindChildComponent<TMP_Text>(Cards + "DailyChallengeBtn/Go/m_text");
            _continueLabel = FindChildComponent<TMP_Text>(Cards + "ContinueGameBtn/m_text");
            _newGameLabel = FindChildComponent<TMP_Text>(Cards + "NewGameBtn/m_text");
            var coinDisplay = CreateWidget<CoinDisplayWidget>(FindChild("Top/CoinRoot/Btn/m_num").gameObject);
            coinDisplay.SetAnimationRoot(FindChildComponent<UnityEngine.RectTransform>("Top/CoinRoot"));
            _continueInfo = FindChildComponent<TMP_Text>(Cards + "ContinueGameBtn/m_des");
            _dailyInfo = FindChildComponent<TMP_Text>(Cards + "DailyChallengeBtn/m_des");
            _continue = CreateWidget<XYButton>(Cards + "ContinueGameBtn");
            _continue.OnAddListener(() => SudokuPresenter.ContinueGameAsync().Forget());
            CreateWidget<XYButton>(Cards + "DailyChallengeBtn").OnAddListener(() => (OwnerWindow as UIHome)?.SelectDailyDate(SudokuPresenter.Current.Today));
            CreateWidget<XYButton>(Cards + "NewGameBtn").OnAddListener(() => OpenDifficultyAsync().Forget());
            CreateWidget<XYButton>("Top/Right/Layout/ColorTheme/Btn")
                .OnAddListener(() => SudokuPresenter.ShowPaletteAsync(OwnerWindow).Forget());
            CreateWidget<XYButton>("Top/Right/Layout/DailyTask/Btn")
                .OnAddListener(() => SudokuPresenter.ShowDailyTaskAsync(OwnerWindow).Forget());
            CreateWidget<XYButton>("Top/Right/Layout/SignIn/Btn")
                .OnAddListener(() => SudokuPresenter.ShowDailySignInAsync(OwnerWindow).Forget());
            BindThemes();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnRefresh() { base.OnRefresh(); RefreshDisplay(); }

        public void RefreshDisplay()
        {
            var game = SudokuPresenter.Current;
            if (game == null) return;
            _continueInfo.text = SudokuUIFormatting.Difficulty(game.NormalDifficulty) + " · " + SudokuUIFormatting.Time(game.NormalIsComplete ? 0 : game.NormalElapsedSeconds);
            bool canContinue = game.HasNormalGame && !game.NormalIsComplete;
            _continue.Visible = canContinue;
            _continue.Interactable = canContinue;
            _displayDate = game.Today;
            var daily = game.GetDaily(_displayDate);
            _dailyInfo.text = daily == null ? string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_incomplete), _displayDate.Month, _displayDate.Day) :
                string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_selected_info), _displayDate.Month, _displayDate.Day,
                    daily.completed ? LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_complete) :
                    string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_progress), daily.progressPercent));
            _signInLabel.text = LocalizationHelper.GetLocalText(LanguageKey.home_check_in);
            _tasksLabel.text = LocalizationHelper.GetLocalText(LanguageKey.home_daily_tasks);
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_title);
            _otherGamesLabel.text = LocalizationHelper.GetLocalText(LanguageKey.home_more_games);
            _dailyLabel.text = LocalizationHelper.GetLocalText(LanguageKey.daily_challenge);
            _dailyGoLabel.text = LocalizationHelper.GetLocalText(LanguageKey.challenge_btn);
            _continueLabel.text = LocalizationHelper.GetLocalText(LanguageKey.continue_btn);
            _newGameLabel.text = LocalizationHelper.GetLocalText(LanguageKey.new_game_btn);
        }

        protected override void OnUpdate()
        {
            if (Visible && OwnerWindow.IsOpen && SudokuPresenter.Current != null && _displayDate != SudokuPresenter.Current.Today) RefreshDisplay();
        }

        private async UniTaskVoid OpenDifficultyAsync()
        {
            if (_openingDifficulty || SudokuPresenter.IsNavigating) return;
            _openingDifficulty = true;
            int version = OwnerWindow.OpenVersion;
            try
            {
                var popup = await GameModule.UI.ShowUIAsyncAwait<UIHomeNewGame>(new SudokuContext(SudokuPresenter.Current));
                if (!OwnerWindow.IsOpen || OwnerWindow.OpenVersion != version) GameModule.UI.CloseUI<UIHomeNewGame>();
            }
            catch (OperationCanceledException) { }
            finally { _openingDifficulty = false; }
        }

        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Graphic>(path), role);

        private void BindThemes()
        {
            Theme("Top/CoinRoot/Btn/m_num", SudokuThemeRole.Text);
            Theme("Top/Right/Layout/SignIn/Btn/icon", SudokuThemeRole.Accent);
            Theme("Top/Right/Layout/DailyTask/Btn/icon", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_signInLabel, SudokuThemeRole.Accent);
            UIThemeColor.Bind(_tasksLabel, SudokuThemeRole.Accent);
            Theme("Top/Right/Layout/ColorTheme/Btn/icon", SudokuThemeRole.Accent);
            Theme("Center/Layout/Top/icon", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            Theme(Cards + "OtherGameBtn/bg", SudokuThemeRole.Matching);
            UIThemeColor.Bind(_otherGamesLabel, SudokuThemeRole.Text);
            Theme(Cards + "OtherGameBtn/icon", SudokuThemeRole.Accent);
            Theme(Cards + "OtherGameBtn/iconGo", SudokuThemeRole.MutedText);
            Theme(Cards + "DailyChallengeBtn/bg", SudokuThemeRole.Related);
            Theme(Cards + "DailyChallengeBtn/bg/outline", SudokuThemeRole.Matching);
            Theme(Cards + "DailyChallengeBtn/icon", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_dailyLabel, SudokuThemeRole.Text);
            UIThemeColor.Bind(_dailyInfo, SudokuThemeRole.MutedText);
            Theme(Cards + "DailyChallengeBtn/Go", SudokuThemeRole.Surface);
            Theme(Cards + "DailyChallengeBtn/Go/iconGo", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_dailyGoLabel, SudokuThemeRole.Accent);
            Theme(Cards + "ContinueGameBtn/bg", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_continueLabel, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_continueInfo, SudokuThemeRole.Surface);
            Theme(Cards + "ContinueGameBtn/iconTime", SudokuThemeRole.Surface);
            Theme(Cards + "NewGameBtn/bg", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_newGameLabel, SudokuThemeRole.Accent);
        }
    }
}

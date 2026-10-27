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
        private TMP_Text _coins, _continueInfo, _dailyInfo;
        private XYButton _continue;
        private bool _openingDifficulty;
        private DateTime _displayDate;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _coins = FindChildComponent<TMP_Text>("Top/CoinRoot/Btn/m_num");
            _continueInfo = FindChildComponent<TMP_Text>(Cards + "ContinueGameBtn/m_des");
            _dailyInfo = FindChildComponent<TMP_Text>(Cards + "DailyChallengeBtn/m_des");
            _continue = CreateWidget<XYButton>(Cards + "ContinueGameBtn");
            _continue.OnAddListener(() => SudokuPresenter.ContinueGameAsync().Forget());
            CreateWidget<XYButton>(Cards + "DailyChallengeBtn").OnAddListener(() => (OwnerWindow as UIHome)?.SelectDailyDate(SudokuPresenter.Current.Today));
            CreateWidget<XYButton>(Cards + "NewGameBtn").OnAddListener(() => OpenDifficultyAsync().Forget());
            CreateWidget<XYButton>("Top/Right/Layout/ColorTheme/Btn")
                .OnAddListener(() => SudokuPresenter.ShowPaletteAsync(OwnerWindow).Forget());
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
            _coins.text = game.Props.coinCount.ToString();
            _continueInfo.text = SudokuUIFormatting.Difficulty(game.NormalDifficulty) + " · " + SudokuUIFormatting.Time(game.NormalIsComplete ? 0 : game.NormalElapsedSeconds);
            _continue.Interactable = game.HasNormalGame;
            _displayDate = game.Today;
            var daily = game.GetDaily(_displayDate);
            _dailyInfo.text = daily == null ? string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_incomplete), _displayDate.Month, _displayDate.Day) :
                string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_selected_info), _displayDate.Month, _displayDate.Day,
                    daily.completed ? LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_complete) :
                    string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_progress), daily.progressPercent));
            Label("Top/Right/Layout/SignIn/Btn/m_text", LanguageKey.home_check_in);
            Label("Top/Right/Layout/DailyTask/Btn/m_text", LanguageKey.home_daily_tasks);
            Label("Center/Layout/Top/m_text", LanguageKey.sudoku_title);
            Label(Cards + "OtherGameBtn/m_text", LanguageKey.home_more_games);
            Label(Cards + "DailyChallengeBtn/m_text", LanguageKey.daily_challenge);
            Label(Cards + "DailyChallengeBtn/Go/m_text", LanguageKey.challenge_btn);
            Label(Cards + "ContinueGameBtn/m_text", LanguageKey.continue_btn);
            Label(Cards + "NewGameBtn/m_text", LanguageKey.new_game_btn);
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

        private void Label(string path, string key) => FindChildComponent<TMP_Text>(path).text = LocalizationHelper.GetLocalText(key);
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Graphic>(path), role);

        private void BindThemes()
        {
            Theme("Top/CoinRoot/Btn/m_num", SudokuThemeRole.Text);
            foreach (string action in new[] { "SignIn", "DailyTask" })
            {
                Theme("Top/Right/Layout/" + action + "/Btn/icon", SudokuThemeRole.Accent);
                Theme("Top/Right/Layout/" + action + "/Btn/m_text", SudokuThemeRole.Accent);
            }
            Theme("Top/Right/Layout/ColorTheme/Btn/icon", SudokuThemeRole.Accent);
            Theme("Center/Layout/Top/icon", SudokuThemeRole.Accent);
            Theme("Center/Layout/Top/m_text", SudokuThemeRole.Text);
            Theme(Cards + "OtherGameBtn/bg", SudokuThemeRole.Matching);
            Theme(Cards + "OtherGameBtn/m_text", SudokuThemeRole.Text);
            Theme(Cards + "OtherGameBtn/icon", SudokuThemeRole.Accent);
            Theme(Cards + "OtherGameBtn/iconGo", SudokuThemeRole.MutedText);
            Theme(Cards + "DailyChallengeBtn/bg", SudokuThemeRole.Related);
            Theme(Cards + "DailyChallengeBtn/bg/outline", SudokuThemeRole.Matching);
            Theme(Cards + "DailyChallengeBtn/icon", SudokuThemeRole.Accent);
            Theme(Cards + "DailyChallengeBtn/m_text", SudokuThemeRole.Text);
            Theme(Cards + "DailyChallengeBtn/m_des", SudokuThemeRole.MutedText);
            Theme(Cards + "DailyChallengeBtn/Go", SudokuThemeRole.Surface);
            Theme(Cards + "DailyChallengeBtn/Go/iconGo", SudokuThemeRole.Accent);
            Theme(Cards + "DailyChallengeBtn/Go/m_text", SudokuThemeRole.Accent);
            Theme(Cards + "ContinueGameBtn/bg", SudokuThemeRole.Accent);
            Theme(Cards + "ContinueGameBtn/m_text", SudokuThemeRole.Surface);
            Theme(Cards + "ContinueGameBtn/m_des", SudokuThemeRole.Surface);
            Theme(Cards + "ContinueGameBtn/iconTime", SudokuThemeRole.Surface);
            Theme(Cards + "NewGameBtn/bg", SudokuThemeRole.Accent);
            Theme(Cards + "NewGameBtn/m_text", SudokuThemeRole.Accent);
        }
    }
}

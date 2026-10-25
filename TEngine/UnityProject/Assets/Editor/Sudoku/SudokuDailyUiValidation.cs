#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    public static class SudokuDailyUiValidation
    {
        private const string Bottom = "Center/Layout/Bottom/Panel/";
        private const string Calendar = "Center/Layout/Content/Panel/Calendar/";

        public static async UniTask RunAsync(string output, CancellationToken token)
        {
            Application.runInBackground = true;
            var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
            await home.WaitForContentAsync(token);
            var game = SudokuPresenter.Current;
            game.DebugResetProgress(); game.SetSuspended(false);
            await SudokuPresenter.ContinueGameAsync();
            game.SetSuspended(false);
            var ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
            Require(ui != null && ui.IsBoardReady && !game.IsDaily, "normal board ready");
            var normalPuzzle = game.Session.Capture();
            int normalCell = FirstEmpty(normalPuzzle);
            var normalSolution = Solve(normalPuzzle);
            Enter(ui, normalCell, normalSolution[normalCell]);
            int frame = Time.frameCount;
            double seconds = game.ElapsedSeconds;
            await UniTask.Delay(200, ignoreTimeScale: true, cancellationToken: token);
            Require(Time.frameCount > frame && game.ElapsedSeconds > seconds, "player loop and gameplay timer advance");
            await SudokuPresenter.ShowHomeAsync();
            string normalBefore = JsonUtility.ToJson(SudokuPresenter.Cache.CacheData.sudokuSaveData);

            DateTime first = SudokuDailyRules.FirstDate;
            home.SelectDailyDate(first);
            await home.WaitForContentAsync(token);
            var page = home.DailyPage;
            Require(page != null && page.Visible, "daily tab loads UIHome_DC widget");
            Require(page.FindChild(Calendar + "Layout").childCount == 43, "42 calendar slots and one hidden template");
            Require(!page.FindChild(Calendar + "Layout/CalendarItem1").gameObject.activeSelf, "calendar template hidden");
            Require(!page.FindChildComponent<Button>("Center/Layout/Content/Panel/Left/Btn").interactable, "January 2026 is earliest month");
            page.MoveMonth(-1);
            Require(page.ViewedMonth == first, "cannot navigate before January 2026");
            page.SelectDate(new DateTime(2026, 3, 1));
            Require(page.FindChildComponent<TMP_Text>(Calendar + "Layout/CalendarDay36/Btn/Bg/m_num").text == "31", "sixth calendar row contains March 31");
            page.SelectDate(game.Today);
            var future = game.Today.AddDays(1);
            int futureSlot = SudokuDailyRules.MondayOffset(game.Today) + future.Day - 1;
            if (future.Month == game.Today.Month)
                Require(!page.FindChildComponent<Button>(Calendar + "Layout/CalendarDay" + futureSlot + "/Btn").interactable, "future day disabled");
            int quickCursor = game.PeekNextIndex(SudokuDifficulty.Quick);
            page.SelectDate(first);
            Require(game.PeekNextIndex(SudokuDifficulty.Quick) == quickCursor && game.GetDaily(first) == null, "calendar selection does not allocate puzzle");
            page.FindChildComponent<Button>(Bottom + "ContinueGameBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            Require(game.IsDaily && game.DailyDate == first && game.Session.LevelIndex == quickCursor && game.GetDaily(first) != null, "start button binds date and shared-pool puzzle");
            Require(game.PeekNextIndex(SudokuDifficulty.Quick) == (quickCursor + 1) % game.Session.LevelCount(SudokuDifficulty.Quick), "UI start advances shared cursor once");
            Require(JsonUtility.ToJson(SudokuPresenter.Cache.CacheData.sudokuSaveData) == normalBefore, "UI daily start preserves normal save");
            ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
            game.SetSuspended(false);
            var dailyPuzzle = game.Session.Capture();
            int dailyCell = FirstEmpty(dailyPuzzle);
            var answer = Solve(dailyPuzzle);
            Enter(ui, dailyCell, answer[dailyCell]);
            game.Tick(2);
            int savedIndex = game.Session.LevelIndex;
            await Back(ui, token);
            Require(home.SelectedTab == 1 && page.SelectedDate == first, "back returns to selected daily date");
            int slot = SudokuDailyRules.MondayOffset(first);
            Require(page.FindChild(Calendar + "Layout/CalendarDay" + slot + "/Btn/Bg/ProgressRoot").gameObject.activeSelf, "started day shows progress circle");
            Require(page.FindChildComponent<Image>(Calendar + "Layout/CalendarDay" + slot + "/Btn/Bg/ProgressRoot/Fill").fillAmount > 0, "progress circle uses saved correct-cell progress");
            page.SelectDate(first.AddDays(6));
            page.FindChildComponent<Button>(Bottom + "ContinueGameBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            Require(game.DailyDate == first.AddDays(6) && game.Session.LevelIndex != savedIndex, "second date gets independent puzzle");
            ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
            await Back(ui, token);
            page.SelectDate(first);
            int cursorBeforeResume = game.PeekNextIndex(SudokuDifficulty.Quick);
            page.FindChildComponent<Button>(Bottom + "ContinueGameBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            Require(game.Session.LevelIndex == savedIndex && game.Session.Value(dailyCell) == answer[dailyCell] && game.ElapsedSeconds >= 2, "continue restores selected day's board and time");
            Require(game.PeekNextIndex(SudokuDifficulty.Quick) == cursorBeforeResume, "UI continue does not advance pool");
            ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
            game.SetSuspended(false);
            for (int i = 0; i < 81; i++) if (!game.Session.IsGiven(i)) Enter(ui, i, answer[i]);
            Require(game.Session.IsComplete && game.GetDaily(first).completed && game.CompletedDays(first) == 1, "real UI playthrough completes selected date once");
            Require(game.Session.CompletedCount(SudokuDifficulty.Quick) == 0 && !game.Session.IsUnlocked(SudokuDifficulty.Beginner), "UI daily completion does not unlock normal difficulty");
            await Back(ui, token);
            Require(!page.FindChildComponent<Button>(Bottom + "ContinueGameBtn").interactable, "completed date button disabled");
            Require(page.FindChild(Calendar + "Layout/CalendarDay" + slot + "/Btn/Bg/SuccessRoot").gameObject.activeSelf &&
                page.FindChild(Calendar + "Layout/CalendarDay" + slot + "/Btn/Bg/SelectRoot").gameObject.activeSelf, "selected completed day retains checkmark");
            var selectedBackground = page.FindChild(Calendar + "Layout/CalendarDay" + slot + "/Btn/Bg");
            Require(selectedBackground.Find("SelectRoot").GetSiblingIndex() < selectedBackground.Find("m_num").GetSiblingIndex() &&
                selectedBackground.Find("SelectRoot").GetSiblingIndex() < selectedBackground.Find("SuccessRoot").GetSiblingIndex(), "date and checkmark render above selected background");
            Require(page.FindChildComponent<TMP_Text>("Center/Layout/Top/Progress/m_progress").text == "1/31", "monthly completed-day count updated");
            await SudokuPresenter.StartDailyChallengeAsync(first);
            Require(home.IsOpen && game.PeekNextIndex(SudokuDifficulty.Quick) == cursorBeforeResume, "completed date cannot replay through presenter");
            var oldLanguage = GameModule.Localization.Language;
            GameModule.Localization.Language = Language.ChineseSimplified;
            SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(output, "daily-chinese.png"));
            GameModule.Localization.Language = Language.English;
            Require(page.FindChildComponent<TMP_Text>("Top/Center/m_title").text == "Daily Challenge", "daily English labels");
            SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(output, "daily-english.png"));
            GameModule.Localization.Language = oldLanguage;
            home.SelectTab(0); await home.WaitForContentAsync(token);
            await SudokuPresenter.ContinueGameAsync();
            Require(!game.IsDaily && game.Session.Value(normalCell) == normalSolution[normalCell], "normal continue remains independent of daily games");
            game.SetSuspended(false); game.Tick(12); game.DebugComplete();
            SudokuDifficulty completedDifficulty = game.NormalDifficulty;
            double completedTime = game.NormalElapsedSeconds;
            int nextNormal = game.PeekNextIndex(completedDifficulty);
            await SudokuPresenter.ShowHomeAsync();
            var normalPage = home.FindChild("Panel/Content/UIHome_Home");
            var continueButton = normalPage.Find("Center/Layout/Content/ContinueGameBtn").GetComponent<Button>();
            Require(continueButton.interactable, "completed normal continue button enabled");
            Require(normalPage.Find("Center/Layout/Content/ContinueGameBtn/m_des").GetComponent<TMP_Text>().text ==
                SudokuUIFormatting.Difficulty(completedDifficulty) + " · 00:00", "completed normal card displays same difficulty and zero time");
            Require(game.NormalElapsedSeconds == completedTime && game.PeekNextIndex(completedDifficulty) == nextNormal, "home display preserves completed time and pool cursor");
            continueButton.onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            Require(!game.IsDaily && !game.Session.IsComplete && game.Session.Difficulty == completedDifficulty && game.Session.LevelIndex == nextNormal && game.ElapsedSeconds < 1,
                "actual continue button starts next same-difficulty puzzle with reset timer");
            Require(game.PeekNextIndex(completedDifficulty) == (nextNormal + 1) % game.Session.LevelCount(completedDifficulty), "actual continue button advances cursor once");
            Require(!game.CanOpenDaily(first), "normal continue does not reopen completed daily date");
            await SudokuPresenter.ShowHomeAsync();
            File.WriteAllText(Path.Combine(output, "daily-ui-validation.txt"), "PASS: Actual player loop, normal/daily switching, 42 calendar positions, 2026 lower bound, six-row month, future dates, shared pool, per-date resume, real button playthrough, completed-date lock, monthly count and Chinese/English UI. Completed ordinary game keeps Continue enabled, displays same difficulty and 00:00, preserves completed time until clicked, then starts next puzzle and advances cursor exactly once.\n");
        }

        private static async UniTask Back(UISudoku ui, CancellationToken token)
        {
            ui.FindChildComponent<Button>("Panel/Layout/Top/BackBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
        }

        private static void Enter(UISudoku ui, int cell, int digit)
        {
            ui.FindChild("Panel/Layout/Board/m_rect_Board/Cell" + cell).GetComponent<Button>().onClick.Invoke();
            ui.FindChildComponent<Button>("Panel/Layout/Bottom/Layout/Num" + digit).onClick.Invoke();
        }

        private static byte[] Solve(SudokuSaveData data)
        {
            var puzzle = new byte[81]; for (int i = 0; i < 81; i++) puzzle[i] = (byte)data.puzzle[i];
            Require(SudokuSolver.Solve(puzzle, out var answer) == 1, "unique test puzzle"); return answer;
        }

        private static int FirstEmpty(SudokuSaveData data)
        {
            for (int i = 0; i < 81; i++) if (data.puzzle[i] == 0) return i;
            throw new InvalidOperationException("Test puzzle has no empty cells");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Daily UI validation failed: " + message);
        }
    }
}
#endif

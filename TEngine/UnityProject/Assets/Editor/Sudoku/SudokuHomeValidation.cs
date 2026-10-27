#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    public static class SudokuHomeValidation
    {
        private const string Cards = "Center/Layout/Content/";
        private const string Sheet = "Panel/Bottom/bg/";

        public static async UniTask RunAsync(string output, CancellationToken token)
        {
            var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
            Require(home != null, "startup opens UIHome");
            await home.WaitForContentAsync(token);
            var content = home.FindChild("Panel/Content");
            var page = content.Find("UIHome_Home");
            Require(page != null, "one asynchronous home content instance");
            int pageId = page.GetInstanceID();
            var game = SudokuPresenter.Current;
            game.DebugResetProgress(); game.DebugSetProps(2, 345); game.SetSuspended(false);
            Require(!game.IsWindowActive, "home stops gameplay");
            Require(page.Find("Top/CoinRoot/Btn/m_num").GetComponent<TMP_Text>().text == "345", "coin event updates home");
            double seconds = game.ElapsedSeconds; game.Tick(5);
            Require(game.ElapsedSeconds == seconds, "home stops elapsed time");
            for (int i = 0; i < 4; i++)
            {
                string[] tabs = { "Home", "DailyChallenge", "Statistics", "Setting" };
                home.FindChildComponent<Button>("Panel/Bottom/Layout/" + tabs[i] + "/root").onClick.Invoke();
                await home.WaitForContentAsync(token);
                Require(home.SelectedTab == i && content.Find("UIHome_Home").GetInstanceID() == pageId && page.gameObject.activeSelf == (i == 0 || i == 2), "tabs reuse separate cached content");
                if (i == 1) Require(home.DailyPage != null && home.DailyPage.Visible, "daily tab displays UIHome_DC");
                if (i == 3) Require(content.Find("UIHome_Setting").gameObject.activeSelf, "settings tab displays UIHome_Setting");
                SudokuThemeManager.SetTheme(i);
                for (int j = 0; j < 4; j++)
                {
                    Color color = home.FindChildComponent<Image>("Panel/Bottom/Layout/" + tabs[j] + "/root/icon").color;
                    Color expected = SudokuThemeManager.GetColor(i == j ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
                    Require(Vector4.Distance(color, expected) < 0.01f, "selected tab follows current palette");
                }
            }
            home.SelectTab(0); SudokuThemeManager.SetTheme(0);
            var originalLanguage = GameModule.Localization.Language;
            GameModule.Localization.Language = Language.English;
            Require(page.Find(Cards + "NewGameBtn/m_text").GetComponent<TMP_Text>().text == "New Game", "home English labels");
            SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(output, "home-english.png"));
            GameModule.Localization.Language = Language.ChineseSimplified;
            SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(output, "home-classic.png"));
            var popup = await OpenPopup(home, page, token);
            Require(popup.FindChild(Sheet + "Layout").childCount == 7, "six difficulty clones plus hidden template");
            Require(!popup.FindChild(Sheet + "Layout/NewGameSelectDiffItem").gameObject.activeSelf, "hidden difficulty template");
            for (int i = 0; i < 6; i++)
            {
                var item = popup.FindChild(Sheet + "Layout/NewGameSelectDiffItem" + i);
                Require(item.Find("bg/Layout").childCount == 3, "two progress clones plus hidden template");
                Require(item.Find("bg").GetComponent<Button>().interactable == (i == 0), "locked difficulty cannot be selected");
            }
            GameModule.Localization.Language = Language.English;
            Require(popup.FindChildComponent<TMP_Text>(Sheet + "m_title").text == "Choose Difficulty", "difficulty English title");
            Require(!popup.FindChildComponent<TMP_Text>(Sheet + "m_des").text.Contains("{0}"), "difficulty placeholders formatted");
            SudokuDemoValidation.CaptureCanvas(popup.gameObject, Path.Combine(output, "difficulty-english.png"));
            GameModule.Localization.Language = Language.ChineseSimplified;
            SudokuDemoValidation.CaptureCanvas(popup.gameObject, Path.Combine(output, "difficulty-classic.png"));
            int revision = game.LevelRevision;
            var bottom = popup.FindChildComponent<RectTransform>("Panel/Bottom");
            Vector2 shown = bottom.anchoredPosition;
            float maskAlpha = popup.gameObject.GetComponent<Image>().color.a;
            Require(maskAlpha > 0, "sheet mask fades in");
            popup.FindChildComponent<Button>("Panel/Close_Btn").onClick.Invoke();
            Require(popup.IsOpen && popup.IsAnimating, "close uses framework out animation");
            await UniTask.WaitUntil(() => !popup.IsOpen, cancellationToken: token);
            Require(bottom.anchoredPosition.y < shown.y && popup.gameObject.GetComponent<Image>().color.a == 0, "sheet slides down and clears mask");
            Require(game.LevelRevision == revision, "closing selection preserves current game");
            popup = await OpenPopup(home, page, token);
            Require((popup.FindChildComponent<RectTransform>("Panel/Bottom").anchoredPosition - shown).sqrMagnitude < 0.1f, "cached sheet returns to authored position");
            GameModule.UI.HideUI<UIHomeNewGame>();
            await UniTask.WaitUntil(() => !popup.IsOpen, cancellationToken: token);
            page.Find(Cards + "ContinueGameBtn").GetComponent<Button>().onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            var ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
            Require(ui != null && game.IsWindowActive && !home.IsOpen, "continue opens ready board and closes home");
            game.Tick(4);
            revision = game.LevelRevision;
            seconds = game.ElapsedSeconds;
            ui.FindChildComponent<Button>("Panel/Layout/Top/BackBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
            Require(home != null && !game.IsWindowActive && !ui.IsOpen, "back returns home and stops gameplay");
            Require(home.FindChild("Panel/Content/UIHome_Home").GetInstanceID() == pageId, "cached home content reused");
            Require(game.LevelRevision == revision && game.ElapsedSeconds >= seconds, "back preserves puzzle and time");
            await SudokuPresenter.ContinueGameAsync();
            game.DebugComplete(); game.NextLevel(); game.DebugComplete();
            Require(game.Session.IsUnlocked(SudokuDifficulty.Beginner), "two distinct puzzles unlock beginner");
            await SudokuPresenter.ShowHomeAsync();
            page = home.FindChild("Panel/Content/UIHome_Home");
            popup = await OpenPopup(home, page, token);
            var quick = popup.FindChild(Sheet + "Layout/NewGameSelectDiffItem0/bg/Layout");
            Require(quick.Find("ProgressItem1").GetComponent<UIThemeColor>().Role == SudokuThemeRole.Accent &&
                quick.Find("ProgressItem2").GetComponent<UIThemeColor>().Role == SudokuThemeRole.Accent, "distinct wins light both progress points");
            popup.FindChildComponent<Button>(Sheet + "Layout/NewGameSelectDiffItem1/bg").onClick.Invoke();
            Require(popup.SelectedDifficulty == SudokuDifficulty.Beginner, "difficulty button selection");
            popup.FindChildComponent<Button>(Sheet + "NewGameBtn").onClick.Invoke();
            await UniTask.WaitUntil(() => !SudokuPresenter.IsNavigating, cancellationToken: token);
            Require(game.Session.Difficulty == SudokuDifficulty.Beginner && game.Session.LevelIndex == 0 && !popup.IsOpen, "start uses shared pool cursor and closes sheet");
            await SudokuPresenter.ShowHomeAsync();
            popup = await OpenPopup(home, page, token);
            Require(popup.FindChild(Sheet + "Layout").childCount == 7, "cached popup does not duplicate items");
            SudokuThemeManager.SetTheme(3);
            Require(popup.FindChildComponent<Image>(Sheet + "NewGameBtn/bg").color == SudokuThemeManager.GetColor(SudokuThemeRole.Accent), "sheet follows theme");
            GameModule.UI.HideUI<UIHomeNewGame>();
            await UniTask.WaitUntil(() => !popup.IsOpen, cancellationToken: token);
            await SudokuPresenter.ContinueGameAsync();
            GameModule.Localization.Language = originalLanguage;
            File.WriteAllText(Path.Combine(output, "home-validation.txt"), "PASS: Home startup, separate cached home/daily pages, tabs/palette, coins, stopped timer, localization, six difficulties, two progress points, locking, sheet animations, cancel, continue/back and shared-pool new game.\n");
        }

        private static async UniTask<UIHomeNewGame> OpenPopup(UIHome home, Transform page, CancellationToken token)
        {
            page.Find(Cards + "NewGameBtn").GetComponent<Button>().onClick.Invoke();
            var popup = await GameModule.UI.GetUIAsyncAwait<UIHomeNewGame>(token);
            Require(popup != null, "new game opens difficulty sheet");
            await UniTask.WaitUntil(() => !popup.IsAnimating, cancellationToken: token);
            return popup;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Home validation failed: " + message);
        }
    }
}
#endif

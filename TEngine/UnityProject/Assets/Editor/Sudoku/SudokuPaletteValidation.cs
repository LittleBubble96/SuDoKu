#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Data;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    [InitializeOnLoad]
    public static class SudokuPaletteValidation
    {
        private const string Pending = "Sudoku.Palette.Pending", Phase = "Sudoku.Palette.Phase",
            Output = "Sudoku.Palette.Output", CachePath = "Sudoku.Palette.CachePath",
            HadCache = "Sudoku.Palette.HadCache", Result = "Sudoku.Palette.Result";
        private const string Layout = "Center/Layout/";

        static SudokuPaletteValidation() { EditorApplication.update += Tick; }

        [MenuItem("Tools/数独/验证调色盘界面")]
        public static void Run() => RunToDirectory(Path.GetFullPath("Library/SudokuPaletteValidation"));

        public static void RunToDirectory(string output)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式运行调色盘验证。");
            Directory.CreateDirectory(output);
            string cache = Path.Combine(Application.persistentDataPath, "game_cache.json");
            SessionState.SetString(Output, output); SessionState.SetString(CachePath, cache);
            SessionState.SetBool(HadCache, File.Exists(cache));
            if (File.Exists(cache)) File.Copy(cache, Path.Combine(output, "cache-before-test.json"), true);
            SessionState.SetBool(Pending, true); SessionState.SetInt(Phase, 0);
            SudokuDemoTools.PlayForValidation();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            int phase = SessionState.GetInt(Phase, 0);
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(phase == 2 ? SessionState.GetString(Result, "No result.") : "FAIL: play mode stopped before validation.");
                return;
            }
            if (phase != 0 || !EditorApplication.isPlaying || SudokuPresenter.Current == null) return;
            SessionState.SetInt(Phase, 1);
            ValidateAsync().Forget();
        }

        private static async UniTaskVoid ValidateAsync()
        {
            string result;
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
            {
                try
                {
                    Application.runInBackground = true;
                    var token = timeout.Token;
                    var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                    await home.WaitForContentAsync(token); home.SelectTab(0);
                    await home.WaitForContentAsync(token);
                    SudokuSettingsManager.SetLanguage("Ch"); SudokuThemeManager.SetTheme(0);
                    var homeButton = home.FindChildComponent<Button>("Panel/Content/UIHome_Home/Top/Right/Layout/ColorTheme/Btn");
                    homeButton.onClick.Invoke(); homeButton.onClick.Invoke();
                    var popup = await PaletteAsync(token);
                    Require(!SudokuPresenter.Current.IsWindowActive, "home palette keeps timer inactive");
                    CheckItems(popup);
                    for (int i = 0; i < SudokuThemeManager.ThemeCount; i++)
                    {
                        popup.FindChildComponent<Button>("Panel/Dialog/Layout/ColorItem" + i).onClick.Invoke();
                        Require(SudokuThemeManager.CurrentThemeId == i, "theme selected by real item button");
                        CheckItems(popup);
                        Require(Distance(home.FindChildComponent<Image>("Panel/Content/UIHome_Home/Top/Right/Layout/ColorTheme/Btn/icon").color,
                            SudokuThemeManager.GetAccent(i)) < .01f, "home recolors immediately");
                    }
                    SudokuDemoValidation.CaptureCanvas(popup.gameObject, Path.Combine(OutputPath, "palette-ch.png"));
                    SudokuSettingsManager.SetLanguage("En");
                    Require(popup.FindChildComponent<TMP_Text>("Panel/Dialog/m_title").text == "Theme colors", "English title");
                    Require(popup.FindChildComponent<TMP_Text>("Panel/Dialog/Layout/ColorItem0/m_name").text == "Classic light", "English option name");
                    SudokuDemoValidation.CaptureCanvas(popup.gameObject, Path.Combine(OutputPath, "palette-en.png"));
                    popup.FindChildComponent<Button>("Panel/Dialog/CloseBtn").onClick.Invoke();
                    Require(!popup.IsOpen && !SudokuPresenter.Current.IsWindowActive, "X closes home palette");
                    homeButton.onClick.Invoke();
                    var reopened = await PaletteAsync(token);
                    Require(ReferenceEquals(popup,reopened), "cached palette reused"); CheckItems(reopened);
                    reopened.FindChildComponent<Button>("Panel/Close_Btn").onClick.Invoke();
                    Require(!reopened.IsOpen, "mask closes palette");
                    SudokuSettingsManager.SetLanguage("Ch");
                    await SudokuPresenter.StartNewGameAsync(SudokuDifficulty.Quick);
                    var ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
                    var game = SudokuPresenter.Current; game.SetSuspended(false);
                    var gameButton = ui.FindChildComponent<Button>("Panel/Layout/Top/ColorBtn");
                    gameButton.onClick.Invoke(); popup = await PaletteAsync(token);
                    Require(!game.IsWindowActive && !game.CanInteract, "game palette blocks board input");
                    double elapsed = game.ElapsedSeconds; int frame = Time.frameCount;
                    await UniTask.Delay(180, ignoreTimeScale:true, cancellationToken:token);
                    Require(Time.frameCount > frame && game.ElapsedSeconds == elapsed, "palette stops timer while loop advances");
                    popup.FindChildComponent<Button>("Panel/Dialog/Layout/ColorItem1").onClick.Invoke();
                    Require(Distance(ui.FindChildComponent<Image>("Panel/Layout/Top/ColorBtn/Icon").color,
                        SudokuThemeManager.GetAccent(1)) < .01f, "game recolors immediately");
                    // A repeated request must not overwrite the state restored on close.
                    await SudokuPresenter.ShowPaletteAsync(ui);
                    popup.FindChildComponent<Button>("Panel/Dialog/CloseBtn").onClick.Invoke();
                    Require(!popup.IsOpen && game.IsWindowActive && !game.IsPaused, "close resumes active game");
                    game.TogglePause(); gameButton.onClick.Invoke(); popup = await PaletteAsync(token);
                    popup.FindChildComponent<Button>("Panel/Close_Btn").onClick.Invoke();
                    Require(game.IsWindowActive && game.IsPaused, "pre-existing pause preserved");
                    game.TogglePause(); gameButton.onClick.Invoke(); popup = await PaletteAsync(token);
                    await SudokuPresenter.ShowHomeAsync();
                    Require(!popup.IsOpen && !game.IsWindowActive, "owner navigation closes palette");
                    await SudokuPresenter.Cache.FlushAsync();
                    var reloaded = new GameCacheManager(); await reloaded.LoadAsync();
                    Require(reloaded.CacheData.gameSettingsData.themeId == 1, "selected theme persists in native cache");
                    result = "PASS: home/game palette entries, six fixed swatches, selected outline/check, immediate recolor, Chinese/English, both close buttons, cached reuse, duplicate requests, timer/input/pause, owner close, theme cache.\nVerified: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception error) { result = error.ToString(); }
            }
            SessionState.SetString(Result, result); SessionState.SetInt(Phase, 2);
            EditorApplication.isPlaying = false;
        }

        private static string OutputPath => SessionState.GetString(Output, "");

        private static async UniTask<UIPalette> PaletteAsync(CancellationToken token)
        {
            var popup = await GameModule.UI.GetUIAsyncAwait<UIPalette>(token);
            await UniTask.WaitUntil(() => popup.IsPrepare && !UIInteractionLock.IsLocked, cancellationToken:token);
            return popup;
        }

        private static void CheckItems(UIPalette popup)
        {
            var layout = popup.FindChild("Panel/Dialog/Layout");
            Require(layout.childCount == 7 && !layout.Find("ColorItem").gameObject.activeSelf, "one hidden template and six instances");
            for (int i = 0; i < 6; i++)
            {
                var item = layout.Find("ColorItem" + i);
                bool selected = i == SudokuThemeManager.CurrentThemeId;
                Require(item.Find("SelectOutline").gameObject.activeSelf == selected && item.Find("Check").gameObject.activeSelf == selected, "selection markers");
                Require(Distance(item.Find("Color").GetComponent<Image>().color, SudokuThemeManager.GetAccent(i)) < .01f, "fixed swatch color");
                Require(item.GetComponent<Button>() != null && !string.IsNullOrEmpty(item.Find("m_name").GetComponent<TMP_Text>().text), "clickable localized item");
            }
        }

        private static float Distance(Color a, Color b) => Vector3.Distance(new Vector3(a.r,a.g,a.b),new Vector3(b.r,b.g,b.b));
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("FAIL: " + message); }
        private static void Finish(string result)
        {
            string cache = SessionState.GetString(CachePath, "");
            if (SessionState.GetBool(HadCache, false)) File.Copy(Path.Combine(OutputPath,"cache-before-test.json"),cache,true);
            else if (File.Exists(cache)) File.Delete(cache);
            File.WriteAllText(Path.Combine(OutputPath,"palette-validation.txt"),result);
            SessionState.EraseBool(Pending);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
        }
    }
}
#endif


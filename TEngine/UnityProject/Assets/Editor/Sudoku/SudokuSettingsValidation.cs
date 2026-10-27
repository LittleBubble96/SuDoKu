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
    public static class SudokuSettingsValidation
    {
        private const string Pending = "Sudoku.Settings.Pending", Phase = "Sudoku.Settings.Phase",
            Output = "Sudoku.Settings.Output", CachePath = "Sudoku.Settings.CachePath",
            HadCache = "Sudoku.Settings.HadCache", Result = "Sudoku.Settings.Result";
        private const string Layout = "Center/Layout/";

        static SudokuSettingsValidation() { EditorApplication.update += Tick; }

        [MenuItem("Tools/数独/验证设置界面")]
        public static void Run() => RunToDirectory(Path.GetFullPath("Library/SudokuSettingsValidation"));

        public static void RunToDirectory(string output)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式运行设置验证。");
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
                    await CheckLegacyCacheAsync();
                    var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                    await home.WaitForContentAsync(token);
                    home.SelectTab(3); await home.WaitForContentAsync(token);
                    var page = home.FindChild("Panel/Content/UIHome_Setting");
                    Require(page != null && page.gameObject.activeSelf && home.SelectedTab == 3, "home settings tab");
                    var settings = SudokuSettingsManager.Data;
                    SudokuSettingsManager.SetLanguage("Ch");
                    var sound = page.Find(Layout + "GameExperience/SoundToggleItem/toggleBtn").GetComponent<Button>();
                    bool soundBefore = settings.SoundVolume > 0;
                    sound.onClick.Invoke();
                    Require((settings.SoundVolume > 0) != soundBefore && Mathf.Abs(GameModule.Audio.SoundVolume - settings.SoundVolume) < .0002f, "sound toggle applies audio");
                    sound.onClick.Invoke();
                    Click(page, "GameExperience/VibrationToggleItem");
                    Click(page, "GameAssist/SameNumHighlightToggleItem");
                    Click(page, "GameAssist/SameNoteHighlightToggleItem");
                    Click(page, "GameAssist/relatedAreaToggleItem");
                    Click(page, "GameAssist/CompletedDigitToggleItem");
                    Click(page, "GameAssist/RemainingCountToggleItem");
                    SudokuSettingsManager.SetHighlightSameNumber(true);
                    SudokuSettingsManager.SetHighlightSameNote(true);
                    SudokuSettingsManager.SetHighlightRelated(true);
                    SudokuSettingsManager.SetDimCompletedDigits(true);
                    SudokuSettingsManager.SetShowRemainingCount(false);
                    var dropdown = page.Find(Layout + "GameOther/LanguageItem/dropdown").GetComponent<TMP_Dropdown>();
                    dropdown.value = 1;
                    Require(settings.language == "En" && page.Find("Top/Center/m_title").GetComponent<TMP_Text>().text == "Settings", "language dropdown and labels");
                    SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(OutputPath, "settings-home-en.png"));
                    dropdown.value = 0;
                    SudokuThemeManager.SetTheme(1);
                    Require(ColorDistance(page.Find(Layout + "GameExperience/SoundToggleItem/icon").GetComponent<Image>().color,
                        SudokuThemeManager.GetColor(SudokuThemeRole.Accent)) < .01f, "settings icons follow palette");
                    SudokuDemoValidation.CaptureCanvas(home.gameObject, Path.Combine(OutputPath, "settings-home-mint.png"));
                    home.SelectTab(0); home.SelectTab(1); home.SelectTab(3);
                    await home.WaitForContentAsync(token);
                    Require(page.gameObject.activeSelf && home.SelectedTab == 3, "rapid page switching settles on settings");
                    page.Find("Top/Back/Btn").GetComponent<Button>().onClick.Invoke();
                    Require(home.SelectedTab == 0, "home settings back selects home");

                    SudokuThemeManager.SetTheme(0);
                    await SudokuPresenter.StartNewGameAsync(SudokuDifficulty.Quick);
                    var ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>(token);
                    var game = SudokuPresenter.Current;
                    game.SetSuspended(false);
                    var session = game.Session;
                    var pack = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_quick.bytes"));
                    var puzzle = pack.GetPuzzle(session.LevelIndex);
                    SudokuSolver.Solve(puzzle, out var solution);
                    int given = -1, same = -1, empty = -1;
                    for (int i = 0; i < 81 && same < 0; i++)
                        if (session.Value(i) != 0)
                            for (int j = i + 1; j < 81; j++)
                                if (session.Value(i) == session.Value(j)) { given = i; same = j; break; }
                    for (int i = 0; i < 81; i++) if (session.Value(i) == 0) { empty = i; break; }
                    int digit = session.Value(given);
                    game.EnterDigit(empty, digit, true); game.SelectCell(given);
                    var board = ui.FindChild("Panel/Layout/Board/m_rect_Board");
                    Require(board.Find("Cell" + same).GetComponent<UIThemeColor>().Role == SudokuThemeRole.Matching, "matching digit highlight on");
                    SudokuSettingsManager.SetHighlightSameNumber(false);
                    Require(board.Find("Cell" + same).GetComponent<UIThemeColor>().Role != SudokuThemeRole.Matching, "matching digit highlight off");
                    Require(board.Find("Cell" + empty + "/m_tmp_Notes").GetComponent<TMP_Text>().text.Contains("<color="), "matching note highlight on");
                    SudokuSettingsManager.SetHighlightSameNote(false);
                    Require(!board.Find("Cell" + empty + "/m_tmp_Notes").GetComponent<TMP_Text>().text.Contains("<color="), "matching note highlight off");
                    SudokuSettingsManager.SetHighlightSameNote(true); SudokuThemeManager.SetTheme(3);
                    Require(board.Find("Cell" + empty + "/m_tmp_Notes").GetComponent<TMP_Text>().text.Contains(ColorUtility.ToHtmlStringRGB(
                        SudokuThemeManager.GetColor(SudokuThemeRole.Accent))), "note highlight recolors with theme");
                    SudokuSettingsManager.SetHighlightRelated(false);
                    for (int i = 0; i < 81; i++) if (session.Value(i) == 0)
                        Require(board.Find("Cell" + i).GetComponent<UIThemeColor>().Role == SudokuThemeRole.Surface, "related highlight off");
                    SudokuSettingsManager.SetHighlightRelated(true);
                    int remaining = session.RemainingCount(digit);
                    game.EnterDigit(empty, digit, false);
                    Require(session.RemainingCount(digit) == remaining - 1, "remaining count updates after input");
                    game.Undo(); Require(session.RemainingCount(digit) == remaining, "remaining count restores after undo");
                    SudokuSettingsManager.SetShowRemainingCount(true);
                    Require(ui.FindChild("Panel/Layout/Bottom/Layout/Num1/m_remaining") == null, "optional count node is not generated");
                    for (int i = 0; i < 81; i++) if (!session.IsGiven(i) && solution[i] == 1) game.EnterDigit(i, 1, false);
                    Require(session.RemainingCount(1) == 0 && ui.FindChildComponent<TMP_Text>("Panel/Layout/Bottom/Layout/Num1/Bg/m_num")
                        .GetComponent<UIThemeColor>().Role == SudokuThemeRole.MutedText, "completed digit dims");
                    SudokuSettingsManager.SetDimCompletedDigits(false);
                    Require(ui.FindChildComponent<TMP_Text>("Panel/Layout/Bottom/Layout/Num1/Bg/m_num").GetComponent<UIThemeColor>().Role ==
                        SudokuThemeRole.Accent, "completed digit dimming can be disabled");
                    SudokuThemeManager.SetTheme(0);
                    ui.FindChildComponent<Button>("Panel/Layout/Top/SettingBtn").onClick.Invoke();
                    var popup = await GameModule.UI.GetUIAsyncAwait<UISetting>(token);
                    await UniTask.WaitUntil(() => popup.IsPrepare && !UIInteractionLock.IsLocked, cancellationToken: token);
                    double elapsed = game.ElapsedSeconds; int frame = Time.frameCount;
                    await UniTask.Delay(180, ignoreTimeScale:true, cancellationToken:token);
                    Require(Time.frameCount > frame && !game.IsWindowActive && game.ElapsedSeconds == elapsed, "settings stops timer while player loop advances");
                    SudokuDemoValidation.CaptureCanvas(popup.gameObject, Path.Combine(OutputPath, "settings-ingame.png"));
                    popup.FindChildComponent<Button>("Panel/Content/UIHome_Setting/Top/Back/Btn").onClick.Invoke();
                    Require(!popup.IsOpen && game.IsWindowActive && !game.IsPaused, "close restores active gameplay");
                    game.TogglePause();
                    ui.FindChildComponent<Button>("Panel/Layout/Top/SettingBtn").onClick.Invoke();
                    popup = await GameModule.UI.GetUIAsyncAwait<UISetting>(token);
                    await UniTask.WaitUntil(() => !UIInteractionLock.IsLocked, cancellationToken:token);
                    popup.FindChildComponent<Button>("Panel/Content/UIHome_Setting/Top/Back/Btn").onClick.Invoke();
                    Require(game.IsWindowActive && game.IsPaused, "close preserves pre-existing pause");
                    await SudokuPresenter.Cache.FlushAsync();
                    var reloaded = new GameCacheManager(); await reloaded.LoadAsync();
                    Require(JsonUtility.ToJson(reloaded.CacheData.gameSettingsData) == JsonUtility.ToJson(settings), "native settings cache roundtrip");
                    result = "PASS: home/in-game settings, seven toggles, language, palette, highlights, notes, remaining counts, dimming, timer/pause, legacy/native cache.\nVerified: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception error) { result = error.ToString(); }
            }
            SessionState.SetString(Result, result); SessionState.SetInt(Phase, 2);
            EditorApplication.isPlaying = false;
        }

        private static string OutputPath => SessionState.GetString(Output, "");

        private sealed class MemoryFileSystem : IReadCacheFileSystem
        {
            public string Json;
            public UniTask<string> ReadCacheAsync() => UniTask.FromResult(Json);
            public UniTask WriteCacheAsync(string json) { Json = json; return UniTask.CompletedTask; }
            public void FlushOnQuit(string json) => Json = json;
        }

        private static async UniTask CheckLegacyCacheAsync()
        {
            var memory = new MemoryFileSystem { Json = "{\"version\":1,\"gameSettingsData\":{\"themeId\":2,\"MusicVolume\":0.5,\"SoundVolume\":0.25}}" };
            var manager = new GameCacheManager(memory); await manager.LoadAsync();
            var data = manager.CacheData.gameSettingsData;
            Require(data.highlightSameNumber && data.highlightSameNote && data.highlightRelated && data.dimCompletedDigits &&
                !data.vibration && !data.showRemainingCount && data.language == "Ch" && data.themeId == 2 && data.SoundVolume == .25f, "legacy settings keep defaults");
            data.highlightSameNumber = false; data.showRemainingCount = true; await manager.FlushAsync();
            manager = new GameCacheManager(memory); await manager.LoadAsync();
            Require(!manager.CacheData.gameSettingsData.highlightSameNumber && manager.CacheData.gameSettingsData.showRemainingCount, "explicit false survives cache reload");
        }

        private static float ColorDistance(Color a, Color b) => Vector3.Distance(new Vector3(a.r,a.g,a.b),new Vector3(b.r,b.g,b.b));
        private static void Click(Transform root, string path) => root.Find(Layout + path + "/toggleBtn").GetComponent<Button>().onClick.Invoke();
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("FAIL: " + message); }
        private static void Finish(string result)
        {
            string cache = SessionState.GetString(CachePath, "");
            if (SessionState.GetBool(HadCache, false)) File.Copy(Path.Combine(OutputPath,"cache-before-test.json"),cache,true);
            else if (File.Exists(cache)) File.Delete(cache);
            File.WriteAllText(Path.Combine(OutputPath,"settings-validation.txt"),result);
            SessionState.EraseBool(Pending);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
        }
    }
}
#endif


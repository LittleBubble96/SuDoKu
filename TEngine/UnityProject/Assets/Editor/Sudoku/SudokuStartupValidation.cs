#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Data;
using GameLogic.Sudoku;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    [InitializeOnLoad]
    public static class SudokuStartupValidation
    {
        private const string Pending = "Sudoku.Integration.Pending", Phase = "Sudoku.Integration.Phase", Output = "Sudoku.Integration.Output", CachePath = "Sudoku.Integration.CachePath", HadCache = "Sudoku.Integration.HadCache", Result = "Sudoku.Integration.Result";
        private static double _started;
        static SudokuStartupValidation() { EditorApplication.update += Tick; }

        public static void Run()
        {
            RunToDirectory(Path.GetFullPath("../Tests/Sudoku/Integration"));
        }

        public static void RunToDirectory(string output)
        {
            Directory.CreateDirectory(output);
            string cache = Path.Combine(Application.persistentDataPath, "game_cache.json");
            SessionState.SetString(Output, output); SessionState.SetString(CachePath, cache);
            SessionState.SetBool(HadCache, File.Exists(cache));
            if (File.Exists(cache)) File.Copy(cache, Path.Combine(output, "cache-before-test.json"), true);
            SessionState.SetBool(Pending, true); SessionState.SetInt(Phase, 0); _started = EditorApplication.timeSinceStartup;
            try { SudokuDemoTools.PlayForValidation(); }
            catch (Exception error) { Finish(error.ToString()); }
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            int phase = SessionState.GetInt(Phase, 0);
            if (phase != 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish("ERROR: validation stopped before completion."); return;
            }
            if (phase == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(SessionState.GetString(Result, "ERROR: no result")); return;
            }
            if (phase != 0 || !EditorApplication.isPlaying || SudokuPresenter.Current == null || GameObject.Find("UIShowGM") == null) return;
            SessionState.SetInt(Phase, 1);
            ValidateAsync().Forget();
        }
        private static async UniTaskVoid ValidateAsync()
        {
            string result;
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                await SudokuTrophyUiValidation.RunAsync(SessionState.GetString(Output, ""), timeout.Token);
                await SudokuDailyUiValidation.RunAsync(SessionState.GetString(Output, ""), timeout.Token);
                await SudokuHomeValidation.RunAsync(SessionState.GetString(Output, ""), timeout.Token);
                var ui = await GameModule.UI.GetUIAsyncAwait<UISudoku>();
                Require(ui != null && ui.IsBoardReady, "real asynchronous UI startup / board readiness");
                var game = SudokuPresenter.Current;
                game.DebugResetProgress(); game.DebugSetProps(2, 0); game.SetSuspended(false);
                var board = Board(ui);
                Require(board.childCount == 102, "81 cells, 20 lines, one hidden template");
                Require(!board.Find("SudoCell").gameObject.activeSelf, "hidden template");
                Require(ui.FindChild("Panel/Layout/Bottom/Layout").childCount == 9, "Num1 cloned through Num9");
                CheckLayout(board);
                var pack = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_quick.bytes"));
                byte[] puzzle = pack.GetPuzzle(0); SudokuSolver.Solve(puzzle, out var answer);
                int empty = Array.IndexOf(puzzle, (byte)0);
                board.Find("Cell" + empty).GetComponent<Button>().onClick.Invoke();
                Require(game.SelectedCell == empty, "cell click binding");
                Button(ui, "PropArea/NoteBtn").onClick.Invoke(); Number(ui, 1).onClick.Invoke();
                Require(game.NoteMode && game.Session.Notes(empty) == 2 && game.Session.Value(empty) == 0, "note button / number binding");
                Button(ui, "PropArea/UnDoBtn").onClick.Invoke();
                Require(game.Session.Notes(empty) == 0, "undo notes");
                Button(ui, "PropArea/NoteBtn").onClick.Invoke();
                Number(ui, answer[empty]).onClick.Invoke();
                Require(game.Session.Value(empty) == answer[empty], "digit click binding");
                Button(ui, "PropArea/ClearBtn").onClick.Invoke(); Require(game.Session.Value(empty) == 0, "erase binding");
                Button(ui, "PropArea/TipsBtn").onClick.Invoke();
                Require(game.Props.tipCount == 1 && game.Session.Value(empty) == answer[empty], "hint inventory payment");
                Button(ui, "PropArea/UnDoBtn").onClick.Invoke();
                game.DebugSetProps(0, 100);
                Require(ui.FindChild("Panel/Layout/PropArea/TipsBtn/Sign/m_coin").gameObject.activeSelf, "coin badge");
                Button(ui, "PropArea/TipsBtn").onClick.Invoke();
                Require(game.Props.coinCount == 0 && game.Session.Value(empty) == answer[empty], "hint coin payment");
                Button(ui, "PropArea/UnDoBtn").onClick.Invoke(); game.DebugSetProps(0, 0);
                Require(AdSystem.IsAdAvailable(AdIds.Tip), "Editor SDK ad preloaded");
                Require(ui.FindChild("Panel/Layout/PropArea/TipsBtn/Sign/m_Ad/Ready").gameObject.activeSelf, "ready ad badge");
                Button(ui, "PropArea/TipsBtn").onClick.Invoke();
                await UniTask.WaitUntil(() => !game.IsAdShowing, cancellationToken: timeout.Token);
                Require(game.Session.Value(empty) == answer[empty] && game.Props.coinCount == 0 && game.Props.tipCount == 0, "completed ad grants exactly one free hint");
                int revision = game.LevelRevision; game.Restart();
                Require(!game.TryUseHint(true, revision), "stale ad result cannot affect restarted puzzle");
                game.Tick(5);
                Button(ui, "Top/StopBtn").onClick.Invoke(); double elapsed = game.ElapsedSeconds;
                game.Tick(3); Number(ui, 1).onClick.Invoke();
                Require(game.IsPaused && game.ElapsedSeconds == elapsed && game.Session.Value(empty) == 0, "paused input / timer blocked");
                Require(!board.Find("Cell0").gameObject.activeSelf, "pause hides board values");
                Button(ui, "Top/StopBtn").onClick.Invoke(); game.Tick(1);
                Require(!game.IsPaused && game.ElapsedSeconds > elapsed, "resume timer");
                SudokuThemeManager.SetTheme(0);
                SudokuDemoValidation.CaptureCanvas(ui.gameObject, Path.Combine(SessionState.GetString(Output, ""), "ui-classic.png"));
                var icon = ui.FindChildComponent<Image>("Panel/Layout/Top/ColorBtn/Icon"); Color classic = icon.color;
                SudokuThemeManager.SetTheme(3);
                Require(icon.color != classic && icon.color == SudokuThemeManager.GetColor(SudokuThemeRole.Accent), "theme subscription refreshes existing images");
                game.SelectCell(empty); game.ToggleNotes(); Number(ui, 2).onClick.Invoke();
                await SudokuPresenter.Cache.FlushAsync();
                var reloaded = new GameCacheManager(); await reloaded.LoadAsync();
                var restored = new SudokuGameController(LoadPacks(), reloaded);
                Require(restored.SelectedCell == empty && restored.NoteMode && restored.Session.Notes(empty) == 4, "local file restore selection / notes / mode");
                Require(restored.ElapsedSeconds >= 6 && reloaded.CacheData.gameSettingsData.themeId == 3 && restored.Props.tipCount == 0, "local file restore timer / theme / props");
                game.Restart();
                Complete(ui, puzzle); Require(game.Session.CompletedCount(SudokuDifficulty.Quick) == 1 && !game.Session.IsUnlocked(SudokuDifficulty.Beginner), "one distinct win stays locked");
                game.Restart(); Complete(ui, puzzle); Require(game.Session.CompletedCount(SudokuDifficulty.Quick) == 1, "replaying same puzzle adds no unlock credit");
                game.NextLevel(); Complete(ui, pack.GetPuzzle(1)); Require(game.Session.IsUnlocked(SudokuDifficulty.Beginner), "two distinct wins unlock next tier");
                await SudokuPresenter.Cache.FlushAsync();
                reloaded = new GameCacheManager(); await reloaded.LoadAsync();
                restored = new SudokuGameController(LoadPacks(), reloaded);
                Require(restored.Session.IsComplete && restored.Session.IsUnlocked(SudokuDifficulty.Beginner), "completed game and unlock survive reload");
                game.OpenLevel(SudokuDifficulty.Beginner, 0);
                Require(ui.FindChildComponent<TMP_Text>("Panel/Layout/Board/SudoInfoPanel/Level").text == SudokuUIFormatting.Difficulty(SudokuDifficulty.Beginner), "difficulty label has no numbered stage");
                SudokuDemoValidation.CaptureCanvas(ui.gameObject, Path.Combine(SessionState.GetString(Output, ""), "ui-peach.png"));
                GMSingle.Instance.TogglePanel();
                var gm = await GameModule.UI.GetUIAsyncAwait<UIGM>();
                await UniTask.WaitUntil(() => gm.IsReady, cancellationToken: timeout.Token);
                gm.FindChildComponent<Button>("Panel/CloseBtn").onClick.Invoke();
                int first = Board(ui).Find("Cell0").GetInstanceID();
                GameModule.UI.CloseUI<UISudoku>();
                ui = await GameModule.UI.ShowUIAsyncAwait<UISudoku>(new SudokuContext(game));
                Require(Board(ui).Find("Cell0").GetInstanceID() == first && ui.FindChild("Panel/Layout/Bottom/Layout").childCount == 9, "cached reopen duplicates neither board nor keypad");
                // 在广告展示前关闭窗口：等待中的旧回调不能发奖励。
                game.DebugSetProps(0, 0);
                await UniTask.WaitUntil(() => AdSystem.IsAdAvailable(AdIds.Tip), cancellationToken: timeout.Token);
                int before = Filled(game.Session);
                Button(ui, "PropArea/TipsBtn").onClick.Invoke(); GameModule.UI.CloseUI<UISudoku>();
                await UniTask.DelayFrame(2);
                ui = await GameModule.UI.ShowUIAsyncAwait<UISudoku>(new SudokuContext(game));
                Require(Filled(game.Session) == before && !game.IsAdShowing && !UIInteractionLock.IsLocked, "close cancels ad request without late reward or input lock leak");
                GameModule.UI.DestroyUI<UISudoku>();
                ui = await GameModule.UI.ShowUIAsyncAwait<UISudoku>(new SudokuContext(game));
                Require(ui.IsBoardReady && Board(ui).childCount == 102 && ui.FindChild("Panel/Layout/Bottom/Layout").childCount == 9, "destroy / recreate bindings");
                await SudokuCompletionValidation.ValidateAsync(ui, game, timeout.Token);
                await TestCacheQueueAsync(timeout.Token);
                await SudokuPresenter.Cache.FlushAsync();
                result = "PASS: real startup; existing hierarchy bound; Num1 generates 1-9; 81 cells and dynamic lines; keypad/note/erase/undo; inventory/coin/ad hint payments; pause input/timer; theme subscriptions; local JSON reload; distinct-win unlock persistence; stale/closed ad guards; GM initialization; cache reopen and destroy/recreate; ordered/coalesced async cache writes; completion ripple (row/column/box/combined), theme/selection updates and lifecycle cancellation. Player cache restored after test.\n";
            }
            catch (Exception error) { result = "ERROR: " + error; Debug.LogException(error); }
            finally { timeout.Dispose(); }
            SessionState.SetString(Result, result); SessionState.SetInt(Phase, 2);
            EditorApplication.isPlaying = false;
        }
        private sealed class MemoryFileSystem : IReadCacheFileSystem
        {
            public string Json;
            public int Writes;
            public UniTask<string> ReadCacheAsync() => UniTask.FromResult(Json);
            public async UniTask WriteCacheAsync(string json) { await UniTask.DelayFrame(2); Json = json; Writes++; }
            public void FlushOnQuit(string json) => Json = json;
        }
        private static async UniTask TestCacheQueueAsync(CancellationToken token)
        {
            var storage = new MemoryFileSystem(); var manager = new GameCacheManager(storage); await manager.LoadAsync();
            for (int i = 1; i <= 20; i++) { manager.CacheData.gamePropData.coinCount = i; manager.Save(); }
            await manager.FlushAsync();
            var loaded = new GameCacheManager(storage); await loaded.LoadAsync();
            Require(loaded.CacheData.gamePropData.coinCount == 20 && storage.Writes <= 3, "serialized writes retain latest snapshot");
            storage.Json = "bad json"; await loaded.LoadAsync();
            Require(loaded.CacheData.gamePropData != null, "malformed cache fallback");
        }
        private static SudokuPack[] LoadPacks()
        {
            var packs = new SudokuPack[6];
            for (int d = 0; d < 6; d++) packs[d] = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes"));
            return packs;
        }
        private static Transform Board(UISudoku ui) => ui.FindChild("Panel/Layout/Board/m_rect_Board");
        private static Button Button(UISudoku ui, string path) => ui.FindChildComponent<Button>("Panel/Layout/" + path);
        private static Button Number(UISudoku ui, int digit) => Button(ui, "Bottom/Layout/Num" + digit);
        private static int Filled(SudokuSession session) { int count = 0; for (int i = 0; i < 81; i++) if (session.Value(i) != 0) count++; return count; }
        private static void Complete(UISudoku ui, byte[] puzzle)
        {
            SudokuSolver.Solve(puzzle, out var answer);
            for (int i = 0; i < 81; i++) if (!ui.Session.IsGiven(i))
            {
                Board(ui).Find("Cell" + i).GetComponent<Button>().onClick.Invoke(); Number(ui, answer[i]).onClick.Invoke();
            }
            Require(ui.Session.IsComplete, "UI button playthrough");
        }
        private static void CheckLayout(Transform board)
        {
            var size = ((RectTransform)board).rect.size;
            Require(Vector2.Distance(size, new Vector2(990, 990)) < .01f, "authored 990x990 board retained");
            Vector2 unit = size / 9f;
            for (int i = 0; i < 81; i++)
            {
                var cell = (RectTransform)board.Find("Cell" + i);
                Require(Vector2.Distance(cell.rect.size, unit) < .01f && Vector2.Distance(cell.anchoredPosition, new Vector2(i % 9 * unit.x, -(i / 9) * unit.y)) < .01f, "dynamic cell geometry");
            }
            for (int i = 0; i < 10; i++)
            {
                var v = (RectTransform)board.Find("Vertical" + i); var h = (RectTransform)board.Find("Horizontal" + i);
                Require(Mathf.Abs(v.rect.height - size.y) < .01f && Mathf.Abs(h.rect.width - size.x) < .01f, "dynamic grid lengths");
                Require(!v.GetComponent<Image>().raycastTarget && !h.GetComponent<Image>().raycastTarget, "grid does not block input");
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("FAIL: " + message); }
        private static void Finish(string result)
        {
            string output = SessionState.GetString(Output, "");
            string cache = SessionState.GetString(CachePath, "");
            if (!string.IsNullOrEmpty(cache))
            {
                if (SessionState.GetBool(HadCache, false)) File.Copy(Path.Combine(output, "cache-before-test.json"), cache, true);
                else if (File.Exists(cache)) File.Delete(cache);
            }
            File.WriteAllText(Path.Combine(output, "startup-validation.txt"), result);
            SessionState.EraseBool(Pending);
            if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
        }
    }
}
#endif

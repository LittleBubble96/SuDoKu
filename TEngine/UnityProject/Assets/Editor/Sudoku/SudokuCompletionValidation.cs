#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    /// <summary>通过实际 UISudoku 和按钮检查完成动画，随启动集成验证一起运行。</summary>
    public static class SudokuCompletionValidation
    {
        public static async UniTask ValidateAsync(UISudoku ui, SudokuGameController game, CancellationToken token)
        {
            var report = new StringBuilder();
            var all = SudokuCompletedRegion.Row | SudokuCompletedRegion.Column | SudokuCompletedRegion.Box;
            foreach (var regions in new[] { SudokuCompletedRegion.Row, SudokuCompletedRegion.Column, SudokuCompletedRegion.Box,
                SudokuCompletedRegion.Row | SudokuCompletedRegion.Column, all })
            {
                int origin = Prepare(ui, game, regions, out var answer);
                var firstChanged = new float[81];
                for (int i = 0; i < 81; i++) firstChanged[i] = -1;
                int frame = Time.frameCount;
                Require(game.EnterDigit(origin, answer[origin], false), "normal digit accepted");
                Require(game.Session.CompletedRegions(origin) == regions, "only fixture regions completed");
                float started = Time.unscaledTime;
                while (Time.unscaledTime - started < 2)
                {
                    await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, token);
                    for (int i = 0; i < 81; i++)
                        if (!IsRestored(ui, i) && firstChanged[i] < 0) firstChanged[i] = Time.unscaledTime - started;
                }
                Require(Time.frameCount > frame + 10, "real player frames advance");
                Require(firstChanged[origin] >= 0, "input cell flashes");
                for (int i = 0; i < 81; i++)
                {
                    int step = SudokuCompletion.WaveStep(origin, i, regions);
                    Require((step >= 0) == (firstChanged[i] >= 0), "only completed union flashes, including givens");
                    // 传播间隔可小于一个采样帧；允许同帧观察到，禁止外圈先于起点。
                    if (step > 0) Require(firstChanged[i] >= firstChanged[origin], "source flashes no later than outward cells");
                    if (step > 1)
                    {
                        bool earlierNeighbour = false;
                        foreach (int neighbour in new[] { i - 9, i + 9, i % 9 > 0 ? i - 1 : -1, i % 9 < 8 ? i + 1 : -1 })
                            if (neighbour >= 0 && neighbour < 81 && SudokuCompletion.WaveStep(origin, neighbour, regions) == step - 1 &&
                                firstChanged[neighbour] >= 0 && firstChanged[neighbour] <= firstChanged[i]) earlierNeighbour = true;
                        Require(earlierNeighbour, "rings propagate in order");
                    }
                }
                AssertRestored(ui);
                report.AppendLine("PASS: " + regions + " origin=" + origin + "; source-first ripple; union only; base colors restored.");
            }

            int cell = Prepare(ui, game, all, out var solution);
            game.EnterDigit(cell, solution[cell] % 9 + 1, false);
            await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, token);
            AssertRestored(ui);
            game.EnterDigit(cell, 0, false);
            game.EnterDigit(cell, solution[cell], true);
            await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, token);
            AssertRestored(ui);
            Require(game.Session.Value(cell) == 0, "notes do not fill the cell");

            game.EnterDigit(cell, solution[cell], false);
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            game.SelectCell((cell + 1) % 81);
            SudokuThemeManager.SetTheme(1);
            await UniTask.Delay(2000, ignoreTimeScale: true, cancellationToken: token);
            AssertRestored(ui);
            Require(Cell(ui, cell).GetComponent<UIThemeColor>().Role != SudokuThemeRole.Selected, "selection changes during animation remain current");

            cell = Prepare(ui, game, all, out solution);
            game.EnterDigit(cell, solution[cell], false);
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            game.Undo();
            AssertRestored(ui);
            Require(game.Session.Value(cell) == 0, "undo cancels invalidated region wave");

            cell = Prepare(ui, game, all, out solution);
            game.EnterDigit(cell, solution[cell], false);
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            game.TogglePause();
            AssertRestored(ui);
            game.TogglePause();
            AssertRestored(ui);

            cell = Prepare(ui, game, all, out solution);
            game.EnterDigit(cell, solution[cell], false);
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            game.Restart();
            AssertRestored(ui);

            cell = Prepare(ui, game, all, out solution);
            game.DebugSetProps(2, 0);
            Require(game.TryUseHint(false, game.LevelRevision), "hint accepted");
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            Require(!IsRestored(ui, cell), "hint completion animates actual hint cell");
            GameModule.UI.CloseUI<UISudoku>();
            AssertRestored(ui);
            ui = await GameModule.UI.ShowUIAsyncAwait<UISudoku>(new SudokuContext(game));
            AssertRestored(ui);
            await UniTask.Delay(250, ignoreTimeScale: true, cancellationToken: token);
            AssertRestored(ui);

            cell = Prepare(ui, game, all, out solution);
            for (int i = 0; i < 81; i++)
                if (!game.Session.IsGiven(i) && i != cell) game.EnterDigit(i, solution[i], false);
            game.SelectCell(cell);
            game.EnterDigit(cell, solution[cell], false);
            Require(game.Session.IsComplete, "whole puzzle completed");
            await UniTask.Delay(30, ignoreTimeScale: true, cancellationToken: token);
            Require(!IsRestored(ui, cell), "final winning digit still animates");
            await UniTask.Delay(2000, ignoreTimeScale: true, cancellationToken: token);
            AssertRestored(ui);
            game.DebugResetProgress();
            SudokuThemeManager.SetTheme(0);
            report.AppendLine("PASS: wrong digit / notes silent; selection + theme change; undo / pause / restart / close cleanup; hints and final winning digit.");
            File.WriteAllText("../Tests/Sudoku/CompletionRipple/play-validation.txt", report.ToString());
        }

        private static int Prepare(UISudoku ui, SudokuGameController game, SudokuCompletedRegion regions, out byte[] answer)
        {
            game.DebugUnlockAll();
            Require(game.OpenLevel(SudokuDifficulty.Master, 0), "open sparse fixture");
            var pack = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_master.bytes"));
            byte[] puzzle = pack.GetPuzzle(0); SudokuSolver.Solve(puzzle, out answer);
            for (int origin = 0; origin < 81; origin++)
            {
                if (puzzle[origin] != 0) continue;
                var board = (byte[])puzzle.Clone();
                for (int i = 0; i < 81; i++)
                    if (SudokuCompletion.WaveStep(origin, i, regions) >= 0) board[i] = answer[i];
                if (SudokuCompletion.GetCompletedRegions(board, answer, origin) != regions) continue;
                board[origin] = 0;
                var save = game.Session.Capture();
                for (int i = 0; i < 81; i++) save.values[i] = board[i];
                Require(game.Session.TryRestore(save), "restore fixture");
                game.SelectCell(origin);
                AssertRestored(ui);
                return origin;
            }
            throw new Exception("No isolated fixture for " + regions);
        }
        private static UnityEngine.UI.Image Cell(UISudoku ui, int index) =>
            ui.FindChildComponent<UnityEngine.UI.Image>("Panel/Layout/Board/m_rect_Board/Cell" + index);
        private static bool IsRestored(UISudoku ui, int index)
        {
            var image = Cell(ui, index);
            Color expected = SudokuThemeManager.GetColor(image.GetComponent<UIThemeColor>().Role);
            return Mathf.Abs(image.color.r - expected.r) < .001f && Mathf.Abs(image.color.g - expected.g) < .001f &&
                Mathf.Abs(image.color.b - expected.b) < .001f;
        }
        private static void AssertRestored(UISudoku ui)
        {
            for (int i = 0; i < 81; i++) Require(IsRestored(ui, i), "cell " + i + " restored to current theme and role");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception("Completion animation: " + message); }
    }
}
#endif

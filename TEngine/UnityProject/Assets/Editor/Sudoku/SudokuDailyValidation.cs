#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Data;
using GameLogic.Sudoku;
using UnityEditor;
using UnityEngine;

namespace SudokuEditor
{
    /// <summary>隔离的内存存档回归，不读写玩家存档。</summary>
    public static class SudokuDailyValidation
    {
        private static readonly StringBuilder Checks = new StringBuilder();

        [MenuItem("Tools/数独/验证每日挑战规则")]
        public static void RunMenu() => Debug.Log(RunCore());

        public static string RunCore()
        {
            Checks.Clear();
            var packs = LoadPacks();
            var first = SudokuDailyRules.FirstDate;
            for (int i = 0; i < 18; i++) Require((int)SudokuDailyRules.Difficulty(first.AddDays(i)) == i % 6, "date rotation " + i);
            Require(!SudokuDailyRules.CanStart(first.AddDays(-1), first), "2025 rejected");
            Require(SudokuDailyRules.CanStart(first, first), "2026 first day allowed");
            Require(!SudokuDailyRules.CanStart(first.AddDays(1), first), "future rejected");
            Require(SudokuDailyRules.Difficulty(new DateTime(2026, 1, 31)) == SudokuDifficulty.Quick &&
                SudokuDailyRules.Difficulty(new DateTime(2026, 2, 1)) == SudokuDifficulty.Beginner, "rotation continues across month boundary");
            Require(SudokuDailyRules.Difficulty(new DateTime(2026, 12, 31)) == SudokuDifficulty.Expert &&
                SudokuDailyRules.Difficulty(new DateTime(2027, 1, 1)) == SudokuDifficulty.Master, "rotation continues across year boundary");
            Require(SudokuDailyRules.Difficulty(new DateTime(2028, 2, 29)) == SudokuDifficulty.Advanced &&
                SudokuDailyRules.Difficulty(new DateTime(2028, 3, 1)) == SudokuDifficulty.Expert, "leap day included in rotation");
            Require(SudokuDailyRules.MondayOffset(new DateTime(2026, 8, 1)) == 5, "Monday calendar offset");
            Require(SudokuDailyRules.MondayOffset(new DateTime(2026, 3, 1)) + 31 > 35, "six calendar rows required");

            DateTime now = new DateTime(2026, 10, 24, 23, 59, 0);
            var storage = new MemoryStorage();
            var cache = NewCache(storage);
            var game = new SudokuGameController(packs, cache, () => now);
            Require(!game.HasNormalGame && game.PeekNextIndex(SudokuDifficulty.Quick) == 0, "startup does not allocate a puzzle");
            game.SaveGameProgress();
            Require(cache.CacheData.sudokuSaveData == null, "startup save does not create a normal game");
            Require(!game.OpenDailyChallenge(first.AddDays(-1)) && !game.OpenDailyChallenge(now.Date.AddDays(1)), "invalid dates do not open");
            Require(cache.CacheData.dailyChallenges.Count == 0 && game.PeekNextIndex(SudokuDifficulty.Quick) == 0, "invalid dates do not consume pool");

            Require(game.StartNewGame(SudokuDifficulty.Quick) && game.Session.LevelIndex == 0, "first normal puzzle from shared pool");
            var normal = game.Session.Capture();
            var answer = Solve(normal);
            var empty = Empty(normal);
            game.SelectCell(empty[0]);
            Require(game.EnterDigit(empty[0], answer[empty[0]], false), "normal input");
            game.ToggleNotes();
            Require(game.EnterDigit(empty[1], answer[empty[1]], true), "normal notes");
            game.Tick(7); game.SaveGameProgress();
            string normalBefore = JsonUtility.ToJson(cache.CacheData.sudokuSaveData);

            Require(game.OpenDailyChallenge(first) && game.Session.LevelIndex == 1, "daily consumes next shared quick puzzle");
            Require(game.GetDaily(first) != null && game.GetDaily(first).progressPercent == 0, "entering daily immediately creates record");
            Require(JsonUtility.ToJson(cache.CacheData.sudokuSaveData) == normalBefore, "daily does not overwrite normal save");
            Require(game.PeekNextIndex(SudokuDifficulty.Quick) == 2, "shared cursor advanced once");
            int dailyRevision = game.LevelRevision;
            Require(game.OpenDailyChallenge(first) && game.LevelRevision == dailyRevision && game.PeekNextIndex(SudokuDifficulty.Quick) == 2, "same active day is idempotent");
            var daily = game.Session.Capture();
            answer = Solve(daily); empty = Empty(daily);
            int wrong = answer[empty[0]] % 9 + 1;
            Require(game.EnterDigit(empty[0], wrong, false) && game.GetDaily(first).progressPercent == 0, "wrong values do not count as progress");
            Require(game.EnterDigit(empty[0], answer[empty[0]], false) && game.GetDaily(first).progressPercent > 0, "correct empty cells count as progress");
            game.SelectCell(empty[1]); game.ToggleNotes(); game.EnterDigit(empty[1], answer[empty[1]], true); game.Tick(11); game.SaveGameProgress();
            string dailyBefore = JsonUtility.ToJson(game.GetDaily(first).game);
            Require(game.OpenDailyChallenge(first.AddDays(6)) && game.Session.LevelIndex == 2, "second date has independent puzzle");
            Require(game.ContinueNormalGame(), "normal restore from daily");
            Require(JsonUtility.ToJson(cache.CacheData.sudokuSaveData) == normalBefore && game.ElapsedSeconds == 7 && game.NoteMode, "normal values, notes, time restored");
            Require(game.OpenDailyChallenge(first), "restore earlier daily");
            Require(JsonUtility.ToJson(game.GetDaily(first).game) == dailyBefore && game.ElapsedSeconds == 11 && game.NoteMode, "daily values, notes, time restored");
            Require(game.PeekNextIndex(SudokuDifficulty.Quick) == 3, "restores do not consume pool");

            var reloadedCache = NewCache(storage);
            var reloaded = new SudokuGameController(packs, reloadedCache, () => now);
            Require(reloaded.IsDaily && reloaded.DailyDate == first && reloaded.ElapsedSeconds == 11 && reloaded.NoteMode, "JSON reload restores active daily");
            Require(reloaded.HasNormalGame && reloaded.NormalElapsedSeconds == 7 && reloaded.PeekNextIndex(SudokuDifficulty.Quick) == 3, "JSON reload retains normal and cursor");
            now = now.AddMinutes(2);
            Require(reloaded.Today.Day == 25 && reloaded.DailyDate == first, "midnight does not move active challenge date");
            int poolBefore = reloaded.PeekNextIndex(SudokuDifficulty.Quick);
            reloaded.Restart();
            Require(reloaded.Session.LevelIndex == 1 && reloaded.PeekNextIndex(SudokuDifficulty.Quick) == poolBefore && reloaded.GetDaily(first).progressPercent == 0, "restart keeps puzzle and cursor");
            reloaded.DebugComplete();
            Require(reloaded.GetDaily(first).completed && reloaded.GetDaily(first).progressPercent == 100 && reloaded.CompletedDays(first) == 1, "daily completion recorded for original month");
            Require(reloaded.Session.CompletedCount(SudokuDifficulty.Quick) == 0 && !reloaded.Session.IsUnlocked(SudokuDifficulty.Beginner), "daily wins do not unlock normal difficulty");
            Require(!reloaded.OpenDailyChallenge(first) && reloaded.PeekNextIndex(SudokuDifficulty.Quick) == poolBefore, "completed date cannot replay or consume pool");
            reloaded.Restart();
            Require(reloaded.Session.IsComplete && reloaded.CompletedDays(first) == 1, "completed daily cannot restart");
            Require(reloaded.OpenDailyChallenge(first.AddDays(5)) && reloaded.Session.Difficulty == SudokuDifficulty.Master, "daily bypasses normal difficulty lock");
            reloaded.DebugComplete();
            Require(reloaded.Session.CompletedCount(SudokuDifficulty.Master) == 0 && !reloaded.Session.IsUnlocked(SudokuDifficulty.Master), "master daily remains separate from normal wins");
            Require(reloaded.StartNewGame(SudokuDifficulty.Quick), "normal new game after daily completion");
            reloaded.DebugComplete(); reloaded.NextLevel(); reloaded.DebugComplete();
            Require(reloaded.Session.CompletedCount(SudokuDifficulty.Quick) == 2 && reloaded.Session.IsUnlocked(SudokuDifficulty.Beginner), "two different normal wins still unlock beginner");
            Require(reloaded.StartNewGame(SudokuDifficulty.Beginner), "ordinary continue fixture uses non-default difficulty");
            reloaded.Tick(23); reloaded.DebugComplete();
            int nextBeginner = reloaded.PeekNextIndex(SudokuDifficulty.Beginner);
            var completedReload = new SudokuGameController(packs, NewCache(storage), () => now);
            Require(completedReload.NormalIsComplete && completedReload.NormalDifficulty == SudokuDifficulty.Beginner && completedReload.NormalElapsedSeconds == 23,
                "completed normal save retains difficulty and original time on reload");
            Require(completedReload.ContinueNormalGame() && !completedReload.IsDaily && !completedReload.Session.IsComplete &&
                completedReload.Session.Difficulty == SudokuDifficulty.Beginner && completedReload.Session.LevelIndex == nextBeginner && completedReload.ElapsedSeconds == 0,
                "completed normal continue starts next puzzle at same difficulty with zero time");
            Require(completedReload.PeekNextIndex(SudokuDifficulty.Beginner) == (nextBeginner + 1) % packs[1].Count, "completed normal continue advances pool exactly once");
            var continued = completedReload.Session.Capture();
            int continuedCell = Empty(continued)[0];
            byte[] continuedAnswer = Solve(continued);
            Require(completedReload.EnterDigit(continuedCell, continuedAnswer[continuedCell], false), "continued normal accepts input");
            completedReload.Tick(9);
            int continuedRevision = completedReload.LevelRevision;
            int continuedCursor = completedReload.PeekNextIndex(SudokuDifficulty.Beginner);
            Require(completedReload.ContinueNormalGame() && completedReload.Session.Value(continuedCell) == continuedAnswer[continuedCell] && completedReload.ElapsedSeconds == 9 &&
                completedReload.LevelRevision == continuedRevision && completedReload.PeekNextIndex(SudokuDifficulty.Beginner) == continuedCursor,
                "incomplete normal continue preserves puzzle input time and cursor");
            completedReload.DebugComplete();
            Require(completedReload.OpenDailyChallenge(first.AddDays(4)) && completedReload.Session.Difficulty == SudokuDifficulty.Expert,
                "daily difficulty differs from completed normal fixture");
            Require(completedReload.ContinueNormalGame() && !completedReload.IsDaily && completedReload.Session.Difficulty == SudokuDifficulty.Beginner &&
                completedReload.Session.LevelIndex == continuedCursor && completedReload.ElapsedSeconds == 0,
                "continue from daily uses completed normal difficulty instead of daily difficulty");
            Require(!completedReload.OpenDailyChallenge(first) && completedReload.GetDaily(first).completed, "completed daily remains locked after normal continue");
            int cursor = reloaded.PeekNextIndex(SudokuDifficulty.Quick);
            for (int i = 0; i <= packs[0].Count; i++)
            {
                Require(reloaded.StartNewGame(SudokuDifficulty.Quick) && reloaded.Session.LevelIndex == cursor, "pool sequence " + i);
                cursor = (cursor + 1) % packs[0].Count;
                Require(reloaded.PeekNextIndex(SudokuDifficulty.Quick) == cursor, "pool wrap " + i);
            }

            var legacy = new SudokuSession(packs); legacy.BeginLevel(SudokuDifficulty.Quick, 7);
            var legacyData = new GameCacheData { sudokuSaveData = legacy.Capture() };
            legacyData.sudokuSaveData.elapsedSeconds = 19;
            var legacyStorage = new MemoryStorage { Json = JsonUtility.ToJson(legacyData) };
            var migrated = new SudokuGameController(packs, NewCache(legacyStorage), () => now);
            Require(migrated.HasNormalGame && migrated.Session.LevelIndex == 7 && migrated.ElapsedSeconds == 19 && migrated.PeekNextIndex(SudokuDifficulty.Quick) == 8, "legacy save migration preserves current puzzle");
            Require(migrated.StartNewGame(SudokuDifficulty.Quick) && migrated.Session.LevelIndex == 8, "legacy migration starts next puzzle");

            // 历史日期题号可超过新版题库长度，仍使用缓存的原始题面。
            var historicalDate = first.AddDays(12);
            Require(migrated.OpenDailyChallenge(historicalDate), "historical puzzle allocated");
            var historical = migrated.Session.Capture();
            var updatedPacks = new SudokuPack[6];
            for (int d = 0; d < 6; d++) updatedPacks[d] = SudokuPack.Read(SudokuPack.Write((SudokuDifficulty)d, new List<byte[]> { packs[d].GetPuzzle(1), packs[d].GetPuzzle(0) }));
            var updatedCache = NewCache(legacyStorage);
            var updated = new SudokuGameController(updatedPacks, updatedCache, () => now);
            Require(updated.IsDaily && updated.Session.LevelIndex == historical.puzzleIndex && historical.puzzleIndex >= 2, "historical daily restores despite changed pool size");
            Require(Equal(updated.Session.Capture().puzzle, historical.puzzle), "historical original puzzle retained");
            Require(updated.PeekNextIndex(SudokuDifficulty.Quick) == 0, "changed pool signature resets cursor");
            var corruptDate = first.AddDays(18);
            var invalid = new SudokuDailySaveData { date = SudokuDailyRules.DateKey(corruptDate), game = historical };
            invalid.game.values = new int[1];
            updatedCache.CacheData.dailyChallenges.Add(invalid); updatedCache.Save();
            var corrupt = new SudokuGameController(updatedPacks, NewCache(legacyStorage), () => now);
            int beforeCorrupt = corrupt.PeekNextIndex(SudokuDifficulty.Quick);
            Require(!corrupt.OpenDailyChallenge(corruptDate) && corrupt.PeekNextIndex(SudokuDifficulty.Quick) == beforeCorrupt, "invalid restore does not consume a puzzle");

            return "PASS: " + Checks.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length + " daily checks\n" + Checks;
        }

        private static GameCacheManager NewCache(MemoryStorage storage)
        {
            var cache = new GameCacheManager(storage);
            cache.LoadAsync().GetAwaiter().GetResult();
            return cache;
        }

        private static SudokuPack[] LoadPacks()
        {
            var packs = new SudokuPack[6];
            for (int d = 0; d < 6; d++) packs[d] = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes"));
            return packs;
        }

        private static byte[] Solve(SudokuSaveData data)
        {
            var puzzle = new byte[81]; for (int i = 0; i < 81; i++) puzzle[i] = (byte)data.puzzle[i];
            Require(SudokuSolver.Solve(puzzle, out var answer) == 1, "fixture unique solution");
            return answer;
        }

        private static List<int> Empty(SudokuSaveData data)
        {
            var result = new List<int>(); for (int i = 0; i < 81; i++) if (data.puzzle[i] == 0) result.Add(i);
            return result;
        }

        private static bool Equal(int[] left, int[] right)
        {
            if (left?.Length != right?.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Daily validation failed: " + label);
            Checks.AppendLine("PASS " + label);
        }

        private sealed class MemoryStorage : IReadCacheFileSystem
        {
            public string Json;
            public UniTask<string> ReadCacheAsync() => UniTask.FromResult(Json);
            public UniTask WriteCacheAsync(string json) { Json = json; return UniTask.CompletedTask; }
            public void FlushOnQuit(string json) { Json = json; }
        }
    }
}
#endif

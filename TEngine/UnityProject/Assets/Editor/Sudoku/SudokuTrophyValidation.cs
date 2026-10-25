#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using GameConfig.trophy;
using GameLogic;
using GameLogic.Data;
using GameLogic.Sudoku;
using Luban;
using UnityEditor;
using UnityEngine;

namespace SudokuEditor
{
    public static class SudokuTrophyValidation
    {
        private static readonly StringBuilder Checks = new StringBuilder();
        [MenuItem("Tools/数独/验证奖杯管理器")]
        public static void RunMenu() => Debug.Log(RunCore());

        public static string RunCore()
        {
            Checks.Clear(); var packs = LoadPacks();
            var table = new TbTrophy(new ByteBuf(File.ReadAllBytes("Assets/AssetRaw/Configs/bytes/trophy_tbtrophy.bytes")));
            Require(table.DataList.Count == 12, "Luban loads twelve configured 2026 trophies");
            ConfigSystem.Instance.LoadEditor(); var languages = ConfigSystem.Instance.Tables.TbLanguageContent;
            foreach (var config in table.DataList)
            {
                Require(config.Id == 202600 + config.Month && config.Year == 2026 && config.UnlockValue == 0 && config.UnlockType == ETrophyUnlockType.AllDailyChallengesInMonth, "monthly configuration " + config.Month);
                Require(languages.GetOrDefault(config.NameKey) != null && languages.GetOrDefault(config.DescriptionKey) != null, "localized configuration " + config.Month);
                Require(File.Exists("Assets/AssetRaw/UIRaw/Raw/UI/DailyChallenge/" + config.IconObtained + ".png") && File.Exists("Assets/AssetRaw/UIRaw/Raw/UI/DailyChallenge/" + config.IconNotObtained + ".png"), "both icon assets " + config.Month);
            }
            DateTime now = new DateTime(2026, 10, 25);
            var storage = new MemoryStorage(); var cache = NewCache(storage);
            var game = new SudokuGameController(packs, cache, () => now, table.DataList);
            Require(game.Trophies.Years.Count == 1 && game.Trophies.Years[0] == 2026 && game.Trophies.GetForYear(2026).Count == 12, "year catalog from configuration");
            Require(game.Trophies.GetForYear(2027).Count == 0 && game.Trophies.Get(999) == null && game.Trophies.GetForMonth(2025, 13) == null, "missing and invalid catalog queries");
            Require(game.Trophies.GetForMonth(2026, 1).State == SudokuTrophyState.NotObtained && game.Trophies.GetForMonth(2026, 10).State == SudokuTrophyState.InProgress &&
                game.Trophies.GetForMonth(2026, 11).State == SudokuTrophyState.NotStarted, "past current and future status");
            Require(game.Trophies.GetForMonth(2026, 1).RequiredDays == 31 && game.Trophies.GetForMonth(2026, 2).RequiredDays == 28 && game.Trophies.GetForMonth(2026, 4).RequiredDays == 30, "actual month lengths 28 30 31");
            Require(game.Trophies.GetForMonth(2026, 10).Icon == table.Get(202610).IconNotObtained && game.Trophies.CollectedCount(2026) == 0, "unearned icon and empty collection");
            game.StartNewGame(SudokuDifficulty.Quick); game.DebugComplete();
            Require(game.Trophies.CollectedCount(2026) == 0, "normal win cannot unlock monthly trophy");
            AddDays(cache.CacheData, 2026, 2, 27);
            game.SaveGameProgress();
            Require(game.Trophies.Get(202602).CompletedDays == 27 && !game.Trophies.Get(202602).IsObtained, "one missing daily date prevents full-month award");
            Require(game.OpenDailyChallenge(new DateTime(2026, 2, 28)), "make-up final February date opens through controller");
            game.DebugComplete();
            Require(game.Trophies.Get(202602).IsObtained && game.Trophies.Get(202602).CompletedDays == 28 && game.Trophies.Get(202602).AwardedDate == "2026-10-25", "real daily completion retroactively awards past month");
            Require(game.Trophies.Get(202602).Icon == table.Get(202602).IconObtained && game.Trophies.CollectedCount(2026) == 1, "earned icon and yearly count");
            game.SaveGameProgress(); game.SaveGameProgress();
            Require(cache.CacheData.trophyAwards.Count == 1, "repeat saves do not grant duplicate trophy");
            var restoredCache = NewCache(storage); var restored = new SudokuGameController(packs, restoredCache, () => now, table.DataList);
            Require(restored.Trophies.Get(202602).IsObtained && restored.Trophies.Get(202602).AwardedDate == "2026-10-25", "JSON reload retains awarded trophy and date");
            restoredCache.CacheData.dailyChallenges.Clear(); restored.SaveGameProgress();
            Require(restored.Trophies.Get(202602).IsObtained, "earned trophy stays earned when historical daily data is removed");

            // 旧版本只有每日记录，登录时补发；重复日期、无效日期和未来日期不计数。
            var legacyStorage = new MemoryStorage(); var legacyCache = NewCache(legacyStorage);
            AddDays(legacyCache.CacheData, 2026, 1, 31);
            legacyCache.CacheData.dailyChallenges.Add(new SudokuDailySaveData { date = "2026-01-01", completed = true });
            legacyCache.CacheData.dailyChallenges.Add(new SudokuDailySaveData { date = "invalid", completed = true });
            legacyCache.CacheData.dailyChallenges.Add(new SudokuDailySaveData { date = "2025-12-31", completed = true });
            AddDays(legacyCache.CacheData, 2026, 11, 30);
            legacyCache.Save();
            var legacyGame = new SudokuGameController(packs, NewCache(legacyStorage), () => now, table.DataList);
            Require(legacyGame.Trophies.Get(202601).IsObtained && legacyGame.Trophies.Get(202601).CompletedDays == 31, "legacy full month reconciled and duplicate dates deduplicated");
            Require(!legacyGame.Trophies.Get(202611).IsObtained && legacyGame.Trophies.Get(202611).CompletedDays == 0, "future cached completions cannot unlock trophy");
            Require(new SudokuGameController(packs, NewCache(legacyStorage), () => now, table.DataList).Trophies.CollectedCount(2026) == 1, "legacy backfill persisted without player action");
            legacyGame.DebugResetProgress();
            Require(legacyGame.Trophies.CollectedCount(2026) == 0 && legacyGame.Trophies.Get(202601).CompletedDays == 0, "GM reset clears trophies and daily progress together");

            var leap = Config(202802, 2028, 2); var threshold = Config(202603, 2026, 3, ETrophyUnlockType.DailyChallengeCountInMonth, 3);
            now = new DateTime(2028, 3, 1);
            var customCache = NewCache(new MemoryStorage()); AddDays(customCache.CacheData, 2028, 2, 28); AddDays(customCache.CacheData, 2026, 3, 2);
            var custom = new SudokuGameController(packs, customCache, () => now, new[] { leap, threshold });
            Require(custom.Trophies.Years.Count == 2 && custom.Trophies.Years[0] == 2026 && custom.Trophies.Get(202802).RequiredDays == 29 && !custom.Trophies.Get(202802).IsObtained, "leap February requires 29 and catalog years sort");
            Require(custom.Trophies.Get(202603).RequiredDays == 3 && !custom.Trophies.Get(202603).IsObtained, "configured count condition below threshold");
            customCache.CacheData.dailyChallenges.Add(new SudokuDailySaveData { date = "2028-02-29", completed = true });
            customCache.CacheData.dailyChallenges.Add(new SudokuDailySaveData { date = "2026-03-03", completed = false, progressPercent = 99 }); custom.SaveGameProgress();
            Require(custom.Trophies.Get(202802).IsObtained && !custom.Trophies.Get(202603).IsObtained, "leap day grants trophy but partial daily does not");
            customCache.CacheData.dailyChallenges[customCache.CacheData.dailyChallenges.Count - 1].completed = true; custom.SaveGameProgress();
            Require(custom.Trophies.Get(202603).IsObtained && custom.Trophies.CollectedCount(2026) == 1 && custom.Trophies.CollectedCount(2028) == 1, "count threshold unlock and per-year totals");
            Rejected(() => new SudokuTrophyManager(new[] { leap, leap }, new GameCacheData(), () => now), "duplicate ID rejected");
            Rejected(() => new SudokuTrophyManager(new[] { leap, Config(999, 2028, 2) }, new GameCacheData(), () => now), "duplicate month rejected");
            Rejected(() => new SudokuTrophyManager(new[] { Config(1, 2026, 2, ETrophyUnlockType.DailyChallengeCountInMonth, 29) }, new GameCacheData(), () => now), "impossible required count rejected");
            Rejected(() => new SudokuTrophyManager(new[] { Config(1, 2026, 2, (ETrophyUnlockType)999) }, new GameCacheData(), () => now), "unknown unlock condition rejected");
            return "PASS: " + Checks.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length + " trophy checks\n" + Checks;
        }

        private static ConfTrophy Config(int id, int year, int month, ETrophyUnlockType type = ETrophyUnlockType.AllDailyChallengesInMonth, int value = 0)
        {
            var buffer = new ByteBuf(); buffer.WriteInt(id); buffer.WriteInt(year); buffer.WriteInt(month);
            buffer.WriteString("earned"); buffer.WriteString("locked"); buffer.WriteString("name"); buffer.WriteString("description");
            buffer.WriteInt((int)type); buffer.WriteInt(value); buffer.WriteInt(month); return new ConfTrophy(buffer);
        }
        private static void AddDays(GameCacheData data, int year, int month, int days)
        {
            for (int day = 1; day <= days; day++) data.dailyChallenges.Add(new SudokuDailySaveData { date = SudokuDailyRules.DateKey(new DateTime(year, month, day)), completed = true });
        }
        private static GameCacheManager NewCache(MemoryStorage storage) { var cache = new GameCacheManager(storage); cache.LoadAsync().GetAwaiter().GetResult(); return cache; }
        private static SudokuPack[] LoadPacks()
        {
            var packs = new SudokuPack[6]; for (int d = 0; d < 6; d++) packs[d] = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes")); return packs;
        }
        private static void Rejected(Action action, string label) { try { action(); } catch (ArgumentException) { Require(true, label); return; } throw new Exception("Invalid trophy configuration accepted: " + label); }
        private static void Require(bool condition, string label) { if (!condition) throw new Exception("Trophy validation failed: " + label); Checks.AppendLine("PASS " + label); }
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

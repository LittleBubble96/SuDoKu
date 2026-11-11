using System;
using System.Collections.Generic;
using GameConfig.trophy;
using GameLogic.Data;

namespace GameLogic.Sudoku
{
    public enum SudokuTrophyState { NotObtained, InProgress, NotStarted, Obtained }

    /// <summary>提供状态、原始多语言 Key 和图标资源名，展示层负责翻译与加载资源。</summary>
    public sealed class SudokuTrophyProgress
    {
        public ConfTrophy Config { get; }
        public int CompletedDays { get; }
        public int RequiredDays { get; }
        public SudokuTrophyState State { get; }
        public string AwardedDate { get; }
        public bool IsObtained => State == SudokuTrophyState.Obtained;
        public string Icon => IsObtained ? Config.IconObtained : Config.IconNotObtained;

        internal SudokuTrophyProgress(ConfTrophy config, int completed, int required, SudokuTrophyState state, string awardedDate)
        {
            Config = config; CompletedDays = completed; RequiredDays = required; State = state; AwardedDate = awardedDate;
        }
    }

    /// <summary>从每日挑战记录统计月进度，已获得奖杯按配置 ID 永久保存，不创建或操作 UI。</summary>
    public sealed class SudokuTrophyManager
    {
        private readonly GameCacheData _data;
        private readonly Func<DateTime> _clock;
        private readonly List<ConfTrophy> _configs = new List<ConfTrophy>();
        private readonly Dictionary<int, ConfTrophy> _byId = new Dictionary<int, ConfTrophy>();
        private readonly Dictionary<int, ConfTrophy> _byMonth = new Dictionary<int, ConfTrophy>();
        private readonly Dictionary<int, SudokuTrophyAward> _awards = new Dictionary<int, SudokuTrophyAward>();
        private readonly List<int> _years = new List<int>();
        public IReadOnlyList<int> Years { get; }

        public SudokuTrophyManager(IEnumerable<ConfTrophy> configs, GameCacheData data, Func<DateTime> clock)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _data.InitAll();
            foreach (var config in configs ?? Array.Empty<ConfTrophy>())
            {
                Validate(config);
                if (_byId.ContainsKey(config.Id) || _byMonth.ContainsKey(MonthKey(config.Year, config.Month)))
                    throw new ArgumentException("奖杯 ID 或年月重复: " + config.Id, nameof(configs));
                _configs.Add(config); _byId.Add(config.Id, config); _byMonth.Add(MonthKey(config.Year, config.Month), config);
            }
            _configs.Sort((a, b) => a.Year != b.Year ? a.Year.CompareTo(b.Year) : a.SortOrder != b.SortOrder ? a.SortOrder.CompareTo(b.SortOrder) : a.Month.CompareTo(b.Month));
            foreach (var config in _configs) if (!_years.Contains(config.Year)) _years.Add(config.Year);
            Years = _years.AsReadOnly();
            foreach (var award in _data.trophyAwards)
                if (award != null && award.id > 0 && !_awards.ContainsKey(award.id)) _awards.Add(award.id, award);
        }

        public SudokuTrophyProgress Get(int id)
        {
            var today = _clock().Date;
            return _byId.TryGetValue(id, out var config) ? Progress(config, CompletedByMonth(today), today) : null;
        }

        public SudokuTrophyProgress GetForMonth(int year, int month)
        {
            if (year < SudokuDailyRules.FirstDate.Year || year > 9999 || month < 1 || month > 12) return null;
            return _byMonth.TryGetValue(MonthKey(year, month), out var config) ? Get(config.Id) : null;
        }

        public IReadOnlyList<SudokuTrophyProgress> GetForYear(int year)
        {
            var result = new List<SudokuTrophyProgress>();
            var today = _clock().Date; var counts = CompletedByMonth(today);
            foreach (var config in _configs) if (config.Year == year) result.Add(Progress(config, counts, today));
            return result.AsReadOnly();
        }

        public int CollectedCount(int year)
        {
            int count = 0;
            foreach (var config in _configs) if (config.Year == year && _awards.ContainsKey(config.Id)) count++;
            return count;
        }

        // 控制器在读档后和保存进度时调用，与每日记录在同一次缓存写入中持久化。
        internal bool RefreshAwards(string source = "reconcile", string roundId = null)
        {
            if (_configs.Count == 0) return false;
            var today = _clock().Date; var counts = CompletedByMonth(today); bool changed = false;
            foreach (var config in _configs)
            {
                if (_awards.ContainsKey(config.Id) || new DateTime(config.Year, config.Month, 1) > today) continue;
                if (CompletedDays(config, counts) < RequiredDays(config)) continue;
                GrantAward(config.Id, today);
                var dates = new HashSet<DateTime>();
                foreach (var record in _data.dailyChallenges)
                    if (record?.completed == true &&
                        SudokuDailyRules.TryParseDate(record.date, out var date) && date <= today &&
                        date.Year == config.Year && date.Month == config.Month) dates.Add(date);
                if (dates.Count >= RequiredDays(config))
                {
                    var bi = GameSDK.BiManager.DailyTrophyAwarded;
                    bi.SetData(config.Id,
                        new DateTime(config.Year, config.Month, 1).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                        dates.Count, RequiredDays(config), SudokuDailyRules.DateKey(today), source,
                        source == "challenge_complete" ? roundId : null);
                    bi.Report();
                }
                changed = true;
            }
            return changed;
        }

        private void GrantAward(int id, DateTime today)
        {
            var award = new SudokuTrophyAward { id = id, awardedDate = SudokuDailyRules.DateKey(today) };
            _awards.Add(id, award);
            _data.trophyAwards.Add(award);
        }

#if !TE_RELEASE
        internal void ClearAwards()
        {
            _awards.Clear();
            _data.trophyAwards.Clear();
            _data.trophyProgressOverrides.Clear();
        }

        internal void DebugGrant(int yearMonth)
        {
            var config = GetDebugConfig(yearMonth);
            if (!_awards.ContainsKey(config.Id)) GrantAward(config.Id, _clock().Date);
        }

        internal void DebugSetProgress(int yearMonth, int completedDays)
        {
            var config = GetDebugConfig(yearMonth);
            int required = RequiredDays(config);
            if (completedDays < 0 || completedDays > required)
                throw new ArgumentOutOfRangeException(nameof(completedDays), $"奖杯进度请输入 0～{required} 天。");

            var today = _clock().Date;
            SetProgressOverride(config, completedDays, CompletedByMonth(today));
            if (completedDays == required)
            {
                if (!_awards.ContainsKey(config.Id)) GrantAward(config.Id, today);
            }
            else if (_awards.Remove(config.Id))
            {
                _data.trophyAwards.RemoveAll(award => award?.id == config.Id);
            }
        }

        internal void DebugClear(int? yearMonth)
        {
            if (yearMonth.HasValue)
            {
                DebugSetProgress(yearMonth.Value, 0);
                return;
            }

            ClearAwards();
            // 奖杯清零后仍保留每日记录，避免读档或保存时按原记录补发。
            var counts = CompletedByMonth(_clock().Date);
            foreach (var config in _configs)
                if (counts.ContainsKey(MonthKey(config.Year, config.Month))) SetProgressOverride(config, 0, counts);
        }

        private ConfTrophy GetDebugConfig(int yearMonth) => _byMonth.TryGetValue(yearMonth, out var config)
            ? config : throw new ArgumentException($"{yearMonth} 没有奖杯配置，请输入配置中的年月（如 202611）。");

        private GameCacheData.TrophyProgressOverride GetProgressOverride(int id) =>
            _data.trophyProgressOverrides.Find(entry => entry?.id == id);

        private void SetProgressOverride(ConfTrophy config, int completedDays, Dictionary<int, int> counts)
        {
            var entry = GetProgressOverride(config.Id);
            if (entry == null)
            {
                entry = new GameCacheData.TrophyProgressOverride { id = config.Id };
                _data.trophyProgressOverrides.Add(entry);
            }
            entry.completedDays = completedDays;
            counts.TryGetValue(MonthKey(config.Year, config.Month), out entry.dailyCompletedAtOverride);
        }
#endif

        private int CompletedDays(ConfTrophy config, Dictionary<int, int> counts)
        {
            counts.TryGetValue(MonthKey(config.Year, config.Month), out int completed);
#if !TE_RELEASE
            var progressOverride = GetProgressOverride(config.Id);
            if (progressOverride != null)
            {
                int newCompletions = Math.Max(0, completed - progressOverride.dailyCompletedAtOverride);
                return Math.Min(RequiredDays(config), progressOverride.completedDays + newCompletions);
            }
#endif
            return completed;
        }

        private SudokuTrophyProgress Progress(ConfTrophy config, Dictionary<int, int> counts, DateTime today)
        {
            int completed = CompletedDays(config, counts);
            _awards.TryGetValue(config.Id, out var award);
            var month = new DateTime(config.Year, config.Month, 1);
            var state = award != null ? SudokuTrophyState.Obtained : month > today && completed == 0 ? SudokuTrophyState.NotStarted :
                config.Year == today.Year && config.Month == today.Month ? SudokuTrophyState.InProgress : SudokuTrophyState.NotObtained;
            return new SudokuTrophyProgress(config, completed, RequiredDays(config), state, award?.awardedDate);
        }

        private Dictionary<int, int> CompletedByMonth(DateTime today)
        {
            var counts = new Dictionary<int, int>(); var dates = new HashSet<DateTime>();
            foreach (var record in _data.dailyChallenges)
            {
                if (record == null || !record.completed || !SudokuDailyRules.TryParseDate(record.date, out var date) ||
                    date < SudokuDailyRules.FirstDate || date > today || !dates.Add(date)) continue;
                int key = MonthKey(date.Year, date.Month); counts.TryGetValue(key, out int count); counts[key] = count + 1;
            }
            return counts;
        }

        private static int MonthKey(int year, int month) => year * 100 + month;
        private static int RequiredDays(ConfTrophy config) => config.UnlockType == ETrophyUnlockType.AllDailyChallengesInMonth ? DateTime.DaysInMonth(config.Year, config.Month) : config.UnlockValue;
        private static void Validate(ConfTrophy config)
        {
            if (config == null || config.Id <= 0 || config.Year < SudokuDailyRules.FirstDate.Year || config.Year > 9999 || config.Month < 1 || config.Month > 12)
                throw new ArgumentException("奖杯配置的 ID、年份或月份无效。");
            if (string.IsNullOrWhiteSpace(config.IconObtained) || string.IsNullOrWhiteSpace(config.IconNotObtained) || string.IsNullOrWhiteSpace(config.NameKey) || string.IsNullOrWhiteSpace(config.DescriptionKey))
                throw new ArgumentException("奖杯图标或多语言 Key 为空: " + config.Id);
            bool valid = config.UnlockType == ETrophyUnlockType.AllDailyChallengesInMonth ? config.UnlockValue == 0 :
                config.UnlockType == ETrophyUnlockType.DailyChallengeCountInMonth && config.UnlockValue > 0 && config.UnlockValue <= DateTime.DaysInMonth(config.Year, config.Month);
            if (!valid) throw new ArgumentException("奖杯解锁条件或条件值无效: " + config.Id);
        }
    }
}

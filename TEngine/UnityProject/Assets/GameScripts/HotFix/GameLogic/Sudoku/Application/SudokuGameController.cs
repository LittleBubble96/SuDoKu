using System;
using System.Collections.Generic;
using System.Text;
using TEngine;
using GameLogic.Data;

namespace GameLogic.Sudoku
{
    /// <summary>统一协调玩家操作和 GM 操作；界面通过框架事件刷新已有控件。</summary>
    public sealed class SudokuGameController
    {
        private readonly SudokuPack[] _packs;
        public const int HintCoinCost = 100;
        private readonly GameCacheManager _cache;
        private readonly GameCacheData _data;
        private readonly Func<DateTime> _clock;
        private readonly Dictionary<string, SudokuDailySaveData> _daily = new Dictionary<string, SudokuDailySaveData>();
        private bool _hasActiveGame, _normalValid, _normalComplete;
        public SudokuSession Session { get; private set; }
        public SudokuTrophyManager Trophies { get; }
        public string Message { get; private set; }
        public int LevelRevision { get; private set; }
        public int SelectedCell { get; private set; } = -1;
        public bool NoteMode { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsSuspended { get; private set; }
        public bool IsAdShowing { get; private set; }
        public bool IsWindowActive { get; private set; } = true;
        public bool CanInteract => _hasActiveGame && IsWindowActive && !IsPaused && !IsSuspended && !IsAdShowing && !Session.IsComplete;
        public GamePropData Props { get; }
        public bool IsDaily { get; private set; }
        public DateTime DailyDate { get; private set; }
        public DateTime Today => _clock().Date;
        public bool HasNormalGame => _normalValid;
        public bool NormalIsComplete => _normalComplete;
        public SudokuDifficulty NormalDifficulty => _normalValid ? (SudokuDifficulty)_data.sudokuSaveData.difficulty : SudokuDifficulty.Quick;
        public double NormalElapsedSeconds => _normalValid ? _data.sudokuSaveData.elapsedSeconds : 0;

        public SudokuGameController(SudokuPack[] packs, GameCacheManager cache = null, Func<DateTime> clock = null, IEnumerable<GameConfig.trophy.ConfTrophy> trophyConfigs = null)
        {
            _packs = (SudokuPack[])packs.Clone();
            _cache = cache;
            _data = cache?.CacheData ?? new GameCacheData();
            _data.InitAll();
            _clock = clock ?? (() => NetTimeSystem.Instance.GetNetTime());
            Props = _data.gamePropData;
            Session = new SudokuSession(_packs);
            var saved = _data.sudokuSaveData;
            if (Session.TryRestore(saved))
            {
                _hasActiveGame = _normalValid = true;
                _normalComplete = Session.IsComplete;
                RestorePlayState(saved);
            }
            else Session.BeginLevel(SudokuDifficulty.Quick, 0); // 首页占位，不分配新题、不推进游标。
            InitializePools(saved);
            foreach (var entry in _data.dailyChallenges)
                if (entry != null && SudokuDailyRules.TryParseDate(entry.date, out _) && entry.game != null &&
                    entry.game.difficulty >= 0 && entry.game.difficulty < 6 && entry.game.puzzleIndex >= 0 && !_daily.ContainsKey(entry.date))
                {
                    entry.progressPercent = entry.completed ? 100 : Math.Max(0, Math.Min(99, entry.progressPercent));
                    _daily.Add(entry.date, entry);
                }
            if (SudokuDailyRules.TryParseDate(_data.activeDailyDate, out var activeDate) &&
                _daily.TryGetValue(_data.activeDailyDate, out var active) && !active.completed)
            {
                var candidate = NewSession();
                if (candidate.TryRestoreDaily(active.game))
                {
                    Session = candidate; IsDaily = _hasActiveGame = true; DailyDate = activeDate;
                    RestorePlayState(active.game);
                }
                else _data.activeDailyDate = null;
            }
            if (!IsDaily) _data.activeDailyDate = null;
            LevelRevision = 1;
            Message = "每行、每列、每个九宫格内，数字 1—9 不重复。";
            Trophies = new SudokuTrophyManager(trophyConfigs, _data, () => Today);
            if (Trophies.RefreshAwards()) _cache?.Save();
        }

        public bool OpenLevel(SudokuDifficulty difficulty, int index)
        {
            try
            {
                var candidate = NewSession();
                candidate.BeginLevel(difficulty, index);
                CommitGame(candidate, null, null);
                SetMessage(Session.WasCompleted(difficulty, index) ? "本题已完成过，重玩不会重复累计解锁进度。" : "每行、每列、每个九宫格内，数字 1—9 不重复。");
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                SetMessage(e.Message); return false;
            }
        }

        public int PeekNextIndex(SudokuDifficulty difficulty) => _data.sudokuPools[(int)difficulty].nextIndex;

        public bool StartNewGame(SudokuDifficulty difficulty)
        {
            if (!Session.IsUnlocked(difficulty)) return false;
            int index = PeekNextIndex(difficulty);
            var candidate = NewSession();
            try { candidate.BeginLevel(difficulty, index); }
            catch (Exception error) when (error is ArgumentException || error is InvalidOperationException) { SetMessage(error.Message); return false; }
            CommitGame(candidate, null, null, difficulty);
            SetMessage("");
            return true;
        }

        public bool ContinueNormalGame()
        {
            if (!_normalValid) return false;
            if (_normalComplete) return StartNewGame(NormalDifficulty);
            if (!IsDaily && _hasActiveGame) return true;
            var candidate = NewSession();
            if (!candidate.TryRestore(_data.sudokuSaveData)) return false;
            CommitGame(candidate, null, _data.sudokuSaveData);
            SetMessage("");
            return true;
        }

        public SudokuDailySaveData GetDaily(DateTime date)
        {
            _daily.TryGetValue(SudokuDailyRules.DateKey(date.Date), out var record);
            return record;
        }

        public bool CanOpenDaily(DateTime date) => SudokuDailyRules.CanStart(date, Today) && GetDaily(date)?.completed != true;

        public int CompletedDays(DateTime month)
        {
            int count = 0;
            foreach (var entry in _daily.Values)
                if (entry.completed && SudokuDailyRules.TryParseDate(entry.date, out var date) && date.Year == month.Year && date.Month == month.Month) count++;
            return count;
        }

        public bool OpenDailyChallenge(DateTime date)
        {
            date = date.Date;
            if (!CanOpenDaily(date)) return false;
            if (IsDaily && _hasActiveGame && DailyDate == date) return true;
            var record = GetDaily(date);
            var candidate = NewSession();
            SudokuDifficulty difficulty;
            if (record == null)
            {
                difficulty = SudokuDailyRules.Difficulty(date);
                try { candidate.BeginDailyLevel(difficulty, PeekNextIndex(difficulty)); }
                catch (Exception error) when (error is ArgumentException || error is InvalidOperationException) { SetMessage(error.Message); return false; }
                record = new SudokuDailySaveData { date = SudokuDailyRules.DateKey(date) };
                CommitGame(candidate, record, null, difficulty);
            }
            else
            {
                if (!candidate.TryRestoreDaily(record.game)) return false;
                CommitGame(candidate, record, record.game);
            }
            SetMessage("");
            return true;
        }

        private SudokuSession NewSession()
        {
            var candidate = new SudokuSession(_packs);
            candidate.CopyProgressFrom(Session);
            return candidate;
        }

        private void CommitGame(SudokuSession candidate, SudokuDailySaveData daily, SudokuSaveData saved, SudokuDifficulty? consumedPool = null)
        {
            CaptureActiveGame();
            Session = candidate; _hasActiveGame = true; IsDaily = daily != null;
            DailyDate = IsDaily ? DateTime.ParseExact(daily.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : default;
            _data.activeDailyDate = daily?.date;
            if (daily != null && !_daily.ContainsKey(daily.date)) { _daily.Add(daily.date, daily); _data.dailyChallenges.Add(daily); }
            if (consumedPool.HasValue)
                _data.sudokuPools[(int)consumedPool.Value].nextIndex = (candidate.LevelIndex + 1) % candidate.LevelCount(consumedPool.Value);
            LevelRevision++; ResetPlayState();
            if (saved != null) RestorePlayState(saved);
            SaveGameProgress();
        }

        private void RestorePlayState(SudokuSaveData saved)
        {
            ElapsedSeconds = double.IsNaN(saved.elapsedSeconds) || double.IsInfinity(saved.elapsedSeconds) ? 0 : Math.Max(0, Math.Min(315360000, saved.elapsedSeconds));
            SelectedCell = saved.selectedCell >= 0 && saved.selectedCell < 81 ? saved.selectedCell : -1;
            NoteMode = saved.noteMode;
        }

        private void InitializePools(SudokuSaveData legacy)
        {
            var old = _data.sudokuPools;
            _data.sudokuPools = new SudokuPoolCursor[6];
            for (int d = 0; d < 6; d++)
            {
                int next = 0;
                var cursor = old != null && d < old.Length ? old[d] : null;
                if (cursor?.packSignature == _packs[d].Signature && cursor.nextIndex >= 0 && cursor.nextIndex < _packs[d].Count) next = cursor.nextIndex;
                else if (cursor == null && legacy != null)
                {
                    int last = -1;
                    if (legacy.difficulty == d && legacy.packSignature == _packs[d].Signature && legacy.puzzleIndex >= 0 && legacy.puzzleIndex < _packs[d].Count) last = legacy.puzzleIndex;
                    var wins = legacy.completed != null && d < legacy.completed.Length ? legacy.completed[d] : null;
                    if (wins?.packSignature == _packs[d].Signature && wins.indices != null)
                        foreach (int index in wins.indices) if (index >= 0 && index < _packs[d].Count) last = Math.Max(last, index);
                    next = (last + 1) % _packs[d].Count;
                }
                _data.sudokuPools[d] = new SudokuPoolCursor { packSignature = _packs[d].Signature, nextIndex = next };
            }
        }

        public void NextLevel()
        {
            if (!IsDaily) StartNewGame(Session.Difficulty);
        }

        public void PreviousLevel() { if (!IsDaily) OpenLevel(Session.Difficulty, Math.Max(0, Session.LevelIndex - 1)); }

        public void Restart()
        {
            if (IsDaily && Session.IsComplete) return;
            Session.Restart(); LevelRevision++; ResetPlayState(); SaveGameProgress(); SetMessage("本题已重开。");
        }

        public bool EnterDigit(int cell, int digit, bool notes)
        {
            if (!CanInteract) return false;
            if (cell < 0 || cell >= 81 || Session.IsGiven(cell)) { SetMessage("请选择一个可填写的空格。"); return false; }
            int d = (int)Session.Difficulty;
            bool nextWasLocked = d < 5 && !Session.IsUnlocked((SudokuDifficulty)(d + 1));
            var completedBefore = Session.CompletedRegions(cell);
            if (!Session.SetDigit(cell, digit, notes)) return false;
            var newlyCompleted = Session.CompletedRegions(cell) & ~completedBefore;
            SaveGameProgress();
            if (Session.IsComplete && !IsDaily)
            {
                string message = "通关！当前难度已完成 " + Session.CompletedCount(Session.Difficulty) + " 道不同题。";
                if (nextWasLocked && Session.IsUnlocked((SudokuDifficulty)(d + 1))) message += "\n已解锁「" + SudokuBoard.DifficultyNames[d + 1] + "」！";
                SetMessage(message);
            }
            else SetMessage(Session.HasConflict(cell) ? "红色数字与同行、同列或同宫数字重复。" : notes ? "笔记模式：再次输入相同数字可取消标记。" : "");
            NotifyRegionsCompleted(cell, newlyCompleted);
            return true;
        }

        private void ResetPlayState()
        {
            ElapsedSeconds = 0; SelectedCell = -1; NoteMode = false; IsPaused = false;
        }
        public void SelectCell(int index)
        {
            if (!CanInteract || index < 0 || index >= 81) return;
            SelectedCell = index; GameEvent.Send(EventDefine.SudokuStateChanged);
        }
        public void ToggleNotes()
        {
            if (!CanInteract) return;
            NoteMode = !NoteMode; SaveGameProgress(); GameEvent.Send(EventDefine.SudokuStateChanged);
        }
        public void Undo()
        {
            if (!CanInteract || !Session.Undo()) return;
            SaveGameProgress(); SetMessage("");
        }
        public void TogglePause()
        {
            if (!IsWindowActive || IsAdShowing || Session.IsComplete) return;
            IsPaused = !IsPaused; SaveGameProgress(); GameEvent.Send(EventDefine.SudokuStateChanged);
        }
        public void SetSuspended(bool suspended)
        {
            IsSuspended = suspended;
            if (suspended) SaveGameProgress();
            GameEvent.Send(EventDefine.SudokuStateChanged);
        }
        public void SetWindowActive(bool active)
        {
            IsWindowActive = active;
            if (!active) SaveGameProgress();
        }
        public void SetAdShowing(bool showing)
        {
            IsAdShowing = showing;
            if (showing) SaveGameProgress();
            GameEvent.Send(EventDefine.SudokuStateChanged);
        }
        public void Tick(float seconds)
        {
            if (CanInteract && seconds > 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) ElapsedSeconds += seconds;
        }
        public bool TryUseHint(bool adRewarded, int expectedRevision)
        {
            if (!CanInteract || expectedRevision != LevelRevision) return false;
            int cell = Session.FindHintCell(SelectedCell);
            if (cell < 0) return false;
            bool byCount = Props.tipCount > 0;
            bool byCoin = !byCount && Props.coinCount >= HintCoinCost;
            if (!byCount && !byCoin && !adRewarded) return false;
            var completedBefore = Session.CompletedRegions(cell);
            if (!Session.ApplyHint(cell)) return false;
            var newlyCompleted = Session.CompletedRegions(cell) & ~completedBefore;
            if (byCount) Props.tipCount--;
            else if (byCoin) Props.coinCount -= HintCoinCost;
            SelectedCell = cell;
            SaveGameProgress();
            SetMessage(Session.IsComplete ? "通关！" : "已填入一个正确数字。");
            NotifyRegionsCompleted(cell, newlyCompleted);
            return true;
        }
        private void NotifyRegionsCompleted(int cell, SudokuCompletedRegion regions)
        {
            // 先完成状态刷新，再播放；读档、选格和笔记不会重播已完成区域。
            if (regions != SudokuCompletedRegion.None)
                GameEvent.Send(EventDefine.SudokuRegionsCompleted, this, cell, regions);
        }
        public void SaveGameProgress()
        {
            CaptureActiveGame();
            bool trophiesChanged = Trophies.RefreshAwards();
            _cache?.Save();
            if (trophiesChanged) GameEvent.Send(EventDefine.SudokuTrophiesChanged);
        }

        private void CaptureActiveGame()
        {
            if (!_hasActiveGame) return;
            var data = Session.Capture();
            data.elapsedSeconds = ElapsedSeconds; data.selectedCell = SelectedCell; data.noteMode = NoteMode;
            if (IsDaily)
            {
                var record = _daily[SudokuDailyRules.DateKey(DailyDate)];
                record.game = data; record.completed |= Session.IsComplete; record.progressPercent = Session.ProgressPercent;
            }
            else
            {
                _data.sudokuSaveData = data; _normalValid = true; _normalComplete = Session.IsComplete;
            }
        }

        public void SetMessage(string message)
        {
            Message = message;
            GameEvent.Send(EventDefine.SudokuStateChanged);
        }

        public string DescribeState()
        {
            var text = new StringBuilder();
            text.Append(SudokuBoard.DifficultyNames[(int)Session.Difficulty]).Append(" 第 ").Append(Session.LevelIndex + 1).Append('/').Append(Session.LevelCount(Session.Difficulty)).Append(" 关");
            text.Append(Session.IsComplete ? "，已通关" : "，进行中");
            for (int d = 0; d < 6; d++) text.Append("\n").Append(SudokuBoard.DifficultyNames[d]).Append(Session.IsUnlocked((SudokuDifficulty)d) ? "：已解锁，完成 " : "：未解锁，完成 ").Append(Session.CompletedCount((SudokuDifficulty)d)).Append(" 题");
            return text.ToString();
        }

#if !TE_RELEASE
        public void DebugUnlockAll()
        {
            Session.DebugUnlockAll(); SetMessage("[GM] 已临时解锁全部难度，实际通关数不变。");
        }
        public void DebugResetProgress()
        {
            Session = new SudokuSession(_packs); Session.BeginLevel(SudokuDifficulty.Quick, 0);
            _hasActiveGame = _normalValid = true; _normalComplete = IsDaily = false;
            DailyDate = default; _data.activeDailyDate = null;
            _data.dailyChallenges.Clear(); _daily.Clear(); InitializePools(null);
            Trophies.ClearAwards();
            _data.sudokuPools[0].nextIndex = 1 % _packs[0].Count;
            LevelRevision++; ResetPlayState(); SaveGameProgress(); SetMessage("[GM] 数独进度已重置，仅快速档开放。");
        }
        public void DebugComplete()
        {
            Session.DebugComplete(); SaveGameProgress(); SetMessage("[GM] 本关已通关，解锁进度按不同题累计。");
        }
        public void DebugFillNext()
        {
            bool filled = Session.DebugFillNext(); SaveGameProgress();
            SetMessage(filled ? "[GM] 已填入一个正确数字。" : "[GM] 本关已完成。");
        }
        public void DebugSetProps(int hints, int coins)
        {
            Props.tipCount = Math.Max(0, hints); Props.coinCount = Math.Max(0, coins);
            SaveGameProgress(); SetMessage("[GM] 已更新提示数量和金币。");
        }
#endif
    }
}

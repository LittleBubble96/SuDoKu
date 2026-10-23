using System;
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
        public SudokuSession Session { get; private set; }
        public string Message { get; private set; }
        public int LevelRevision { get; private set; }
        public int SelectedCell { get; private set; } = -1;
        public bool NoteMode { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsSuspended { get; private set; }
        public bool IsAdShowing { get; private set; }
        public bool IsWindowActive { get; private set; } = true;
        public bool CanInteract => IsWindowActive && !IsPaused && !IsSuspended && !IsAdShowing && !Session.IsComplete;
        public GamePropData Props { get; }

        public SudokuGameController(SudokuPack[] packs, GameCacheManager cache = null)
        {
            _packs = (SudokuPack[])packs.Clone();
            _cache = cache;
            Props = cache?.CacheData.gamePropData ?? new GamePropData();
            Session = new SudokuSession(_packs);
            var saved = cache?.CacheData.sudokuSaveData;
            if (Session.TryRestore(saved))
            {
                ElapsedSeconds = double.IsNaN(saved.elapsedSeconds) || double.IsInfinity(saved.elapsedSeconds) ? 0 : Math.Max(0, Math.Min(315360000, saved.elapsedSeconds));
                SelectedCell = saved.selectedCell >= 0 && saved.selectedCell < 81 ? saved.selectedCell : -1;
                NoteMode = saved.noteMode;
            }
            else Session.BeginLevel(SudokuDifficulty.Quick, Session.FirstUncompleted(SudokuDifficulty.Quick));
            LevelRevision = 1;
            Message = "每行、每列、每个九宫格内，数字 1—9 不重复。";
        }

        public bool OpenLevel(SudokuDifficulty difficulty, int index)
        {
            try
            {
                Session.BeginLevel(difficulty, index);
                LevelRevision++;
                ResetPlayState();
                SaveGameProgress();
                SetMessage(Session.WasCompleted(difficulty, index) ? "本题已完成过，重玩不会重复累计解锁进度。" : "每行、每列、每个九宫格内，数字 1—9 不重复。");
                return true;
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                SetMessage(e.Message); return false;
            }
        }

        public void NextLevel()
        {
            int next = Session.LevelIndex + 1;
            if (next >= Session.LevelCount(Session.Difficulty)) next = Session.FirstUncompleted(Session.Difficulty);
            OpenLevel(Session.Difficulty, next);
        }

        public void PreviousLevel() => OpenLevel(Session.Difficulty, Math.Max(0, Session.LevelIndex - 1));

        public void Restart()
        {
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
            if (Session.IsComplete)
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
            if (_cache == null) return;
            var data = Session.Capture();
            data.elapsedSeconds = ElapsedSeconds; data.selectedCell = SelectedCell; data.noteMode = NoteMode;
            _cache.CacheData.sudokuSaveData = data;
            _cache.Save();
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

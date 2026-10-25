using System;
using System.Collections.Generic;

namespace GameLogic.Sudoku
{
    /// <summary>纯数独规则和玩家状态；存档快照不包含答案。</summary>
    public sealed class SudokuSession
    {
        public const int RequiredCompletions = 2;
        private readonly SudokuPack[] _packs;
        private readonly HashSet<int>[] _completed = new HashSet<int>[6];
#if !TE_RELEASE
        private readonly bool[] _debugUnlocked = new bool[6];
#endif
        private byte[] _puzzle, _board, _solution;
        private string _sourceSignature;
        private bool _countsForUnlock = true;
        private readonly int[] _notes = new int[81];
        private readonly List<UndoState> _undo = new List<UndoState>();
        private sealed class UndoState { public byte[] Board; public int[] Notes; }
        public bool CanUndo => !IsComplete && _undo.Count > 0;
        public SudokuDifficulty Difficulty { get; private set; }
        public int LevelIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public bool HasLevel => _board != null;
        public int ProgressPercent
        {
            get
            {
                if (!HasLevel) return 0;
                int empty = 0, correct = 0;
                for (int i = 0; i < 81; i++)
                    if (_puzzle[i] == 0) { empty++; if (_board[i] == _solution[i]) correct++; }
                return IsComplete ? 100 : empty == 0 ? 0 : Math.Min(99, correct * 100 / empty);
            }
        }
        public int LevelCount(SudokuDifficulty difficulty) => _packs[(int)difficulty].Count;
        public int CompletedCount(SudokuDifficulty difficulty) => _completed[(int)difficulty].Count;
        public bool WasCompleted(SudokuDifficulty difficulty, int index) => _completed[(int)difficulty].Contains(index);
        public bool IsUnlocked(SudokuDifficulty difficulty)
        {
            if ((int)difficulty < 0 || (int)difficulty > 5) return false;
#if !TE_RELEASE
            if (_debugUnlocked[(int)difficulty]) return true;
#endif
            return difficulty == SudokuDifficulty.Quick || _completed[(int)difficulty - 1].Count >= RequiredCompletions;
        }
        public byte Value(int index) => _board[index];
        public bool IsGiven(int index) => _puzzle[index] != 0;
        public int Notes(int index) => _notes[index];
        public bool HasConflict(int index) => SudokuBoard.HasConflict(_board, index);
        public SudokuCompletedRegion CompletedRegions(int index) => SudokuCompletion.GetCompletedRegions(_board, _solution, index);

        public SudokuSession(SudokuPack[] packs)
        {
            if (packs == null || packs.Length != 6) throw new ArgumentException("必须提供六个难度包。");
            _packs = (SudokuPack[])packs.Clone();
            for (int i = 0; i < 6; i++)
            {
                if (packs[i] == null || (int)packs[i].Difficulty != i || packs[i].Count < RequiredCompletions) throw new ArgumentException("难度包缺失、顺序错误或不足两题。");
                _completed[i] = new HashSet<int>();
            }
        }

        public void BeginLevel(SudokuDifficulty difficulty, int index)
        {
            if (!IsUnlocked(difficulty)) throw new InvalidOperationException("前一难度需完成两道不同题。");
            BeginPuzzle(difficulty, index, _packs[(int)difficulty].GetPuzzle(index), _packs[(int)difficulty].Signature, true);
        }

        public void BeginDailyLevel(SudokuDifficulty difficulty, int index)
        {
            if ((int)difficulty < 0 || (int)difficulty >= 6) throw new ArgumentOutOfRangeException(nameof(difficulty));
            BeginPuzzle(difficulty, index, _packs[(int)difficulty].GetPuzzle(index), _packs[(int)difficulty].Signature, false);
        }

        private void BeginPuzzle(SudokuDifficulty difficulty, int index, byte[] puzzle, string signature, bool countsForUnlock)
        {
            // 解析及唯一解检查成功后再提交，失败不会覆盖旧对局。
            if (SudokuSolver.Solve(puzzle, out var solution) != 1) throw new InvalidOperationException("该关卡没有唯一答案。");
            Difficulty = difficulty; LevelIndex = index;
            _sourceSignature = signature; _countsForUnlock = countsForUnlock;
            _puzzle = puzzle; _board = (byte[])puzzle.Clone(); _solution = solution;
            Array.Clear(_notes, 0, 81); _undo.Clear(); IsComplete = false;
        }

        public int FirstUncompleted(SudokuDifficulty difficulty)
        {
            for (int i = 0; i < LevelCount(difficulty); i++) if (!WasCompleted(difficulty, i)) return i;
            return 0;
        }

        public bool SetDigit(int index, int digit, bool noteMode = false)
        {
            if (!HasLevel || index < 0 || index >= 81 || digit < 0 || digit > 9 || IsGiven(index) || IsComplete) return false;
            if (noteMode && digit != 0)
            {
                if (_board[index] != 0) return false;
                RememberUndo();
                _notes[index] ^= 1 << digit;
                return true;
            }
            if (_board[index] == digit && _notes[index] == 0) return false;
            RememberUndo();
            _board[index] = (byte)digit; _notes[index] = 0;
            if (digit != 0) foreach (int peer in SudokuBoard.Peers[index]) _notes[peer] &= ~(1 << digit);
            for (int i = 0; i < 81; i++) if (_board[i] != _solution[i]) return true;
            IsComplete = true;
            if (_countsForUnlock) _completed[(int)Difficulty].Add(LevelIndex);
            return true;
        }

        public void Restart()
        {
            if (!HasLevel) return;
            _board = (byte[])_puzzle.Clone(); Array.Clear(_notes, 0, 81); _undo.Clear(); IsComplete = false;
        }

        private void RememberUndo()
        {
            if (_undo.Count == 100) _undo.RemoveAt(0);
            _undo.Add(new UndoState { Board = (byte[])_board.Clone(), Notes = (int[])_notes.Clone() });
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            var state = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            _board = state.Board;
            Array.Copy(state.Notes, _notes, 81);
            return true;
        }

        public int FindHintCell(int preferred)
        {
            if (!HasLevel || IsComplete) return -1;
            if (preferred >= 0 && preferred < 81 && !IsGiven(preferred) && _board[preferred] != _solution[preferred]) return preferred;
            for (int i = 0; i < 81; i++) if (_board[i] != _solution[i]) return i;
            return -1;
        }

        public bool ApplyHint(int cell) => cell >= 0 && cell < 81 && !IsGiven(cell) && SetDigit(cell, _solution[cell]);

        public SudokuSaveData Capture()
        {
            var data = new SudokuSaveData
            {
                difficulty = (int)Difficulty, puzzleIndex = LevelIndex,
                packSignature = _sourceSignature,
                puzzle = new int[81], values = new int[81], notes = (int[])_notes.Clone(),
                completed = _countsForUnlock ? new SudokuCompletedData[6] : null
            };
            for (int i = 0; i < 81; i++) { data.puzzle[i] = _puzzle[i]; data.values[i] = _board[i]; }
            for (int d = 0; _countsForUnlock && d < 6; d++)
            {
                var indices = new int[_completed[d].Count];
                _completed[d].CopyTo(indices); Array.Sort(indices);
                data.completed[d] = new SudokuCompletedData { packSignature = _packs[d].Signature, indices = indices };
            }
            return data;
        }

        public bool TryRestore(SudokuSaveData data)
        {
            if (data == null) return false;
            RestoreCompleted(data.completed);
            return TryRestoreBoard(data, false);
        }

        public void RestoreCompleted(SudokuCompletedData[] completed)
        {
            // 题库内容变化时，只丢弃受影响档位的旧题号，避免把新题当成已通关。
            for (int d = 0; d < 6; d++)
            {
                _completed[d].Clear();
                var progress = completed != null && d < completed.Length ? completed[d] : null;
                if (progress?.packSignature != _packs[d].Signature || progress.indices == null) continue;
                foreach (int index in progress.indices) if (index >= 0 && index < _packs[d].Count) _completed[d].Add(index);
            }
        }

        public void CopyProgressFrom(SudokuSession other)
        {
            for (int d = 0; d < 6; d++)
            {
                _completed[d].Clear(); _completed[d].UnionWith(other._completed[d]);
#if !TE_RELEASE
                _debugUnlocked[d] = other._debugUnlocked[d];
#endif
            }
        }

        public bool TryRestoreDaily(SudokuSaveData data) => TryRestoreBoard(data, true);

        private bool TryRestoreBoard(SudokuSaveData data, bool daily)
        {
            if (data == null || data.difficulty < 0 || data.difficulty > 5 || (!daily && !IsUnlocked((SudokuDifficulty)data.difficulty))) return false;
            var pack = _packs[data.difficulty];
            if (data.puzzleIndex < 0 || data.values?.Length != 81 || data.notes?.Length != 81) return false;
            byte[] puzzle;
            if (daily && data.puzzle?.Length == 81)
            {
                puzzle = new byte[81];
                for (int i = 0; i < 81; i++) { if (data.puzzle[i] < 0 || data.puzzle[i] > 9) return false; puzzle[i] = (byte)data.puzzle[i]; }
            }
            else
            {
                if (data.packSignature != pack.Signature || data.puzzleIndex >= pack.Count) return false;
                puzzle = pack.GetPuzzle(data.puzzleIndex);
            }
            for (int i = 0; i < 81; i++)
                if (data.values[i] < 0 || data.values[i] > 9 || (puzzle[i] != 0 && data.values[i] != puzzle[i]) || (data.notes[i] & ~1022) != 0) return false;
            try { BeginPuzzle((SudokuDifficulty)data.difficulty, data.puzzleIndex, puzzle, data.packSignature, !daily); }
            catch (InvalidOperationException) { return false; }
            IsComplete = true;
            for (int i = 0; i < 81; i++)
            {
                _board[i] = (byte)data.values[i];
                _notes[i] = _board[i] == 0 ? data.notes[i] : 0;
                if (_board[i] != _solution[i]) IsComplete = false;
            }
            if (IsComplete && _countsForUnlock) _completed[(int)Difficulty].Add(LevelIndex);
            return true;
        }

#if !TE_RELEASE
        // GM overrides do not forge completion counts; resetting the session clears them.
        public void DebugUnlockAll()
        {
            for (int i = 0; i < 6; i++) _debugUnlocked[i] = true;
        }

        public bool DebugFillNext()
        {
            if (!HasLevel || IsComplete) return false;
            for (int i = 0; i < 81; i++) if (_board[i] != _solution[i]) return SetDigit(i, _solution[i]);
            return false;
        }

        public void DebugComplete()
        {
            if (!HasLevel || IsComplete) return;
            for (int i = 0; i < 81; i++) if (!IsGiven(i)) SetDigit(i, _solution[i]);
        }
#endif
    }
}

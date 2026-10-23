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
        private readonly int[] _notes = new int[81];
        private readonly List<UndoState> _undo = new List<UndoState>();
        private sealed class UndoState { public byte[] Board; public int[] Notes; }
        public bool CanUndo => !IsComplete && _undo.Count > 0;
        public SudokuDifficulty Difficulty { get; private set; }
        public int LevelIndex { get; private set; }
        public bool IsComplete { get; private set; }
        public bool HasLevel => _board != null;
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
            // Only commit the new level after decoding and uniqueness checking succeed.
            var puzzle = _packs[(int)difficulty].GetPuzzle(index);
            if (SudokuSolver.Solve(puzzle, out var solution) != 1) throw new InvalidOperationException("该关卡没有唯一答案。");
            Difficulty = difficulty; LevelIndex = index;
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
            _completed[(int)Difficulty].Add(LevelIndex);
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
                packSignature = _packs[(int)Difficulty].Signature,
                values = new int[81], notes = (int[])_notes.Clone(), completed = new SudokuCompletedData[6]
            };
            for (int i = 0; i < 81; i++) data.values[i] = _board[i];
            for (int d = 0; d < 6; d++)
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
            // 题库内容变化时，只丢弃受影响档位的旧题号，避免把新题当成已通关。
            for (int d = 0; d < 6; d++)
            {
                _completed[d].Clear();
                var progress = data.completed != null && d < data.completed.Length ? data.completed[d] : null;
                if (progress?.packSignature != _packs[d].Signature || progress.indices == null) continue;
                foreach (int index in progress.indices) if (index >= 0 && index < _packs[d].Count) _completed[d].Add(index);
            }
            if (data.difficulty < 0 || data.difficulty > 5 || !IsUnlocked((SudokuDifficulty)data.difficulty)) return false;
            var pack = _packs[data.difficulty];
            if (data.packSignature != pack.Signature || data.puzzleIndex < 0 || data.puzzleIndex >= pack.Count || data.values?.Length != 81 || data.notes?.Length != 81) return false;
            var puzzle = pack.GetPuzzle(data.puzzleIndex);
            for (int i = 0; i < 81; i++)
                if (data.values[i] < 0 || data.values[i] > 9 || (puzzle[i] != 0 && data.values[i] != puzzle[i]) || (data.notes[i] & ~1022) != 0) return false;
            BeginLevel((SudokuDifficulty)data.difficulty, data.puzzleIndex);
            IsComplete = true;
            for (int i = 0; i < 81; i++)
            {
                _board[i] = (byte)data.values[i];
                _notes[i] = _board[i] == 0 ? data.notes[i] : 0;
                if (_board[i] != _solution[i]) IsComplete = false;
            }
            if (IsComplete) _completed[(int)Difficulty].Add(LevelIndex);
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

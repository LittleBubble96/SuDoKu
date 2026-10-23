using System;
using System.Collections.Generic;

namespace GameLogic.Sudoku
{
    public enum SudokuTechnique { NakedSingle, HiddenSingle, LockedCandidate, NakedPair, HiddenPair, NakedTriple, HiddenTriple, XWing, XYWing, Swordfish, Coloring }

    public sealed class SudokuRating
    {
        public bool Solved;
        public int Remaining, HighestRank, Givens;
        public SudokuDifficulty Difficulty;
        public byte[] FinalBoard;
        public int[] TechniqueCounts = new int[11];
    }

    /// <summary>按从易到难的顺序循环，任一步有进展后重新从基础技巧开始。未解出不评为大师。</summary>
    public sealed class SudokuRater
    {
        private readonly byte[] _board;
        private readonly int[] _candidates = new int[81];
        private readonly SudokuRating _rating = new SudokuRating();
#if SUDOKU_VERIFY_TECHNIQUES
        private readonly byte[] _oracle;
#endif

        private SudokuRater(byte[] puzzle)
        {
            _board = (byte[])puzzle.Clone();
#if SUDOKU_VERIFY_TECHNIQUES
            if (SudokuSolver.Solve(puzzle, out _oracle) != 1) throw new ArgumentException("评级验证要求唯一解。");
#endif
            for (int i = 0; i < 81; i++)
            {
                if (_board[i] == 0) _candidates[i] = SudokuBoard.Candidates(_board, i);
                else _rating.Givens++;
            }
        }

        public static SudokuRating Rate(byte[] puzzle, int maximumRank = 5)
        {
            if (!SudokuBoard.IsValid(puzzle)) throw new ArgumentException("题面无效。");
            var rater = new SudokuRater(puzzle);
            rater.Run(maximumRank);
            return rater._rating;
        }

        private void Run(int maximumRank)
        {
            for (int guard = 0; guard < 1000; guard++)
            {
                if (NakedSingle()) continue;
                if (maximumRank >= 1 && HiddenSingle()) continue;
                if (maximumRank >= 2 && (LockedCandidate() || NakedSubset(2))) continue;
                if (maximumRank >= 3 && (HiddenSubset(2) || NakedSubset(3) || HiddenSubset(3))) continue;
                if (maximumRank >= 4 && (Fish(2) || XYWing())) continue;
                if (maximumRank >= 5 && (Fish(3) || Coloring())) continue;
                break;
            }
            for (int i = 0; i < 81; i++) if (_board[i] == 0) _rating.Remaining++;
            _rating.Solved = _rating.Remaining == 0 && SudokuBoard.IsValid(_board);
            _rating.FinalBoard = _board;
            // Quick is a shorter naked-single puzzle; it is an experience split within the basic tier.
            _rating.Difficulty = _rating.HighestRank == 0
                ? (_rating.Givens >= 60 ? SudokuDifficulty.Quick : SudokuDifficulty.Beginner)
                : (SudokuDifficulty)_rating.HighestRank;
        }

        private void Record(SudokuTechnique technique, int rank)
        {
            _rating.TechniqueCounts[(int)technique]++;
            _rating.HighestRank = Math.Max(_rating.HighestRank, rank);
        }

        private void Place(int i, int bit, SudokuTechnique technique, int rank)
        {
#if SUDOKU_VERIFY_TECHNIQUES
            if (_oracle[i] != SudokuBoard.Digit(bit)) throw new InvalidOperationException("技巧错误填写: " + technique);
#endif
            _board[i] = (byte)SudokuBoard.Digit(bit); _candidates[i] = 0;
            foreach (int peer in SudokuBoard.Peers[i]) _candidates[peer] &= ~bit;
            Record(technique, rank);
        }

        private bool Remove(int i, int mask)
        {
            int before = _candidates[i];
#if SUDOKU_VERIFY_TECHNIQUES
            if ((before & mask & (1 << _oracle[i])) != 0) throw new InvalidOperationException("技巧错误排除了真实答案。");
#endif
            _candidates[i] &= ~mask;
            return before != _candidates[i];
        }

        private bool NakedSingle()
        {
            for (int i = 0; i < 81; i++) if (SudokuBoard.IsSingle(_candidates[i]))
            { Place(i, _candidates[i], SudokuTechnique.NakedSingle, 0); return true; }
            return false;
        }

        private bool HiddenSingle()
        {
            foreach (var unit in SudokuBoard.Units) for (int d = 1; d <= 9; d++)
            {
                int bit = 1 << d, location = -1, count = 0;
                foreach (int i in unit) if ((_candidates[i] & bit) != 0) { location = i; count++; }
                if (count == 1) { Place(location, bit, SudokuTechnique.HiddenSingle, 1); return true; }
            }
            return false;
        }

        private bool LockedCandidate()
        {
            for (int u = 0; u < 27; u++) for (int d = 1; d <= 9; d++)
            {
                int bit = 1 << d, row = -1, column = -1, box = -1, count = 0;
                foreach (int i in SudokuBoard.Units[u]) if ((_candidates[i] & bit) != 0)
                {
                    if (count++ == 0) { row = i / 9; column = i % 9; box = SudokuBoard.Box(i); }
                    else { if (row != i / 9) row = -2; if (column != i % 9) column = -2; if (box != SudokuBoard.Box(i)) box = -2; }
                }
                if (count < 2) continue;
                int target = u >= 18 ? (row >= 0 ? row : column >= 0 ? 9 + column : -1) : (box >= 0 ? 18 + box : -1);
                if (target < 0) continue;
                bool changed = false;
                foreach (int i in SudokuBoard.Units[target])
                {
                    bool inSource = u < 9 ? i / 9 == u : u < 18 ? i % 9 == u - 9 : SudokuBoard.Box(i) == u - 18;
                    if (!inSource) changed |= Remove(i, bit);
                }
                if (changed) { Record(SudokuTechnique.LockedCandidate, 2); return true; }
            }
            return false;
        }

        private bool NakedSubset(int size)
        {
            foreach (var unit in SudokuBoard.Units)
            {
                var eligible = new List<int>();
                foreach (int i in unit) { int n = SudokuBoard.CountBits(_candidates[i]); if (n >= 2 && n <= size) eligible.Add(i); }
                for (int choice = 1; choice < (1 << eligible.Count); choice++)
                {
                    if (SudokuBoard.CountBits(choice) != size) continue;
                    int mask = 0;
                    for (int k = 0; k < eligible.Count; k++) if ((choice & (1 << k)) != 0) mask |= _candidates[eligible[k]];
                    if (SudokuBoard.CountBits(mask) != size) continue;
                    bool changed = false;
                    foreach (int i in unit)
                    {
                        int k = eligible.IndexOf(i);
                        if (k < 0 || (choice & (1 << k)) == 0) changed |= Remove(i, mask);
                    }
                    if (changed) { Record(size == 2 ? SudokuTechnique.NakedPair : SudokuTechnique.NakedTriple, size == 2 ? 2 : 3); return true; }
                }
            }
            return false;
        }

        private bool HiddenSubset(int size)
        {
            foreach (var unit in SudokuBoard.Units)
            {
                var positions = new int[10];
                for (int k = 0; k < 9; k++) for (int d = 1; d <= 9; d++) if ((_candidates[unit[k]] & (1 << d)) != 0) positions[d] |= 1 << k;
                for (int choice = 1; choice < 512; choice++)
                {
                    if (SudokuBoard.CountBits(choice) != size) continue;
                    int cells = 0, digits = choice << 1; bool valid = true;
                    for (int d = 1; d <= 9; d++) if ((digits & (1 << d)) != 0)
                    {
                        if (positions[d] == 0) { valid = false; break; }
                        cells |= positions[d];
                    }
                    if (!valid || SudokuBoard.CountBits(cells) != size) continue;
                    bool changed = false;
                    for (int k = 0; k < 9; k++) if ((cells & (1 << k)) != 0) changed |= Remove(unit[k], SudokuBoard.AllDigits & ~digits);
                    if (changed) { Record(size == 2 ? SudokuTechnique.HiddenPair : SudokuTechnique.HiddenTriple, 3); return true; }
                }
            }
            return false;
        }

        private bool Fish(int size)
        {
            for (int axis = 0; axis < 2; axis++) for (int d = 1; d <= 9; d++)
            {
                int bit = 1 << d; var positions = new int[9]; int eligible = 0;
                for (int b = 0; b < 9; b++)
                {
                    foreach (int i in SudokuBoard.Units[axis * 9 + b]) if ((_candidates[i] & bit) != 0) positions[b] |= 1 << (axis == 0 ? i % 9 : i / 9);
                    int n = SudokuBoard.CountBits(positions[b]); if (n >= 2 && n <= size) eligible |= 1 << b;
                }
                for (int bases = eligible; bases != 0; bases = (bases - 1) & eligible)
                {
                    if (SudokuBoard.CountBits(bases) != size) continue;
                    int covers = 0;
                    for (int b = 0; b < 9; b++) if ((bases & (1 << b)) != 0) covers |= positions[b];
                    if (SudokuBoard.CountBits(covers) != size) continue;
                    bool changed = false;
                    for (int b = 0; b < 9; b++) if ((bases & (1 << b)) == 0)
                        for (int c = 0; c < 9; c++) if ((covers & (1 << c)) != 0) changed |= Remove(axis == 0 ? b * 9 + c : c * 9 + b, bit);
                    if (changed) { Record(size == 2 ? SudokuTechnique.XWing : SudokuTechnique.Swordfish, size == 2 ? 4 : 5); return true; }
                }
            }
            return false;
        }

        private bool XYWing()
        {
            for (int pivot = 0; pivot < 81; pivot++) if (SudokuBoard.CountBits(_candidates[pivot]) == 2)
                foreach (int a in SudokuBoard.Peers[pivot]) if (SudokuBoard.CountBits(_candidates[a]) == 2)
                {
                    int shared = _candidates[pivot] & _candidates[a], z = _candidates[a] & ~_candidates[pivot], other = _candidates[pivot] & ~shared;
                    if (!SudokuBoard.IsSingle(shared) || !SudokuBoard.IsSingle(z)) continue;
                    foreach (int b in SudokuBoard.Peers[pivot]) if (b != a && _candidates[b] == (other | z))
                    {
                        bool changed = false;
                        foreach (int target in SudokuBoard.Peers[a])
                            if (target != pivot && target != a && target != b && Array.IndexOf(SudokuBoard.Peers[b], target) >= 0) changed |= Remove(target, z);
                        if (changed) { Record(SudokuTechnique.XYWing, 4); return true; }
                    }
                }
            return false;
        }

        // Single-digit strong-link chains: color contradiction and sees-both-colors elimination.
        private bool Coloring()
        {
            for (int d = 1; d <= 9; d++)
            {
                int bit = 1 << d;
                var graph = new List<int>[81];
                for (int i = 0; i < 81; i++) graph[i] = new List<int>();
                foreach (var unit in SudokuBoard.Units)
                {
                    int a = -1, b = -1, n = 0;
                    foreach (int i in unit) if ((_candidates[i] & bit) != 0) { if (n++ == 0) a = i; else b = i; }
                    if (n == 2) { graph[a].Add(b); graph[b].Add(a); }
                }
                var seen = new bool[81];
                for (int root = 0; root < 81; root++) if (!seen[root] && graph[root].Count > 0)
                {
                    var colors = new int[81]; var component = new List<int>(); var queue = new Queue<int>();
                    colors[root] = 1; seen[root] = true; queue.Enqueue(root);
                    while (queue.Count > 0)
                    {
                        int a = queue.Dequeue(); component.Add(a);
                        foreach (int b in graph[a]) if (!seen[b]) { seen[b] = true; colors[b] = 3 - colors[a]; queue.Enqueue(b); }
                    }
                    for (int color = 1; color <= 2; color++)
                    {
                        bool conflict = false;
                        foreach (int a in component) if (colors[a] == color)
                            foreach (int b in SudokuBoard.Peers[a]) if (colors[b] == color) conflict = true;
                        if (!conflict) continue;
                        bool changed = false;
                        foreach (int a in component) if (colors[a] == color) changed |= Remove(a, bit);
                        if (changed) { Record(SudokuTechnique.Coloring, 5); return true; }
                    }
                    bool removed = false;
                    for (int i = 0; i < 81; i++) if (colors[i] == 0 && (_candidates[i] & bit) != 0)
                    {
                        int visible = 0;
                        foreach (int peer in SudokuBoard.Peers[i]) if (colors[peer] != 0) visible |= 1 << colors[peer];
                        if (visible == 6) removed |= Remove(i, bit);
                    }
                    if (removed) { Record(SudokuTechnique.Coloring, 5); return true; }
                }
            }
            return false;
        }
    }
}

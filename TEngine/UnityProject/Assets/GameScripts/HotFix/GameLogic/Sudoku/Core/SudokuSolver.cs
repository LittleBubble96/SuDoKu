using System;

namespace GameLogic.Sudoku
{
    /// <summary>MRV 回溯求解；不修改输入，找到第二个答案立即停止。</summary>
    public static class SudokuSolver
    {
        public static int Solve(byte[] puzzle, out byte[] solution, int limit = 2)
        {
            solution = null;
            if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
            if (!SudokuBoard.IsValid(puzzle)) return 0;
            var search = new Search(puzzle, limit);
            search.Run();
            solution = search.Solution;
            return search.Count;
        }

        public static byte[] GenerateSolved(Random random)
        {
            var search = new Search(new byte[81], 1, random);
            search.Run();
            return search.Solution;
        }

        private sealed class Search
        {
            private readonly byte[] _board;
            private readonly int[] _rows = new int[9], _columns = new int[9], _boxes = new int[9];
            private readonly int _limit;
            private readonly Random _random;
            public byte[] Solution;
            public int Count;

            public Search(byte[] board, int limit, Random random = null)
            {
                _board = (byte[])board.Clone();
                _limit = limit;
                _random = random;
                for (int i = 0; i < 81; i++) if (_board[i] != 0)
                {
                    int bit = 1 << _board[i];
                    _rows[i / 9] |= bit; _columns[i % 9] |= bit; _boxes[SudokuBoard.Box(i)] |= bit;
                }
            }

            public void Run()
            {
                int best = -1, options = 0, bestCount = 10;
                for (int i = 0; i < 81; i++) if (_board[i] == 0)
                {
                    int mask = SudokuBoard.AllDigits & ~(_rows[i / 9] | _columns[i % 9] | _boxes[SudokuBoard.Box(i)]);
                    int n = SudokuBoard.CountBits(mask);
                    if (n == 0) return;
                    if (n < bestCount) { best = i; options = mask; bestCount = n; if (n == 1) break; }
                }
                if (best < 0)
                {
                    Count++;
                    if (Solution == null) Solution = (byte[])_board.Clone();
                    return;
                }
                // Rotation randomizes traversal without allocating candidate arrays at every node.
                int first = _random == null ? 1 : _random.Next(1, 10);
                for (int k = 0; k < 9 && Count < _limit; k++)
                {
                    int value = (first - 1 + k) % 9 + 1, bit = 1 << value;
                    if ((options & bit) == 0) continue;
                    _board[best] = (byte)value;
                    int r = best / 9, c = best % 9, b = SudokuBoard.Box(best);
                    _rows[r] |= bit; _columns[c] |= bit; _boxes[b] |= bit;
                    Run();
                    _rows[r] ^= bit; _columns[c] ^= bit; _boxes[b] ^= bit;
                    _board[best] = 0;
                }
            }
        }
    }
}

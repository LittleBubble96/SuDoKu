using System;
using System.Collections.Generic;

namespace GameLogic.Sudoku
{
    /// <summary>离线使用；每个随机终盘逐步挖空，满足唯一解及评级后收录。生成不进入客户端启动流程。</summary>
    public static class SudokuGenerator
    {
        public static List<byte[]>[] Generate(int perDifficulty, int seed, int maximumAttempts, Action<int, int[]> progress = null)
        {
            if (perDifficulty < 2) throw new ArgumentOutOfRangeException(nameof(perDifficulty));
            var random = new Random(seed);
            var banks = new List<byte[]>[6]; var fingerprints = new HashSet<string>();
            for (int d = 0; d < 6; d++) banks[d] = new List<byte[]>();
            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                var puzzle = SudokuSolver.GenerateSolved(random);
                var order = new int[81]; for (int i = 0; i < 81; i++) order[i] = i;
                for (int i = 80; i > 0; i--) { int j = random.Next(i + 1), swap = order[i]; order[i] = order[j]; order[j] = swap; }
                int givens = 81; var savedThisBoard = new bool[6];
                foreach (int index in order)
                {
                    byte previous = puzzle[index]; puzzle[index] = 0;
                    if (SudokuSolver.Solve(puzzle, out _) != 1) { puzzle[index] = previous; continue; }
                    givens--;
                    bool needBasic = givens >= 60 && givens <= 66 && banks[0].Count < perDifficulty || givens >= 42 && givens <= 50 && banks[1].Count < perDifficulty;
                    bool needHigher = givens <= 38 && (banks[2].Count < perDifficulty || banks[3].Count < perDifficulty || banks[4].Count < perDifficulty || banks[5].Count < perDifficulty);
                    if (!needBasic && !needHigher) continue;
                    var rating = SudokuRater.Rate(puzzle);
                    if (!rating.Solved) continue;
                    int d = (int)rating.Difficulty;
                    if (savedThisBoard[d] || banks[d].Count >= perDifficulty) continue;
                    if (d == 1 && (givens < 42 || givens > 50)) continue;
                    if (d >= 2 && givens > 38) continue;
                    // Normalize digit labels to reject duplicates that merely rename the digits.
                    var map = new byte[10]; byte next = 1; var key = new char[81];
                    for (int i = 0; i < 81; i++) { byte v = puzzle[i]; if (v != 0 && map[v] == 0) map[v] = next++; key[i] = (char)('0' + map[v]); }
                    if (!fingerprints.Add(new string(key))) continue;
                    banks[d].Add((byte[])puzzle.Clone()); savedThisBoard[d] = true;
                }
                var counts = new int[6]; bool done = true;
                for (int d = 0; d < 6; d++) { counts[d] = banks[d].Count; done &= counts[d] >= perDifficulty; }
                if (attempt % 25 == 0 || done) progress?.Invoke(attempt + 1, counts);
                if (done) return banks;
            }
            throw new InvalidOperationException("达到生成次数上限，部分难度数量不足；未降低难度或复制题目补数。");
        }
    }
}

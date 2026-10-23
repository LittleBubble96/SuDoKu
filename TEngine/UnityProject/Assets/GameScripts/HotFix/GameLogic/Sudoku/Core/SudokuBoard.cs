using System;
using System.Collections.Generic;

namespace GameLogic.Sudoku
{
    public enum SudokuDifficulty : byte { Quick, Beginner, Intermediate, Advanced, Expert, Master }

    public static class SudokuBoard
    {
        public const int CellCount = 81;
        public const int AllDigits = 0x3FE;
        public static readonly int[][] Units = CreateUnits();
        public static readonly int[][] Peers = CreatePeers();
        public static readonly string[] DifficultyNames = { "快速", "初级", "中级", "高级", "专家", "大师" };

        public static int Box(int index) => index / 27 * 3 + index % 9 / 3;
        public static int CountBits(int mask) { int n = 0; for (; mask != 0; mask &= mask - 1) n++; return n; }
        public static int Digit(int bit) { int d = 0; while ((bit >>= 1) != 0) d++; return d; }
        public static bool IsSingle(int mask) => mask != 0 && (mask & (mask - 1)) == 0;

        public static bool IsValid(byte[] board)
        {
            if (board == null || board.Length != CellCount) return false;
            foreach (var unit in Units)
            {
                int seen = 0;
                foreach (int i in unit)
                {
                    int value = board[i];
                    if (value > 9) return false;
                    if (value == 0) continue;
                    int bit = 1 << value;
                    if ((seen & bit) != 0) return false;
                    seen |= bit;
                }
            }
            return true;
        }

        public static int Candidates(byte[] board, int index)
        {
            if (board[index] != 0) return 0;
            int mask = AllDigits;
            foreach (int peer in Peers[index]) mask &= ~(1 << board[peer]);
            return mask;
        }

        public static bool HasConflict(byte[] board, int index)
        {
            if (board[index] == 0) return false;
            foreach (int peer in Peers[index]) if (board[peer] == board[index]) return true;
            return false;
        }

        private static int[][] CreateUnits()
        {
            var units = new int[27][];
            for (int i = 0; i < 27; i++) units[i] = new int[9];
            for (int r = 0; r < 9; r++) for (int c = 0; c < 9; c++)
            {
                int i = r * 9 + c;
                units[r][c] = i;
                units[9 + c][r] = i;
                units[18 + Box(i)][r % 3 * 3 + c % 3] = i;
            }
            return units;
        }

        private static int[][] CreatePeers()
        {
            var result = new int[81][];
            for (int i = 0; i < 81; i++)
            {
                var peers = new List<int>();
                for (int j = 0; j < 81; j++)
                    if (i != j && (i / 9 == j / 9 || i % 9 == j % 9 || Box(i) == Box(j))) peers.Add(j);
                result[i] = peers.ToArray();
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace GameLogic.Sudoku
{
    /// <summary>16 字节文件头 + N*41 字节题面。行优先，高半字节在前，最后低半字节固定为 0。</summary>
    public sealed class SudokuPack
    {
        public const int HeaderSize = 16, RecordSize = 41;
        public const byte FormatVersion = 1, RatingVersion = 1;
        public SudokuDifficulty Difficulty { get; private set; }
        public int Count { get; private set; }
        public string Signature => Read32(_bytes, 12).ToString("X8");
        private byte[] _bytes;

        public static SudokuPack Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderSize) throw new InvalidDataException("关卡包不完整。");
            if (bytes[0] != 'S' || bytes[1] != 'D' || bytes[2] != 'K' || bytes[3] != 'P') throw new InvalidDataException("关卡包标识错误。");
            if (bytes[4] != FormatVersion || bytes[7] != RatingVersion) throw new InvalidDataException("关卡包版本不支持。");
            if (bytes[5] > 5 || bytes[6] != RecordSize) throw new InvalidDataException("关卡包格式错误。");
            uint count = Read32(bytes, 8);
            if (count == 0 || HeaderSize + (long)count * RecordSize != bytes.Length) throw new InvalidDataException("关卡数量或文件长度错误。");
            if (Read32(bytes, 12) != Crc32(bytes, HeaderSize)) throw new InvalidDataException("关卡包校验失败。");
            // Validate digits and padding once; decode only the selected puzzle on entering a level.
            for (int record = 0; record < count; record++)
            {
                int start = HeaderSize + record * RecordSize;
                for (int i = 0; i < RecordSize; i++)
                    if ((bytes[start + i] >> 4) > 9 || (bytes[start + i] & 15) > 9) throw new InvalidDataException("关卡含非法数字。");
                if ((bytes[start + 40] & 15) != 0) throw new InvalidDataException("关卡填充位错误。");
            }
            return new SudokuPack { Difficulty = (SudokuDifficulty)bytes[5], Count = (int)count, _bytes = (byte[])bytes.Clone() };
        }

        public byte[] GetPuzzle(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            var puzzle = new byte[81];
            int offset = HeaderSize + index * RecordSize;
            for (int i = 0; i < 81; i++) puzzle[i] = (byte)((_bytes[offset + i / 2] >> (i % 2 == 0 ? 4 : 0)) & 15);
            return puzzle;
        }

        public static byte[] Write(SudokuDifficulty difficulty, IList<byte[]> puzzles)
        {
            if ((int)difficulty > 5 || puzzles == null || puzzles.Count == 0) throw new ArgumentException("关卡或难度无效。");
            var bytes = new byte[checked(HeaderSize + RecordSize * puzzles.Count)];
            bytes[0] = (byte)'S'; bytes[1] = (byte)'D'; bytes[2] = (byte)'K'; bytes[3] = (byte)'P';
            bytes[4] = FormatVersion; bytes[5] = (byte)difficulty; bytes[6] = RecordSize; bytes[7] = RatingVersion;
            Write32(bytes, 8, (uint)puzzles.Count);
            for (int p = 0; p < puzzles.Count; p++)
            {
                if (!SudokuBoard.IsValid(puzzles[p])) throw new ArgumentException("题面违反数独规则。");
                for (int i = 0; i < 81; i++) bytes[HeaderSize + p * RecordSize + i / 2] |= (byte)(puzzles[p][i] << (i % 2 == 0 ? 4 : 0));
            }
            Write32(bytes, 12, Crc32(bytes, HeaderSize));
            return bytes;
        }

        private static uint Read32(byte[] b, int i) => (uint)b[i] | (uint)b[i + 1] << 8 | (uint)b[i + 2] << 16 | (uint)b[i + 3] << 24;
        private static void Write32(byte[] b, int i, uint v) { for (int k = 0; k < 4; k++) b[i + k] = (byte)(v >> (k * 8)); }
        private static uint Crc32(byte[] bytes, int offset)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = offset; i < bytes.Length; i++)
            {
                crc ^= bytes[i];
                for (int k = 0; k < 8; k++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0u);
            }
            return ~crc;
        }
    }
}

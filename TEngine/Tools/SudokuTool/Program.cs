using System.Diagnostics;
using System.Text.Json;
using GameLogic.Sudoku;

internal static class Program
{
    private static int _checks;
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "generate")
            {
                string output = args[1], report = args[2];
                int count = args.Length > 3 ? int.Parse(args[3]) : 20;
                int seed = args.Length > 4 ? int.Parse(args[4]) : 20261022;
                int attempts = args.Length > 5 ? int.Parse(args[5]) : 100000;
                var timer = Stopwatch.StartNew();
                var banks = SudokuGenerator.Generate(count, seed, attempts, (n, counts) => Console.WriteLine($"attempt={n} [{string.Join(',', counts)}] {timer.Elapsed.TotalSeconds:F1}s"));
                Directory.CreateDirectory(output);
                for (int d = 0; d < 6; d++) File.WriteAllBytes(Path.Combine(output, $"sudoku_{((SudokuDifficulty)d).ToString().ToLowerInvariant()}.bytes"), SudokuPack.Write((SudokuDifficulty)d, banks[d]));
                var records = new List<object>();
                for (int d = 0; d < 6; d++) for (int i = 0; i < banks[d].Count; i++)
                {
                    var r = SudokuRater.Rate(banks[d][i]);
                    records.Add(new { difficulty = ((SudokuDifficulty)d).ToString(), index = i, givens = r.Givens,
                        highestRank = r.HighestRank, techniques = Enum.GetValues<SudokuTechnique>().Where(t => r.TechniqueCounts[(int)t] > 0).ToDictionary(t => t.ToString(), t => r.TechniqueCounts[(int)t]) });
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
                File.WriteAllText(report, JsonSerializer.Serialize(new { seed, ratingVersion = SudokuPack.RatingVersion, countPerDifficulty = count, elapsedSeconds = timer.Elapsed.TotalSeconds, records }, new JsonSerializerOptions { WriteIndented = true }));
                Test(output);
            }
            else Test(args.Length > 1 ? args[1] : null);
            Console.WriteLine($"PASS: {_checks} checks"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void TestSavedGame(SudokuPack[] packs, byte[] puzzle, byte[] answer)
    {
        var game = new SudokuSession(packs);
        game.BeginLevel(SudokuDifficulty.Quick, 0);
        int empty = Array.IndexOf(puzzle, (byte)0);
        game.SetDigit(empty, answer[empty], true);
        var save = game.Capture();
        var restored = new SudokuSession(packs);
        Assert(restored.TryRestore(save) && restored.Notes(empty) == (1 << answer[empty]), "saved notes roundtrip");
        restored.SetDigit(empty, answer[empty]);
        Assert(restored.Undo() && restored.Value(empty) == 0 && restored.Notes(empty) == (1 << answer[empty]), "undo restores erased notes");
        Assert(restored.FindHintCell(empty) == empty && restored.ApplyHint(empty), "hint fills selected missing value");
        Assert(restored.Value(empty) == answer[empty], "hint value correct");
        Assert(restored.FindHintCell(0) != 0, "given selection falls back to missing cell");
        restored.Restart();
        Assert(!restored.CanUndo, "restart clears undo stack");
        restored.SetDigit(empty, puzzle[0]);
        var wrongSave = restored.Capture();
        Assert(game.TryRestore(wrongSave) && game.Value(empty) == puzzle[0] && game.HasConflict(empty), "wrong player entries survive reload");
        save.values[0] = 0;
        Assert(!new SudokuSession(packs).TryRestore(save), "reject altered givens in cache");
        save = restored.Capture(); save.notes[empty] = 1 << 12;
        Assert(!new SudokuSession(packs).TryRestore(save), "reject invalid note bit in cache");
        save = restored.Capture(); save.packSignature = "changed";
        Assert(!new SudokuSession(packs).TryRestore(save), "stale puzzle pack rejected");
        save = restored.Capture(); save.completed[0].indices = new[] { 0, 0, 1, -1, 9999 };
        Assert(new SudokuSession(packs).TryRestore(save), "restore valid progress record");
        var progress = new SudokuSession(packs); progress.TryRestore(save);
        Assert(progress.CompletedCount(SudokuDifficulty.Quick) == 2 && progress.IsUnlocked(SudokuDifficulty.Beginner), "deduplicate progress and restore unlock");
        save.completed[0].packSignature = "old";
        progress.TryRestore(save);
        Assert(progress.CompletedCount(SudokuDifficulty.Quick) == 0 && !progress.IsUnlocked(SudokuDifficulty.Beginner), "changed pack cannot grant stale unlocks");
        save = restored.Capture(); save.values[empty] = 0;
        Assert(restored.Value(empty) != 0, "capture is independent of live board");
    }

    private static void Assert(bool value, string name) { _checks++; if (!value) throw new Exception("FAIL: " + name); }
    private static void Throws(Action action, string name) { bool thrown = false; try { action(); } catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is InvalidDataException) { thrown = true; } Assert(thrown, name); }
    private static byte[] Parse(string text) => text.Select(c => (byte)(c - '0')).ToArray();

    private static void TestCompletion(SudokuPack[] packs, byte[] puzzle, byte[] answer)
    {
        var all = SudokuCompletedRegion.Row | SudokuCompletedRegion.Column | SudokuCompletedRegion.Box;
        for (int origin = 0; origin < 81; origin++)
        {
            var board = (byte[])answer.Clone(); board[origin] = 0;
            Assert(SudokuCompletion.GetCompletedRegions(board, answer, origin) == SudokuCompletedRegion.None, "incomplete origin blocks all units");
            board[origin] = (byte)(answer[origin] % 9 + 1);
            Assert(SudokuCompletion.GetCompletedRegions(board, answer, origin) == SudokuCompletedRegion.None, "wrong final digit does not complete a unit");
            board[origin] = answer[origin];
            Assert(SudokuCompletion.GetCompletedRegions(board, answer, origin) == all, "correct final digit completes all three units");
            for (int flags = 1; flags <= 7; flags++)
            {
                var regions = (SudokuCompletedRegion)flags;
                board = new byte[81];
                if ((regions & SudokuCompletedRegion.Row) != 0)
                    foreach (int cell in SudokuBoard.Units[origin / 9]) board[cell] = answer[cell];
                if ((regions & SudokuCompletedRegion.Column) != 0)
                    foreach (int cell in SudokuBoard.Units[9 + origin % 9]) board[cell] = answer[cell];
                if ((regions & SudokuCompletedRegion.Box) != 0)
                    foreach (int cell in SudokuBoard.Units[18 + SudokuBoard.Box(origin)]) board[cell] = answer[cell];
                Assert(SudokuCompletion.GetCompletedRegions(board, answer, origin) == regions, "only completed units included");
                int visited = 0;
                for (int cell = 0; cell < 81; cell++)
                {
                    int step = SudokuCompletion.WaveStep(origin, cell, regions);
                    Assert((step >= 0) == (board[cell] != 0), "wave stays inside union of completed units");
                    if (step < 0) continue;
                    visited++;
                    bool predecessor = cell == origin;
                    foreach (int adjacent in new[] { cell - 9, cell + 9, cell % 9 > 0 ? cell - 1 : -1, cell % 9 < 8 ? cell + 1 : -1 })
                        if (SudokuCompletion.WaveStep(origin, adjacent, regions) == step - 1 && step > 0) predecessor = true;
                    Assert(predecessor, "each ring follows an adjacent cell from the previous ring");
                }
                int expected = flags == 7 ? 21 : flags == 3 ? 17 : flags == 5 || flags == 6 ? 15 : 9;
                Assert(visited == expected, "intersecting row column and box deduplicated");
                Assert(SudokuCompletion.WaveStep(origin, origin, regions) == 0, "selected input always flashes first");
            }
        }
        var game = new SudokuSession(packs); game.BeginLevel(SudokuDifficulty.Quick, 0);
        int first = Array.FindIndex(puzzle, v => v == 0);
        int other = Enumerable.Range(0, 81).First(i => puzzle[i] == 0 && i / 9 != first / 9 &&
            i % 9 != first % 9 && SudokuBoard.Box(i) != SudokuBoard.Box(first));
        var save = game.Capture();
        for (int i = 0; i < 81; i++) save.values[i] = i == first || i == other ? 0 : answer[i];
        Assert(game.TryRestore(save), "restore near-complete fixture");
        game.SetDigit(first, answer[first], true);
        Assert(game.CompletedRegions(first) == SudokuCompletedRegion.None, "notes never complete units");
        game.SetDigit(first, answer[first]);
        Assert(game.CompletedRegions(first) == all, "manual input completes intersecting units");
        Assert(!game.SetDigit(first, answer[first]), "unchanged input does not create another completion");
        game.Undo();
        Assert(game.CompletedRegions(first) == SudokuCompletedRegion.None, "undo clears completion");
        game.ApplyHint(first);
        Assert(game.CompletedRegions(first) == all, "hint uses same completion rules");
        Assert(!game.IsComplete, "unit completion does not require whole puzzle completion");
        Assert(SudokuCompletion.WaveStep(-1, 0, all) == -1 &&
            SudokuCompletion.GetCompletedRegions(null, answer, first) == SudokuCompletedRegion.None, "invalid completion input rejected");
    }

    private static void Test(string directory)
    {
        var quick = Parse("517962038690734512204018976003856790856407123049123800381640207972385041460271389");
        var expertSample = Parse("000384000048901630050060040910000064203000809870000021090010080036508910000493000");
        var masterSample = Parse("602003004000510000000027005906000030013000680020000509200340000000075000800100407");
        Assert(SudokuSolver.Solve(quick, out var answer) == 1, "unique puzzle");
        Assert(answer.All(x => x >= 1 && x <= 9) && SudokuBoard.IsValid(answer), "valid answer");
        Assert(quick.Contains((byte)0), "solver leaves input unchanged");
        var invalid = (byte[])quick.Clone(); invalid[1] = invalid[0];
        Assert(SudokuSolver.Solve(invalid, out _) == 0, "reject duplicate givens");
        Assert(SudokuSolver.Solve(new byte[81], out _) == 2, "stop at second solution");
        Assert(SudokuRater.Rate(quick).Difficulty == SudokuDifficulty.Quick, "quick classification");
        Assert(SudokuRater.Rate(expertSample).Difficulty == SudokuDifficulty.Beginner, "competitor expert stays basic");
        Assert(SudokuRater.Rate(masterSample).Difficulty == SudokuDifficulty.Intermediate, "competitor master is intermediate");
        Assert(!SudokuRater.Rate(masterSample, 1).Solved, "intermediate fails lower-tier solver");

        var records = new List<byte[]> { quick, expertSample, masterSample };
        byte[] encoded = SudokuPack.Write(SudokuDifficulty.Quick, records);
        Assert(encoded.Length == 16 + 3 * 41, "exact pack size; no solutions stored");
        var pack = SudokuPack.Read(encoded);
        for (int i = 0; i < 3; i++) Assert(pack.GetPuzzle(i).SequenceEqual(records[i]), "all 81 digits roundtrip " + i);
        byte[] corrupt = (byte[])encoded.Clone(); corrupt[20] ^= 1;
        Throws(() => SudokuPack.Read(corrupt), "CRC detects payload corruption");
        Throws(() => SudokuPack.Read(encoded[..^1]), "detect truncated file");
        corrupt = (byte[])encoded.Clone(); corrupt[7]++;
        Throws(() => SudokuPack.Read(corrupt), "reject unsupported rating version");
        Throws(() => pack.GetPuzzle(-1), "reject negative level index");
        var packs = new SudokuPack[6];
        for (int d = 0; d < 6; d++) packs[d] = SudokuPack.Read(SudokuPack.Write((SudokuDifficulty)d, records));
        var session = new SudokuSession(packs);
        TestSavedGame(packs, quick, answer);
        TestCompletion(packs, quick, answer);
        Assert(session.IsUnlocked(SudokuDifficulty.Quick) && !session.IsUnlocked(SudokuDifficulty.Beginner), "initial lock state");
        Throws(() => session.BeginLevel(SudokuDifficulty.Intermediate, 0), "locked tier blocked in model");
        session.BeginLevel(SudokuDifficulty.Quick, 0);
        Assert(!session.SetDigit(0, 1), "givens immutable");
        int empty = Array.IndexOf(quick, (byte)0);
        session.SetDigit(empty, 3, true);
        Assert(session.Value(empty) == 0 && session.Notes(empty) == (1 << 3), "notes do not fill cells");
        session.SetDigit(empty, 3, true);
        Assert(session.Notes(empty) == 0, "notes toggle off");
        session.SetDigit(empty, quick[0]);
        Assert(session.HasConflict(empty), "conflict detected");
        session.Restart();
        Assert(session.Value(empty) == 0 && session.CompletedCount(SudokuDifficulty.Quick) == 0, "restart gives no credit");
        Complete(session, quick);
        Assert(!session.IsUnlocked(SudokuDifficulty.Beginner), "one completion insufficient");
        session.Restart(); Complete(session, quick);
        Assert(session.CompletedCount(SudokuDifficulty.Quick) == 1, "replay does not double count");
        session.BeginLevel(SudokuDifficulty.Quick, 1); Complete(session, expertSample);
        Assert(session.IsUnlocked(SudokuDifficulty.Beginner) && !session.IsUnlocked(SudokuDifficulty.Intermediate), "two distinct quick unlock beginner only");
        for (int d = 1; d < 6; d++)
        {
            Assert(session.IsUnlocked((SudokuDifficulty)d), "sequential unlock " + d);
            for (int i = 0; i < 2; i++) { session.BeginLevel((SudokuDifficulty)d, i); Complete(session, records[i]); }
        }
        Assert(!new SudokuSession(packs).IsUnlocked(SudokuDifficulty.Beginner), "new session resets progress");
#if !TE_RELEASE
        var debugSession = new SudokuSession(packs);
        debugSession.BeginLevel(SudokuDifficulty.Quick, 0);
        debugSession.DebugUnlockAll();
        Assert(debugSession.IsUnlocked(SudokuDifficulty.Master), "GM can unlock all tiers");
        Assert(debugSession.CompletedCount(SudokuDifficulty.Master) == 0 && debugSession.CompletedCount(SudokuDifficulty.Quick) == 0, "GM unlock does not forge completions");
        debugSession.DebugComplete(); debugSession.DebugComplete();
        Assert(debugSession.IsComplete && debugSession.CompletedCount(SudokuDifficulty.Quick) == 1, "GM complete is idempotent");
        Assert(!new SudokuSession(packs).IsUnlocked(SudokuDifficulty.Master), "GM override does not survive reset");
#endif
        if (directory == null) return;
        var seen = new HashSet<string>(); var timer = new Stopwatch(); double slowest = 0; int total = 0;
        for (int d = 0; d < 6; d++)
        {
            string file = Path.Combine(directory, $"sudoku_{((SudokuDifficulty)d).ToString().ToLowerInvariant()}.bytes");
            byte[] bytes = File.ReadAllBytes(file); var bank = SudokuPack.Read(bytes);
            Assert((int)bank.Difficulty == d && bank.Count >= 2, "difficulty pack " + d);
            for (int i = 0; i < bank.Count; i++)
            {
                var puzzle = bank.GetPuzzle(i);
                Assert(seen.Add(string.Concat(puzzle)), "no duplicate puzzle");
                timer.Restart(); int n = SudokuSolver.Solve(puzzle, out var solution); timer.Stop(); slowest = Math.Max(slowest, timer.Elapsed.TotalMilliseconds);
                Assert(n == 1, "every puzzle has unique solution");
                Assert(IndependentCount(puzzle) == 1, "independent uniqueness check");
                var rating = SudokuRater.Rate(puzzle);
                Assert(rating.Solved && rating.FinalBoard.SequenceEqual(solution) && (int)rating.Difficulty == d, "logical grading matches pack");
                if (d >= 2) Assert(!SudokuRater.Rate(puzzle, d - 1).Solved, "lower tier genuinely stuck");
                total++;
            }
            Console.WriteLine($"{(SudokuDifficulty)d}: {bank.Count} puzzles, {bytes.Length} bytes");
        }
        Console.WriteLine($"Validated {total} puzzles; slowest solve+uniqueness={slowest:F3}ms (.NET desktop, not device benchmark)");
    }

    private static void Complete(SudokuSession session, byte[] puzzle)
    {
        SudokuSolver.Solve(puzzle, out var solution);
        for (int i = 0; i < 81; i++) if (!session.IsGiven(i)) session.SetDigit(i, solution[i]);
        Assert(session.IsComplete, "completed board recognized");
    }

    // Independent straightforward backtracking: recomputes candidate sets directly from peer values.
    private static int IndependentCount(byte[] input)
    {
        var b = (byte[])input.Clone(); int found = 0;
        void Search()
        {
            int cell = -1, mask = 0, minimum = 10;
            for (int i = 0; i < 81; i++) if (b[i] == 0)
            {
                int m = SudokuBoard.Candidates(b, i), n = SudokuBoard.CountBits(m);
                if (n == 0) return;
                if (n < minimum) { minimum = n; cell = i; mask = m; }
            }
            if (cell == -1) { found++; return; }
            for (int d = 1; d <= 9 && found < 2; d++) if ((mask & (1 << d)) != 0) { b[cell] = (byte)d; Search(); b[cell] = 0; }
        }
        Search(); return found;
    }
}

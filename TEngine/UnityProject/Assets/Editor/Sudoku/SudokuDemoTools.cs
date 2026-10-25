#if UNITY_EDITOR
using System;
using System.IO;
using GameLogic.Sudoku;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SudokuEditor
{
    [InitializeOnLoad]
    public sealed class SudokuDemoTools : EditorWindow
    {
        public const string GamePrefabPath = "Assets/AssetRaw/UI/Sudoku/UISudoku.prefab";
        private const string PackDirectory = "Assets/AssetRaw/Configs/Sudoku";
        public const string FontPath = "Assets/AssetRaw/Fonts/GeourceSansCHS-Medium SDF.asset";
        private const string PreviousScene = "TEngine.SudokuDemo.PreviousStartScene";
        private const string PreviousMode = "TEngine.SudokuDemo.PreviousResourceMode";
        private const string PlayRequestKey = "TEngine.SudokuDemo.PlayRequested";
        private int _count = 20, _seed = 20261022, _attempts = 100000;

        static SudokuDemoTools()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(PlayRequestKey, false)) return;
                SessionState.EraseBool(PlayRequestKey);
                string previous = SessionState.GetString(PreviousScene, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                EditorPrefs.SetInt("EditorPlayMode", SessionState.GetInt(PreviousMode, 0));
            };
        }

        [MenuItem("Tools/数独/开始试玩")]
        public static void PlayDemo() => StartGame(true);

        internal static void PlayForValidation() => StartGame(false);

        private static void StartGame(bool askToSaveScene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("请先停止当前运行，再启动数独试玩。"); return; }
            if (EditorApplication.isCompiling) { Debug.LogWarning("脚本正在编译，请编译完成后启动数独试玩。"); return; }
            if (askToSaveScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ValidateUIAssets();
            ValidatePacks();
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/main.unity");
            if (scene == null) throw new FileNotFoundException("找不到启动场景 Assets/Scenes/main.unity。");
            SessionState.SetString(PreviousScene, EditorSceneManager.playModeStartScene == null ? "" : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetInt(PreviousMode, EditorPrefs.GetInt("EditorPlayMode", 0));
            // Use editor simulation to load these new local assets without rebuilding old bundles.
            EditorPrefs.SetInt("EditorPlayMode", 0);
            SessionState.SetBool(PlayRequestKey, true);
            EditorSceneManager.playModeStartScene = scene;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Tools/数独/关卡工具")]
        private static void Open() => GetWindow<SudokuDemoTools>("数独关卡工具");

        public static void ValidateUIAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null) throw new FileNotFoundException("缺少 GeourceSansCHS-Medium SDF，请通过 Language.xlsx 导表生成。");
            var game = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabPath);
            if (game == null) throw new InvalidOperationException("请先创建并保存 UISudoku 正式预制体。");
            if (game.transform.Find("Panel/Layout/Board/m_rect_Board/SudoCell") == null || game.transform.Find("Panel/Layout/Bottom/Layout/Num1") == null)
                throw new InvalidOperationException("UISudoku 缺少棋盘模板或 Num1。");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRaw/UI/Sudoku/SudoCell.prefab") == null) throw new InvalidOperationException("SudoCell 预制体缺失。");
            foreach (string name in new[] { "UIGM", "UIShowGM", "TopBtn_GM", "GmGroup", "BtnAndInput_GM" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetRaw/UI/GM/" + name + ".prefab") == null) throw new InvalidOperationException("GM 预制体缺失: " + name);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("数独关卡包", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("每关只存 81 格题面（41 字节）。答案进关时计算。生成结果会经过唯一解和六档技巧评级。", MessageType.Info);
            _count = Math.Max(2, EditorGUILayout.IntField("每档题数", _count));
            _seed = EditorGUILayout.IntField("随机种子", _seed);
            _attempts = Math.Max(1, EditorGUILayout.IntField("最大终盘尝试次数", _attempts));
            EditorGUILayout.LabelField("输出目录", PackDirectory);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("生成六档关卡包")) Generate();
                if (GUILayout.Button("检查现有关卡包")) ValidatePacks();
                if (GUILayout.Button("开始试玩")) PlayDemo();
            }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("快速默认开放。其他档位需要前一档完成 2 道不同题；重复通关不累计。进度、道具和主题自动保存。", MessageType.None);
        }

        private void Generate()
        {
            try
            {
                var banks = SudokuGenerator.Generate(_count, _seed, _attempts, (attempt, counts) =>
                {
                    int total = 0; foreach (int count in counts) total += count;
                    if (EditorUtility.DisplayCancelableProgressBar("生成数独关卡", "终盘尝试 " + attempt + "，六档题数 " + string.Join(",", counts), total / (6f * _count))) throw new OperationCanceledException();
                });
                Directory.CreateDirectory(PackDirectory);
                AssetDatabase.StartAssetEditing();
                try
                {
                    for (int d = 0; d < 6; d++) File.WriteAllBytes(Path.Combine(PackDirectory, "sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes"), SudokuPack.Write((SudokuDifficulty)d, banks[d]));
                }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.Refresh();
                ValidatePacks();
            }
            catch (OperationCanceledException) { Debug.Log("已取消生成，原关卡包未改动。"); }
            catch (Exception e) { Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Tools/数独/检查关卡包")]
        public static void ValidatePacks()
        {
            int total = 0, bytes = 0;
            for (int d = 0; d < 6; d++)
            {
                string path = Path.Combine(PackDirectory, "sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes");
                var data = File.ReadAllBytes(path); var pack = SudokuPack.Read(data);
                if ((int)pack.Difficulty != d || pack.Count < 2) throw new InvalidDataException("难度或数量错误: " + path);
                for (int i = 0; i < pack.Count; i++)
                {
                    byte[] puzzle = pack.GetPuzzle(i);
                    if (SudokuSolver.Solve(puzzle, out _) != 1) throw new InvalidDataException("题目没有唯一解: " + path + " #" + i);
                    var rating = SudokuRater.Rate(puzzle);
                    if (!rating.Solved || (int)rating.Difficulty != d || d >= 2 && SudokuRater.Rate(puzzle, d - 1).Solved) throw new InvalidDataException("评级不符合: " + path + " #" + i);
                }
                total += pack.Count; bytes += data.Length;
            }
            Debug.Log("[Sudoku] 检查通过：" + total + " 道题，关卡包共 " + bytes + " 字节；全部唯一解且符合六档评级。");
        }
    }
}
#endif

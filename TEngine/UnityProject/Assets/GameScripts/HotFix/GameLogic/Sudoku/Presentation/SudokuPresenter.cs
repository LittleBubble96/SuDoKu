using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using GameLogic.Data;

namespace GameLogic.Sudoku
{
    public static class SudokuPresenter
    {
        public static SudokuGameController Current { get; private set; }
        public static GameCacheManager Cache { get; private set; }
        private static GameObject _bridge;
        public static bool IsNavigating { get; private set; }

        public static async UniTask LaunchAsync()
        {
            var token = GameModule.Base.GetCancellationTokenOnDestroy();
            var tasks = new UniTask<SudokuPack>[6];
            for (int d = 0; d < 6; d++) tasks[d] = LoadPackAsync((SudokuDifficulty)d, token);
            var packs = await UniTask.WhenAll(tasks);
            Cache = new GameCacheManager();
            await Cache.LoadAsync();
            await ConfigSystem.Instance.Tables.TbTrophyAsync();
            Current = new SudokuGameController(packs, Cache, trophyConfigs: ConfigSystem.Instance.Tables.TbTrophy.DataList);
            SudokuThemeManager.Initialize(Cache, Current.SaveGameProgress);
            ApplyAudioSettings();
            _bridge = new GameObject("[SudokuAppBridge]");
            UnityEngine.Object.DontDestroyOnLoad(_bridge);
            _bridge.AddComponent<SudokuAppBridge>();
            Current.SetWindowActive(false);
            var home = await GameModule.UI.ShowUIAsyncAwait<UIHome>();
            if (home == null) throw new InvalidOperationException("首页加载失败。");
            await home.WaitForContentAsync(token);
#if !TE_RELEASE
            await GMSingle.Instance.InitializeAsync(Current);
#endif
        }

        public static async UniTask ShowHomeAsync()
        {
            if (IsNavigating || Current == null || Current.IsAdShowing) return;
            var game = Current;
            var token = GameModule.Base.GetCancellationTokenOnDestroy();
            bool wasActive = game.IsWindowActive;
            IsNavigating = true;
            using (UIInteractionLock.Acquire())
            {
                game.SetWindowActive(false);
                try
                {
                    var home = await GameModule.UI.ShowUIAsyncAwait<UIHome>();
                    if (home == null) throw new InvalidOperationException("首页加载失败。");
                    if (game.IsDaily) home.SelectDailyDate(game.DailyDate);
                    else home.SelectTab(0);
                    await home.WaitForContentAsync(token);
                    token.ThrowIfCancellationRequested();
                    GameModule.UI.CloseUI<UISudoku>();
                }
                catch
                {
                    if (!token.IsCancellationRequested)
                    {
                        GameModule.UI.CloseUI<UIHome>();
                        game.SetWindowActive(wasActive);
                    }
                    throw;
                }
                finally { IsNavigating = false; }
            }
        }

        public static UniTask ContinueGameAsync() => OpenGameAsync(null, null);
        public static UniTask StartNewGameAsync(SudokuDifficulty difficulty) => OpenGameAsync(difficulty, null);
        public static UniTask StartDailyChallengeAsync(DateTime date) => OpenGameAsync(null, date.Date);

        private static async UniTask OpenGameAsync(SudokuDifficulty? difficulty, DateTime? dailyDate)
        {
            if (IsNavigating || Current == null || Current.IsAdShowing) return;
            var game = Current;
            if (dailyDate.HasValue)
            {
                if (!game.CanOpenDaily(dailyDate.Value)) return;
            }
            else if (difficulty.HasValue)
            {
                if (!game.Session.IsUnlocked(difficulty.Value)) return;
            }
            else if (!game.HasNormalGame) return;
            var token = GameModule.Base.GetCancellationTokenOnDestroy();
            IsNavigating = true;
            using (UIInteractionLock.Acquire())
            {
                try
                {
                    var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                    int homeVersion = home?.OpenVersion ?? 0;
                    var window = await GameModule.UI.ShowUIAsyncAwait<UISudoku>(new SudokuContext(game));
                    token.ThrowIfCancellationRequested();
                    if (window == null) throw new InvalidOperationException("数独界面加载失败。");
                    if (home != null && (!home.IsOpen || home.OpenVersion != homeVersion))
                    {
                        GameModule.UI.CloseUI<UISudoku>();
                        return;
                    }
                    await UniTask.WaitUntil(() => window.IsBoardReady, cancellationToken: token);
                    // 资源和棋盘准备成功后才替换旧对局；加载失败不会丢失旧题。
                    bool opened = dailyDate.HasValue ? game.OpenDailyChallenge(dailyDate.Value) :
                        difficulty.HasValue ? game.StartNewGame(difficulty.Value) : game.ContinueNormalGame();
                    if (!opened)
                    {
                        GameModule.UI.CloseUI<UISudoku>();
                        return;
                    }
                    var popup = await GameModule.UI.GetUIAsyncAwait<UIHomeNewGame>(token);
                    if (popup != null && popup.IsOpen)
                    {
                        GameModule.UI.HideUI<UIHomeNewGame>();
                        await UniTask.WaitUntil(() => !popup.IsOpen, cancellationToken: token);
                    }
                    GameModule.UI.CloseUI<UIHome>();
                    // 从首页继续时解除此前局内的暂停，保留题面、笔记、撤销和用时。
                    game.SetWindowActive(true);
                    if (game.IsPaused) game.TogglePause();
                    GameEvent.Send(EventDefine.SudokuStateChanged);
                }
                catch
                {
                    if (!token.IsCancellationRequested) GameModule.UI.CloseUI<UISudoku>();
                    throw;
                }
                finally { IsNavigating = false; }
            }
        }

        public static void SaveOnQuit()
        {
            Current?.SaveGameProgress();
            Cache?.FlushOnQuit();
        }
        public static void Release()
        {
            SaveOnQuit();
            AdSystem.Dispose();
            SudokuThemeManager.Release();
            if (_bridge != null) UnityEngine.Object.Destroy(_bridge);
            _bridge = null; Current = null; Cache = null; IsNavigating = false;
        }
        public static void SetAudioVolumes(float music, float sound)
        {
            if (Cache == null) return;
            Cache.CacheData.gameSettingsData.MusicVolume = Mathf.Clamp01(music);
            Cache.CacheData.gameSettingsData.SoundVolume = Mathf.Clamp01(sound);
            ApplyAudioSettings(); Current.SaveGameProgress();
        }
        private static void ApplyAudioSettings()
        {
            AudioSystem.Instance.SetMusicVolume(Cache.CacheData.gameSettingsData.MusicVolume);
            AudioSystem.Instance.SetSoundVolume(Cache.CacheData.gameSettingsData.SoundVolume);
        }

        private static async UniTask<SudokuPack> LoadPackAsync(SudokuDifficulty difficulty, System.Threading.CancellationToken token)
        {
            string location = "sudoku_" + difficulty.ToString().ToLowerInvariant();
            TextAsset asset = null;
            try
            {
                asset = await GameModule.Resource.LoadAssetAsync<TextAsset>(location, token);
                if (asset == null) throw new InvalidOperationException("无法加载关卡包: " + location);
                var pack = SudokuPack.Read(asset.bytes);
                if (pack.Difficulty != difficulty) throw new InvalidOperationException("关卡包难度不匹配: " + location);
                return pack;
            }
            finally { if (asset != null) GameModule.Resource.UnloadAsset(asset); }
        }
    }

    public sealed class SudokuContext
    {
        public readonly SudokuGameController Game;
        public SudokuContext(SudokuGameController game) { Game = game; }
    }
}

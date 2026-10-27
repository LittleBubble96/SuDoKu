using System;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic.Data
{
    /// <summary>沿用参考工程的 CacheData / JSON / 平台文件系统结构。</summary>
    public sealed class GameCacheManager
    {
        public GameCacheData CacheData { get; private set; } = new GameCacheData();
        private readonly IReadCacheFileSystem _fileSystem;
        private string _pendingJson;
        private bool _writing, _stopping;
        public GameCacheManager(IReadCacheFileSystem fileSystem = null)
        {
            _fileSystem = fileSystem ?? GenerateFileSystem();
        }
        private static IReadCacheFileSystem GenerateFileSystem()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new WeChatReadCacheFileSystem();
#else
            return new WindowReadCacheFileSystem();
#endif
        }
        public async UniTask LoadAsync()
        {
            try
            {
                string json = await _fileSystem.ReadCacheAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    // 在默认数据上覆盖，旧缓存缺少的新开关仍保留默认值。
                    var data = new GameCacheData();
                    JsonUtility.FromJsonOverwrite(json, data);
                    if (data != null && data.version == GameCacheData.CurrentVersion) CacheData = data;
                }
            }
            catch (Exception error) { Log.Warning("[GameCache] 读取失败，使用初始数据: " + error.Message); }
            CacheData.InitAll();
        }
        public void Save()
        {
            if (_stopping) return;
            _pendingJson = JsonUtility.ToJson(CacheData);
            if (!_writing) WritePendingAsync().Forget();
        }
        private async UniTaskVoid WritePendingAsync()
        {
            _writing = true;
            try
            {
                while (_pendingJson != null && !_stopping)
                {
                    string json = _pendingJson; _pendingJson = null;
                    await _fileSystem.WriteCacheAsync(json);
                }
            }
            catch (Exception error) { Log.Error("[GameCache] 保存失败: " + error.Message); }
            finally { _writing = false; }
        }
        public async UniTask FlushAsync()
        {
            Save();
            await UniTask.WaitUntil(() => !_writing);
        }
        public void FlushOnQuit()
        {
            if (_stopping) return;
            _stopping = true; _pendingJson = null;
            try { _fileSystem.FlushOnQuit(JsonUtility.ToJson(CacheData)); }
            catch (Exception error) { Log.Error("[GameCache] 退出保存失败: " + error.Message); }
        }
    }
}

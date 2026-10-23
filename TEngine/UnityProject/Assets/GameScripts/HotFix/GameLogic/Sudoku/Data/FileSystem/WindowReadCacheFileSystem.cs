using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public sealed class WindowReadCacheFileSystem : IReadCacheFileSystem
    {
        private readonly string _path = Path.Combine(Application.persistentDataPath, "game_cache.json");
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public async UniTask<string> ReadCacheAsync()
        {
            if (!File.Exists(_path)) return null;
            return await File.ReadAllTextAsync(_path, Encoding.UTF8);
        }

        public async UniTask WriteCacheAsync(string json)
        {
            // 不依赖 Unity 主线程 continuation，退出时等待锁也不会死锁。
            await WriteFileAsync(json);
        }

        private async System.Threading.Tasks.Task WriteFileAsync(string json)
        {
            await _writeLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await File.WriteAllTextAsync(_path + ".tmp", json, Encoding.UTF8).ConfigureAwait(false);
                Commit();
            }
            finally { _writeLock.Release(); }
        }

        private void Commit()
        {
            if (File.Exists(_path)) File.Replace(_path + ".tmp", _path, null);
            else File.Move(_path + ".tmp", _path);
        }

        public void FlushOnQuit(string json)
        {
            _writeLock.Wait();
            try { File.WriteAllText(_path + ".tmp", json, Encoding.UTF8); Commit(); }
            finally { _writeLock.Release(); }
        }
    }
}

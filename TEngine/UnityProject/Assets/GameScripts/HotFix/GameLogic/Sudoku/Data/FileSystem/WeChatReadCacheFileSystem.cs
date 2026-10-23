using Cysharp.Threading.Tasks;
using WeChatWASM;

namespace GameLogic
{
    public sealed class WeChatReadCacheFileSystem : IReadCacheFileSystem
    {
        private const string GameKey = "gameCache";
        public UniTask<string> ReadCacheAsync() => UniTask.FromResult(WXBase.StorageGetStringSync(GameKey, ""));
        public UniTask WriteCacheAsync(string json)
        {
            // 与参考项目保持一致：微信 Storage 同步提交，onHide 返回前即可落盘。
            WXBase.StorageSetStringSync(GameKey, json);
            return UniTask.CompletedTask;
        }
        public void FlushOnQuit(string json) => WXBase.StorageSetStringSync(GameKey, json);
    }
}

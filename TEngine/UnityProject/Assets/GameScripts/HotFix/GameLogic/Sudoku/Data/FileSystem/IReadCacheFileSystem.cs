using Cysharp.Threading.Tasks;

namespace GameLogic
{
    public interface IReadCacheFileSystem
    {
        UniTask<string> ReadCacheAsync();
        UniTask WriteCacheAsync(string json);
        // 退出回调不能等待下一帧；最终刷新必须在返回前完成。
        void FlushOnQuit(string json);
    }
}

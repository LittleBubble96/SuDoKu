using System;

namespace GameLogic.Data
{
    /// <summary>仅保存当天任务，跟随 GameCacheData 使用 Unity/WX 缓存。</summary>
    [Serializable]
    public sealed class DailyTaskData
    {
        public string date;
        public bool freeClaimed;
        public bool playCompleted, challengeCompleted;
        public bool playClaimed, challengeClaimed;
    }
}

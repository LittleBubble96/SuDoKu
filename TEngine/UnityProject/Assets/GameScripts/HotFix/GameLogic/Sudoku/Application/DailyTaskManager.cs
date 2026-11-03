using System;
using GameLogic.Data;
using TEngine;

namespace GameLogic.Sudoku
{
    public enum DailyTaskType { PlayOnce, DailyChallenge }

    public readonly struct DailyTaskReward
    {
        public readonly int Coins, Tips;
        public DailyTaskReward(int coins, int tips) { Coins = coins; Tips = tips; }
    }

    /// <summary>任务完成与领取规则，不操作 UI，也不更新视觉金币。</summary>
    public sealed class DailyTaskManager
    {
        public const int FreeCoins = 50, FreeTips = 1, PlayOnceCoins = 50, ChallengeTips = 1;
        private readonly GameCacheData _cache;
        private readonly Func<DateTime> _today;
        private readonly Action _save;

        public DailyTaskManager(GameCacheData cache, Func<DateTime> today, Action save)
        {
            _cache = cache; _today = today; _save = save;
        }

        public string TodayKey => SudokuDailyRules.DateKey(_today().Date);
        public DailyTaskData Data { get { RefreshDay(); return _cache.dailyTaskData; } }

        public void RefreshDay()
        {
            var data = _cache.dailyTaskData;
            string today = TodayKey;
            if (data.date == today) return;
            data.date = today;
            data.freeClaimed = data.playCompleted = data.challengeCompleted = false;
            data.playClaimed = data.challengeClaimed = false;
        }

        // 由对局控制器在玩家通关时调用，与棋盘及统计在同一次快照中保存。
        public void RecordCompletion(bool daily, DateTime challengeDate)
        {
            var data = Data;
            data.playCompleted = true;
            if (daily && challengeDate.Date == _today().Date) data.challengeCompleted = true;
        }

        public bool IsCompleted(DailyTaskType type) =>
            type == DailyTaskType.PlayOnce ? Data.playCompleted : Data.challengeCompleted;
        public bool IsClaimed(DailyTaskType type) =>
            type == DailyTaskType.PlayOnce ? Data.playClaimed : Data.challengeClaimed;

        public bool TryClaimFree(bool doubled, out DailyTaskReward reward, string expectedDate = null)
        {
            reward = default;
            var data = Data;
            // 广告跨零点返回时，不能把昨日的请求兑换成今日的奖励。
            if (data.freeClaimed || expectedDate != null && expectedDate != data.date) return false;
            data.freeClaimed = true;
            int multiplier = doubled ? 2 : 1;
            reward = new DailyTaskReward(FreeCoins * multiplier, FreeTips * multiplier);
            Grant(reward);
            return true;
        }

        public bool TryClaimTask(DailyTaskType type, out DailyTaskReward reward)
        {
            reward = default;
            if (!IsCompleted(type) || IsClaimed(type)) return false;
            var data = Data;
            if (type == DailyTaskType.PlayOnce)
            {
                data.playClaimed = true;
                reward = new DailyTaskReward(PlayOnceCoins, 0);
            }
            else
            {
                data.challengeClaimed = true;
                reward = new DailyTaskReward(0, ChallengeTips);
            }
            Grant(reward);
            return true;
        }

        private void Grant(DailyTaskReward reward)
        {
            _cache.gamePropData.coinCount += reward.Coins;
            _cache.gamePropData.tipCount += reward.Tips;
            _save();
            GameEvent.Send(EventDefine.SudokuStateChanged);
        }
    }
}

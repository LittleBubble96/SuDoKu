using System;
using GameLogic.Data;
using TEngine;

namespace GameLogic.Sudoku
{
    public readonly struct SignInReward
    {
        public readonly bool IsCoin;
        public readonly int Amount;
        public SignInReward(bool isCoin, int amount) { IsCoin = isCoin; Amount = amount; }
        public SignInReward Multiply(int multiplier) => new SignInReward(IsCoin, Amount * multiplier);
        public string AmountText => IsCoin ? Amount.ToString() : "× " + Amount;
    }

    /// <summary>累计签到，不因漏签重置；每七次领取循环，同一自然日只能领取一次。</summary>
    public sealed class DailySignInManager
    {
        public const int CycleDays = 7;
        private static readonly SignInReward[] Rewards =
        {
            new SignInReward(true, 20), new SignInReward(true, 30), new SignInReward(true, 50),
            new SignInReward(false, 1), new SignInReward(true, 80), new SignInReward(true, 100),
            new SignInReward(false, 2)
        };
        private readonly GameCacheManager _cache;
        private readonly Func<DateTime> _today;
        public DailySignInManager(GameCacheManager cache, Func<DateTime> today)
        {
            _cache = cache;
            _today = today;
        }
        private string TodayKey => SudokuDailyRules.DateKey(_today());
        public int TotalDays => _cache.CacheData.signInDays;
        public bool ClaimedToday => _cache.CacheData.signInDate == TodayKey;
        public int CompletedInCycle => ClaimedToday ? (TotalDays - 1) % CycleDays + 1 : TotalDays % CycleDays;
        public int DisplayDay => ClaimedToday ? CompletedInCycle : CompletedInCycle + 1;
        public static SignInReward GetReward(int day) => Rewards[day - 1];

        public bool TryClaim(bool doubled, out SignInReward reward)
        {
            reward = default;
            if (ClaimedToday) return false;
            reward = GetReward(DisplayDay).Multiply(doubled ? 2 : 1);
            var data = _cache.CacheData;
            if (reward.IsCoin) data.gamePropData.coinCount += reward.Amount;
            else data.gamePropData.tipCount += reward.Amount;
            data.signInDays++;
            data.signInDate = TodayKey;
            // 奖励、进度和当天标记在同一份快照内保存。
            _cache.Save();
            GameEvent.Send(EventDefine.SudokuStateChanged);
            return true;
        }
    }
}

using System;
using DG.Tweening;
using GameLogic.Data;
using TEngine;

namespace GameLogic.Sudoku
{
    /// <summary>逻辑金币仍在 GamePropData；视觉金币只在显式刷新或增长动画时更新，不存档。</summary>
    public sealed class CoinViewManager : IDisposable
    {
        public const float CountDuration = .55f;
        private readonly GamePropData _props;
        private Tween _countTween;
        public int VisualCoins { get; private set; }
        public int LogicalCoins => _props.coinCount;
        public bool IsAnimating => _countTween != null;

        public CoinViewManager(GamePropData props) { _props = props; VisualCoins = props.coinCount; }

        public void RefreshView()
        {
            Stop();
            VisualCoins = LogicalCoins;
            // 缓存窗口重新打开时，即使数值未变，也要刷新关闭期间未收到事件的文本。
            GameEvent.Send(EventDefine.CoinViewChanged);
        }

        public void PlayGainAnimation()
        {
            Stop();
            int target = LogicalCoins;
            if (target <= VisualCoins) { SetVisual(target); return; }
            GameEvent.Send(EventDefine.CoinViewAnimationStarted);
            // 连续领奖从正在显示的数量继续，目标始终是最新逻辑金币。
            _countTween = DOVirtual.Float(VisualCoins, target, CountDuration, value => SetVisual((int)Math.Round(value)))
                .SetEase(DG.Tweening.Ease.OutCubic).SetUpdate(true)
                .OnComplete(() => { _countTween = null; SetVisual(target); });
        }

        private void SetVisual(int value)
        {
            if (VisualCoins == value) return;
            VisualCoins = value;
            GameEvent.Send(EventDefine.CoinViewChanged);
        }

        private void Stop() { _countTween?.Kill(false); _countTween = null; }
        public void Dispose() => Stop();
    }
}

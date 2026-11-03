using System;
using DG.Tweening;
using UnityEngine;

namespace GameLogic
{
    /// <summary>居中弹窗共用节奏：根 CanvasGroup 淡入淡出，Dialog 弹性缩放。</summary>
    internal sealed class UIPopupAnimation
    {
        // 参考帧按 60 FPS 换算；使用独立时间，不受游戏暂停影响。
        public const float FrameTime = 1f / 60f;
        public const float InDuration = FrameTime * 15f;
        public const float SettleDuration = FrameTime * 10f;
        public const float OutDuration = FrameTime * 8f;
        public const float OutScaleDuration = FrameTime * 5f;
        private readonly CanvasGroup _canvas;
        private readonly UnityEngine.UI.Image _back;
        private readonly float _blackAlpha;
        private readonly RectTransform _dialog;
        private readonly Vector3 _shownScale;
        private Sequence _sequence;

        public UIPopupAnimation(CanvasGroup canvas, RectTransform dialog)
        {
            _canvas = canvas;
            _back = canvas.GetComponent<UnityEngine.UI.Image>();
            _blackAlpha = _back.color.a;
            _dialog = dialog;
            _shownScale = dialog.localScale;
        }

        public void PlayIn(Action completed)
        {
            Kill();
            _canvas.alpha = 0f;
            SetBackAlpha(0f);
            _dialog.localScale = _shownScale * .6f;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(_canvas.gameObject);
            _sequence.Append(_back.DOFade(_blackAlpha, InDuration).SetEase(Ease.InOutSine));
            _sequence.Join(_canvas.DOFade(1f, InDuration).SetEase(Ease.InOutSine));
            _sequence.Join(_dialog.DOScale(_shownScale * 1.03f, InDuration).SetEase(Ease.OutSine));
            _sequence.Append(_dialog.DOScale(_shownScale, SettleDuration).SetEase(Ease.InOutSine));
            _sequence.OnComplete(() => { _sequence = null; completed(); });
        }

        public void PlayOut(Action completed)
        {
            Kill();
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(_canvas.gameObject);
            _sequence.Append(_back.DOFade(0f, OutDuration).SetEase(Ease.InOutSine));
            _sequence.Join(_canvas.DOFade(0f, OutScaleDuration).SetEase(Ease.InQuad));
            _sequence.Join(_dialog.DOScale(_shownScale * .8f, OutScaleDuration).SetEase(Ease.InQuad));
            _sequence.OnComplete(() => { _sequence = null; completed(); });
        }

        public void Reset()
        {
            Kill();
            // 直接关闭、销毁和缓存重开也要恢复初值，避免残留透明度及缩放。
            if (_canvas != null) _canvas.alpha = 1f;
            if (_back != null) SetBackAlpha(_blackAlpha);
            if (_dialog != null) _dialog.localScale = _shownScale;
        }

        private void SetBackAlpha(float alpha)
        {
            var color = _back.color;
            color.a = alpha;
            _back.color = color;
        }

        private void Kill()
        {
            _sequence?.Kill(false);
            _sequence = null;
        }
    }
}

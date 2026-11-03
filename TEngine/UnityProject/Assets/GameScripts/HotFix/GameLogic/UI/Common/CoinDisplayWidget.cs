using DG.Tweening;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>绑定任意金币 TMP 文本，共用视觉数量及金币栏缩放节奏。</summary>
    public sealed class CoinDisplayWidget : UIWidget
    {
        private TMP_Text _number;
        private RectTransform _animationRoot;
        private Vector3 _normalScale;
        private Tween _pulse;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _number = gameObject.GetComponent<TMP_Text>();
            SetAnimationRoot(_number.rectTransform);
        }

        public void SetAnimationRoot(RectTransform root)
        {
            StopPulse();
            _animationRoot = root;
            _normalScale = root.localScale;
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.CoinViewChanged, RefreshDisplay);
            AddUIEvent(EventDefine.CoinViewAnimationStarted, Pulse);
        }

        protected override void OnRefresh() { base.OnRefresh(); RefreshDisplay(); }
        private void RefreshDisplay() => _number.text = SudokuPresenter.Coins.VisualCoins.ToString();

        private void Pulse()
        {
            if (!gameObject.activeInHierarchy) return;
            StopPulse();
            _pulse = _animationRoot.DOPunchScale(Vector3.one * .08f, .4f, 1, .5f)
                .SetUpdate(true).SetLink(gameObject);
        }

        private void StopPulse()
        {
            _pulse?.Kill(false); _pulse = null;
            if (_animationRoot != null) _animationRoot.localScale = _normalScale;
        }
        protected override void OnClose() { StopPulse(); base.OnClose(); }
        protected override void OnDestroy() { StopPulse(); base.OnDestroy(); }
    }
}

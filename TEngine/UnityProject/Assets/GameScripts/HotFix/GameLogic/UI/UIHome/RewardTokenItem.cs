using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>复用 UIReward 的 token 模板，布局占位不动，仅 root 做飞行动画。</summary>
    public sealed class RewardTokenItem : UIWidget
    {
        private TMP_Text _number, _name;
        private UnityEngine.UI.Image _icon;
        private Sprite _sprite;
        private CanvasGroup _group;
        private bool _coin;
        public RectTransform MotionRoot { get; private set; }
        public bool IconReady => _sprite != null;
        public float RevealAlpha { get => _group.alpha; set => _group.alpha = value; }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            MotionRoot = FindChildComponent<RectTransform>("root");
            _group = MotionRoot.GetComponent<CanvasGroup>();
            if (_group == null) _group = MotionRoot.gameObject.AddComponent<CanvasGroup>();
            _number = FindChildComponent<TMP_Text>("root/m_textNum");
            _name = FindChildComponent<TMP_Text>("root/m_text");
            _icon = FindChildComponent<UnityEngine.UI.Image>("root/Icon");
            _icon.preserveAspect = true; _icon.raycastTarget = false;
            UIThemeColor.Bind(_number, SudokuThemeRole.Surface);
            UIThemeColor.Bind(_name, SudokuThemeRole.Surface);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("root/bg"), SudokuThemeRole.Accent);
        }
        public void Init(bool coin, int amount)
        {
            _coin = coin; _number.text = amount.ToString();
            RefreshLanguage(); ResetMotion();
        }
        public void RefreshLanguage() => _name.text = LocalizationHelper.GetLocalText(_coin ? LanguageKey.reward_coin : LanguageKey.sudoku_ui_hint);
        public async UniTask LoadIconAsync(CancellationToken token)
        {
            if (_sprite != null) return;
            Sprite asset = null;
            try
            {
                asset = await GameModule.Resource.LoadAssetAsync<Sprite>(_coin ? "Sudoku_icon_reward_coin_star_gold" : "Sudoku_icon_reward_hint_gold", token);
                token.ThrowIfCancellationRequested();
                if (asset == null) throw new System.InvalidOperationException("奖励图标加载失败。");
                _sprite = asset; asset = null; _icon.sprite = _sprite; _icon.color = Color.white;
            }
            finally { if (asset != null) GameModule.Resource.UnloadAsset(asset); }
        }
        public void ResetMotion()
        {
            MotionRoot.anchoredPosition = Vector2.zero; MotionRoot.localScale = Vector3.one;
            _group.alpha = 0; _number.gameObject.SetActive(false); _name.gameObject.SetActive(false);
        }
        public void FinishReveal()
        {
            MotionRoot.anchoredPosition = Vector2.zero; MotionRoot.localScale = Vector3.one; _group.alpha = 1;
            _number.gameObject.SetActive(true); _name.gameObject.SetActive(true);
        }
        protected override void OnDestroy()
        {
            if (_sprite != null) GameModule.Resource.UnloadAsset(_sprite);
            _sprite = null; base.OnDestroy();
        }
    }
}

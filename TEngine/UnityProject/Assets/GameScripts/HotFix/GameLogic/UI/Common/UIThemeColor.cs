using GameLogic.Sudoku;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>可挂在 Image 或 TMP 上；保留图形本身的透明度，用主题色乘白色散图。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class UIThemeColor : MonoBehaviour
    {
        [SerializeField] private SudokuThemeRole _role = SudokuThemeRole.Text;
        [SerializeField] private bool _preserveAlpha = true;
        private Graphic _graphic;
        private float _alpha;
        private SudokuThemeRole _overlayRole;
        private float _overlayWeight;
        public SudokuThemeRole Role => _role;

        /// <summary>为代码绑定的控件复用同一变色组件，不重复订阅主题事件。</summary>
        public static UIThemeColor Bind(Graphic graphic, SudokuThemeRole role)
        {
            var binding = graphic.GetComponent<UIThemeColor>();
            if (binding == null) binding = graphic.gameObject.AddComponent<UIThemeColor>();
            binding.SetRole(role);
            return binding;
        }

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _alpha = _graphic.color.a;
        }
        private void OnEnable()
        {
            SudokuThemeManager.Changed += RefreshColor;
            RefreshColor();
        }
        private void OnDisable() => SudokuThemeManager.Changed -= RefreshColor;
        public void SetRole(SudokuThemeRole role)
        {
            if (_role == role) return;
            _role = role; RefreshColor();
        }
        /// <summary>临时叠加主题颜色。权重归零后恢复当前角色，选格和换色仍能实时刷新。</summary>
        public void SetOverlay(SudokuThemeRole role, float weight)
        {
            weight = Mathf.Clamp01(weight);
            if (_overlayRole == role && _overlayWeight == weight) return;
            _overlayRole = role; _overlayWeight = weight;
            RefreshColor();
        }
        public void RefreshColor()
        {
            if (_graphic == null) return;
            Color color = SudokuThemeManager.GetColor(_role);
            if (_overlayWeight > 0) color = Color.Lerp(color, SudokuThemeManager.GetColor(_overlayRole), _overlayWeight);
            if (_preserveAlpha) color.a = _alpha;
            _graphic.color = color;
        }
    }
}

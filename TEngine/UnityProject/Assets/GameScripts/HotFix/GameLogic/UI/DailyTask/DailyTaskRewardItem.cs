using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class DailyTaskRewardItem : UIWidget
    {
        private TMP_Text _number;
        private UnityEngine.UI.Image _icon;
        public Vector3 RewardPosition => _icon.transform.position;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _number = FindChildComponent<TMP_Text>("m_num");
            _icon = FindChildComponent<UnityEngine.UI.Image>("icon");
            _icon.preserveAspect = true;
            UIThemeColor.Bind(_number, SudokuThemeRole.Text);
        }

        public void Init(bool coin, int amount)
        {
            _number.text = coin ? amount.ToString() : "× " + amount;
            _icon.SetSprite(coin ? "Sudoku_coin_gold" : "Sudoku_icon_hint",
                cancellationToken: gameObject.GetCancellationTokenOnDestroy());
            if (!coin) UIThemeColor.Bind(_icon, SudokuThemeRole.Accent);
            else _icon.color = Color.white;
        }
    }
}

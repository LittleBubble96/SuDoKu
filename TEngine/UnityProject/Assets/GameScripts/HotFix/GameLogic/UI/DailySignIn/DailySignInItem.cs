using System;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class DailySignInItem : UIWidget
    {
        private int _day;
        private TMP_Text _title, _amount, _claimedText, _todayText;
        private UnityEngine.UI.Image _icon;
        private GameObject _claimed, _today, _selectedFill;
        private XYButton _button;
        private UIThemeColor _outline;
        public Vector3 RewardPosition => _icon.rectTransform.position;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("bg/m_title");
            _amount = FindChildComponent<TMP_Text>("bg/Reward/m_num");
            _icon = FindChildComponent<UnityEngine.UI.Image>("bg/Reward/icon");
            _claimedText = FindChildComponent<TMP_Text>("bg/state/claimed/m_title");
            _todayText = FindChildComponent<TMP_Text>("bg/state/get/m_title");
            _claimedText.enableAutoSizing = true;
            _claimedText.fontSizeMax = _claimedText.fontSize;
            _claimedText.fontSizeMin = _claimedText.fontSize * .6f;
            _claimed = FindChild("bg/state/claimed").gameObject;
            _today = FindChild("bg/state/get").gameObject;
            _selectedFill = FindChild("SelectedFill").gameObject;
            _outline = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("bg"), SudokuThemeRole.Matching);
            UIThemeColor.Bind(_selectedFill.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Related);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_amount, SudokuThemeRole.Text);
            UIThemeColor.Bind(_claimedText, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_todayText, SudokuThemeRole.Surface);
            Theme("bg/state/claimed/bg", SudokuThemeRole.Matching);
            Theme("bg/state/claimed/check", SudokuThemeRole.MutedText);
            Theme("bg/state/get/bg", SudokuThemeRole.Accent);
            foreach (var graphic in gameObject.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.raycastTarget = false;
            _button = CreateWidget<XYButton>("bg/btn");
            FindChildComponent<UnityEngine.UI.Graphic>("bg/btn").raycastTarget = true;
        }

        public void Init(int day, Action claim)
        {
            _day = day;
            var reward = DailySignInManager.GetReward(day);
            _icon.SetSprite(reward.IsCoin ? "Sudoku_coin_gold" : "Sudoku_icon_hint", cancellationToken: gameObject.GetCancellationTokenOnDestroy());
            if (!reward.IsCoin) UIThemeColor.Bind(_icon, SudokuThemeRole.Accent);
            _button.OnAddListener(() => claim());
        }

        public void RefreshDisplay(DailySignInManager manager, bool busy)
        {
            bool today = !manager.ClaimedToday && _day == manager.DisplayDay;
            _title.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.daily_sign_in_day), _day);
            _amount.text = DailySignInManager.GetReward(_day).AmountText;
            _claimedText.text = LocalizationHelper.GetLocalText(LanguageKey.daily_sign_in_claimed);
            _todayText.text = LocalizationHelper.GetLocalText(LanguageKey.daily_sign_in_today);
            _claimed.SetActive(_day <= manager.CompletedInCycle);
            _today.SetActive(today);
            _selectedFill.SetActive(today);
            _outline.SetRole(today ? SudokuThemeRole.Accent : SudokuThemeRole.Matching);
            _button.Interactable = today && !busy;
        }

        private void Theme(string path, SudokuThemeRole role) =>
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

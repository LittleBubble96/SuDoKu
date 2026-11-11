using System;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class DailyTaskItem : UIWidget
    {
        private TMP_Text _title, _progress, _claimLabel, _goLabel;
        private UnityEngine.UI.Image _fill, _icon;
        private XYButton _claim, _go;
        private UIThemeColor _claimColor, _claimTextColor;
        private DailyTaskType _type;
        private Action<DailyTaskType> _onClaim, _onGo;
        private DailyTaskRewardItem _reward;
        public Vector3 RewardPosition => _reward.RewardPosition;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("m_title");
            _progress = FindChildComponent<TMP_Text>("m_progress");
            _claimLabel = FindChildComponent<TMP_Text>("ClaimBtn/m_text");
            _goLabel = FindChildComponent<TMP_Text>("GoBtn/m_text");
            _icon = FindChildComponent<UnityEngine.UI.Image>("icon");
            _claim = CreateWidget<XYButton>("ClaimBtn");
            _go = CreateWidget<XYButton>("GoBtn");
            _claim.OnAddListener(() => _onClaim(_type));
            _go.OnAddListener(() => _onGo(_type));
            _fill = FindChildComponent<UnityEngine.UI.Image>("progressBg/progressFill");
            _reward = CreateWidget<DailyTaskRewardItem>("RewardItem");
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_progress, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_icon, SudokuThemeRole.Accent);
            UIThemeColor.Bind(_fill, SudokuThemeRole.Accent);
            Theme("bg", SudokuThemeRole.Related);
            Theme("progressBg", SudokuThemeRole.Matching);
            Theme("GoBtn/bg", SudokuThemeRole.Surface);
            Theme("GoBtn/bg/outline", SudokuThemeRole.Accent);
            UIThemeColor.Bind(_goLabel, SudokuThemeRole.Accent);
            _claimColor = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("ClaimBtn/bg"), SudokuThemeRole.Accent);
            _claimTextColor = UIThemeColor.Bind(_claimLabel, SudokuThemeRole.Surface);
            foreach (var text in new[] { _title, _claimLabel, _goLabel })
            {
                text.fontSizeMax = text.fontSize; text.fontSizeMin = text.fontSize * .6f;
                text.enableAutoSizing = true;
            }
        }

        public void Init(DailyTaskType type, Action<DailyTaskType> onClaim, Action<DailyTaskType> onGo)
        {
            _type = type; _onClaim = onClaim; _onGo = onGo;
            bool playOnce = type == DailyTaskType.PlayOnce;
            _reward.Init(playOnce, playOnce ? DailyTaskManager.PlayOnceCoins : DailyTaskManager.ChallengeTips);
            _icon.SetSprite(playOnce ? "Sudoku_icon_sudoku_logo" : "Sudoku_icon_daily_challenge_grid_star",
                cancellationToken: gameObject.GetCancellationTokenOnDestroy());
        }

        public void RefreshDisplay(DailyTaskManager manager, bool busy)
        {
            bool completed = manager.IsCompleted(_type), claimed = manager.IsClaimed(_type);
            _title.text = LocalizationHelper.GetLocalText(_type == DailyTaskType.PlayOnce ?
                LanguageKey.daily_gift_task_play_game : LanguageKey.daily_gift_task_daily_challenge);
            _progress.text = completed ? "1 / 1" : "0 / 1";
            FillProgress(completed ? 1 : 0);
            _claim.Visible = completed;
            _go.Visible = !completed;
            _claim.Interactable = completed && !claimed && !busy;
            _go.Interactable = !busy;
            _claimLabel.text = LocalizationHelper.GetLocalText(claimed ? LanguageKey.daily_sign_in_claimed : LanguageKey.daily_gift_claim);
            _goLabel.text = LocalizationHelper.GetLocalText(LanguageKey.daily_gift_go);
            _claimColor.SetRole(claimed ? SudokuThemeRole.Line : SudokuThemeRole.Accent);
            _claimTextColor.SetRole(claimed ? SudokuThemeRole.MutedText : SudokuThemeRole.Surface);
        }

        private void Theme(string path, SudokuThemeRole role) =>
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);

        private void FillProgress(float progress)
        {
            _fill.rectTransform.sizeDelta = new Vector2(617.26f * Mathf.Clamp01(progress), _fill.rectTransform.sizeDelta.y);
        }
    }
}

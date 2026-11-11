using GameLogic.Localization;
using GameLogic.Sudoku;
using RTLTMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Top, "UIGuide")]
    public sealed class UIGuide : UIWindow
    {
        private const string Dialog = "Panel/Dialog/";
        private const string Row = "desBgHor";
        private const string Column = "desBgHorVer";
        private const string Box = "desBgHorGrid";
        private const string ConfirmButton = "AdClaimBtn";

        private UIPopupAnimation _animation;
        private string _source, _roundId;
        private int _reportedOpen;
        private bool _dismissed;
        private (RTLTextMeshPro Label, string Key)[] _labels;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _animation = new UIPopupAnimation(gameObject.GetComponent<CanvasGroup>(),
                FindChildComponent<RectTransform>("Panel/Dialog"));

            _labels = new[]
            {
                BindLabel("m_title", LanguageKey.setting_how_to_play),
                BindLabel("m_des", LanguageKey.help_fill_blanks),
                BindLabel(Row + "/m_des", LanguageKey.help_each_row),
                BindLabel(Column + "/m_des", LanguageKey.help_each_column),
                BindLabel(Box + "/m_des", LanguageKey.help_each_box),
                BindLabel("m_text", LanguageKey.help_unique_digits),
                BindLabel("m_des1", LanguageKey.help_notes_desc),
                BindLabel("m_des2", LanguageKey.help_hint_desc),
                BindLabel(ConfirmButton + "/m_text", LanguageKey.help_got_it, SudokuThemeRole.Surface)
            };

            BindImage("Bg", SudokuThemeRole.Surface);
            BindImage("CloseBtn/icon", SudokuThemeRole.MutedText);
            BindImage(Row, SudokuThemeRole.Related);
            BindImage(Column, SudokuThemeRole.Related);
            BindImage(Box, SudokuThemeRole.Related);
            foreach (string group in new[] { Row, Column })
            {
                BindImage(group + "/line", SudokuThemeRole.Accent);
                BindImage(group + "/line (1)", SudokuThemeRole.Accent);
                BindImage(group + "/line (2)", SudokuThemeRole.Accent);
            }
            BindImage(Box + "/icon", SudokuThemeRole.Accent);
            BindImage("m_des1/icon", SudokuThemeRole.Accent);
            BindImage("m_des2/icon", SudokuThemeRole.Accent);
            BindImage(ConfirmButton + "/bg", SudokuThemeRole.Accent);

            CreateWidget<XYButton>("Panel/Close_Btn").OnAddListener(Dismiss);
            CreateWidget<XYButton>(Dialog + "CloseBtn").OnAddListener(Dismiss);
            CreateWidget<XYButton>(Dialog + ConfirmButton).OnAddListener(Dismiss);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _source = UserData as string ?? "settings";
            var game = SudokuPresenter.Current;
            _roundId = game != null && (_source == "first_game" || game.IsWindowActive) ? game.RoundId : null;
            _dismissed = false;
            RefreshLabels();
        }

        internal override void OnLanguageChanged() => RefreshLabels();

        protected override void OnInAnimation() => _animation.PlayIn(() =>
        {
            CompleteInAnimation();
            if (_reportedOpen == OpenVersion) return;
            _reportedOpen = OpenVersion;
            GameSDK.BiManager.SudokuGuide.SetData("show", _source, _roundId).Report();
        });
        protected override void OnOutAnimation() => _animation.PlayOut(CompleteOutAnimation);

        protected override void OnClose()
        {
            _animation.Reset();
            base.OnClose();
        }

        private void RefreshLabels()
        {
            foreach (var (label, key) in _labels)
                label.text = LocalizationHelper.GetLocalText(key);
        }

        private (RTLTextMeshPro Label, string Key) BindLabel(
            string path, string key, SudokuThemeRole role = SudokuThemeRole.Text)
        {
            var label = FindChildComponent<RTLTextMeshPro>(Dialog + path);
            UIThemeColor.Bind(label, role);
            return (label, key);
        }

        private void BindImage(string path, SudokuThemeRole role) =>
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Dialog + path), role);

        private void Dismiss()
        {
            if (_dismissed) return;
            _dismissed = true;
            GameSDK.BiManager.SudokuGuide.SetData("dismiss", _source, _roundId).Report();
            GameModule.UI.HideUI<UIGuide>();
        }
    }
}

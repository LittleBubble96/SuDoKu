using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using RTLTMPro;

namespace GameLogic
{
    public sealed class OtherGameItem : UIWidget
    {
        private RTLTextMeshPro _title, _description, _playLabel;
        private UnityEngine.UI.Image _icon;
        private string _titleKey, _descriptionKey, _appId;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<RTLTextMeshPro>("m_Title");
            _description = FindChildComponent<RTLTextMeshPro>("m_Des");
            _playLabel = FindChildComponent<RTLTextMeshPro>("m_btn/m_text");
            _icon = FindChildComponent<UnityEngine.UI.Image>("icon");
            _icon.preserveAspect = true;

            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_description, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_playLabel, SudokuThemeRole.Surface);
            UIThemeColor.Bind(gameObject.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Related);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("outline"), SudokuThemeRole.CardOutline);
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("m_btn/bg"), SudokuThemeRole.Accent);
            CreateWidget<XYButton>("m_btn").OnAddListener(Play);
        }

        public void Init(string titleKey, string descriptionKey, string appId, string iconAddress)
        {
            _titleKey = titleKey;
            _descriptionKey = descriptionKey;
            _appId = appId;
            if (!string.IsNullOrEmpty(iconAddress))
            {
                _icon.sprite = null;
                _icon.SetSprite(iconAddress, cancellationToken: gameObject.GetCancellationTokenOnDestroy());
            }
        }

        public void RefreshLabels()
        {
            _title.text = LocalizationHelper.GetLocalText(_titleKey);
            _description.text = LocalizationHelper.GetLocalText(_descriptionKey);
            _playLabel.text = LocalizationHelper.GetLocalText(LanguageKey.more_games_play);
        }

        private void Play() => GameSDK.SDK.NavigateToMiniGame(_appId);
    }
}

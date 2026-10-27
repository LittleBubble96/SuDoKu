using System.Collections.Generic;
using GameConfig.language;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;

namespace GameLogic
{
    /// <summary>设置页内容，同时供首页页签和 UISetting 窗口复用。</summary>
    public sealed class UIHomeSettingWidget : UIWidget
    {
        private const string Layout = "Center/Layout/";
        private readonly List<SettingToggleItem> _toggles = new List<SettingToggleItem>(7);
        private TMP_Dropdown _language;
        private List<ConfLanguage> _languages;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _language = FindChildComponent<TMP_Dropdown>(Layout + "GameOther/LanguageItem/dropdown");
            _language.onValueChanged.AddListener(OnLanguageSelected);
            CreateWidget<XYButton>("Top/Back/Btn").OnAddListener(() =>
            {
                if (OwnerWindow is UIHome home) home.SelectTab(0);
                else GameModule.UI.HideUI<UISetting>();
            });
            // 游戏说明尚无对应页面，先保留入口外观。
            CreateWidget<XYButton>(Layout + "GameOther/RuleItem/btn").Interactable = false;
            BindThemes();
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            BindToggle("GameExperience/SoundToggleItem", LanguageKey.setting_sfx,
                () => SudokuSettingsManager.Data.SoundVolume > 0, SudokuSettingsManager.SetSoundEnabled);
            BindToggle("GameExperience/VibrationToggleItem", LanguageKey.setting_vibration,
                () => SudokuSettingsManager.Data.vibration, SudokuSettingsManager.SetVibration);
            BindToggle("GameAssist/SameNumHighlightToggleItem", LanguageKey.setting_highlight_same_number,
                () => SudokuSettingsManager.Data.highlightSameNumber, SudokuSettingsManager.SetHighlightSameNumber);
            BindToggle("GameAssist/SameNoteHighlightToggleItem", LanguageKey.setting_highlight_same_note,
                () => SudokuSettingsManager.Data.highlightSameNote, SudokuSettingsManager.SetHighlightSameNote);
            BindToggle("GameAssist/relatedAreaToggleItem", LanguageKey.setting_highlight_related,
                () => SudokuSettingsManager.Data.highlightRelated, SudokuSettingsManager.SetHighlightRelated);
            BindToggle("GameAssist/CompletedDigitToggleItem", LanguageKey.setting_dim_completed_digits,
                () => SudokuSettingsManager.Data.dimCompletedDigits, SudokuSettingsManager.SetDimCompletedDigits);
            BindToggle("GameAssist/RemainingCountToggleItem", LanguageKey.setting_show_remaining_count,
                () => SudokuSettingsManager.Data.showRemainingCount, SudokuSettingsManager.SetShowRemainingCount);
            _languages = ConfigSystem.Instance.Tables.TbLanguage.DataList;
        }

        protected override void OnRefresh() { base.OnRefresh(); RefreshDisplay(); }

        public void RefreshDisplay()
        {
            Label("Top/Center/m_title", LanguageKey.setting_title);
            Label(Layout + "GameExperience/m_title", LanguageKey.setting_game_experience);
            Label(Layout + "GameAssist/m_title", LanguageKey.setting_game_assist);
            Label(Layout + "GameAssist/relatedAreaToggleItem/m_des", LanguageKey.setting_highlight_related_des);
            Label(Layout + "GameOther/LanguageItem/m_title", LanguageKey.setting_language);
            Label(Layout + "GameOther/RuleItem/m_title", LanguageKey.setting_how_to_play);
            foreach (var toggle in _toggles) toggle.RefreshDisplay();
            var options = new List<TMP_Dropdown.OptionData>(_languages.Count);
            int selected = 0;
            for (int i = 0; i < _languages.Count; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(LocalizationHelper.GetLocalText(_languages[i].Name)));
                if (_languages[i].LanguageCode.ToString() == SudokuSettingsManager.Data.language) selected = i;
            }
            _language.ClearOptions();
            _language.AddOptions(options);
            _language.SetValueWithoutNotify(selected);
            _language.RefreshShownValue();
        }

        private void OnLanguageSelected(int index) =>
            SudokuSettingsManager.SetLanguage(_languages[index].LanguageCode.ToString());

        private void BindToggle(string path, string key, System.Func<bool> getValue, System.Action<bool> setValue)
        {
            var toggle = CreateWidget<SettingToggleItem>(Layout + path);
            toggle.Init(key, getValue, setValue);
            _toggles.Add(toggle);
        }

        private void Label(string path, string key) =>
            FindChildComponent<TMP_Text>(path).text = LocalizationHelper.GetLocalText(key);

        private void Theme(string path, SudokuThemeRole role) =>
            UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Graphic>(path), role);

        private void BindThemes()
        {
            foreach (var text in gameObject.GetComponentsInChildren<TMP_Text>(true))
                UIThemeColor.Bind(text, SudokuThemeRole.Text);
            Theme("Top/Back/Btn/icon", SudokuThemeRole.Accent);
            foreach (string group in new[] { "GameExperience", "GameAssist", "GameOther" })
            {
                var parent = FindChild(Layout + group);
                Theme(Layout + group + "/bg", SudokuThemeRole.Surface);
                if (group != "GameOther")
                {
                    Theme(Layout + group + "/bg/bgTitle", SudokuThemeRole.Related);
                    var title = FindChildComponent<TMP_Text>(Layout + group + "/m_title");
                    title.fontSizeMax = title.fontSize;
                    title.enableAutoSizing = true;
                    title.enableWordWrapping = false;
                }
                for (int i = 0; i < parent.childCount; i++)
                    if (parent.GetChild(i).name.StartsWith("line", System.StringComparison.Ordinal))
                        UIThemeColor.Bind(parent.GetChild(i).GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Matching);
            }
            Theme(Layout + "GameAssist/relatedAreaToggleItem/m_des", SudokuThemeRole.MutedText);
            Theme(Layout + "GameOther/LanguageItem/icon", SudokuThemeRole.Accent);
            Theme(Layout + "GameOther/RuleItem/icon", SudokuThemeRole.MutedText);
            Theme(Layout + "GameOther/RuleItem/m_title", SudokuThemeRole.MutedText);
            const string dropdown = Layout + "GameOther/LanguageItem/dropdown/";
            Theme(dropdown + "Arrow", SudokuThemeRole.Accent);
            Theme(dropdown + "Template", SudokuThemeRole.Surface);
            Theme(dropdown + "Template/Viewport/Content/Item/Item Background", SudokuThemeRole.Related);
            Theme(dropdown + "Template/Viewport/Content/Item/Item Checkmark", SudokuThemeRole.Accent);
        }
    }
}


using System.Collections.Generic;
using GameConfig.language;
using GameLogic.Localization;
using GameLogic.Sudoku;
using RTLTMPro;
using TEngine;
using TMPro;

namespace GameLogic
{
    /// <summary>设置页内容，同时供首页页签和 UISetting 窗口复用。</summary>
    public sealed class UIHomeSettingWidget : UIWidget
    {
        
        private const string ScrollViewPath = "Center/ScrollView";
        private const string Layout = ScrollViewPath + "/Viewport/Content/";
        private readonly List<SettingToggleItem> _toggles = new List<SettingToggleItem>(7);
        private TMP_Dropdown _language, _textSize;
        private RTLTextMeshPro _gameVersionText;
        private List<ConfLanguage> _languages;
        private Language? _displayedLanguage;

        private TMP_Text _title, _experienceTitle, _assistTitle, _relatedDescription, _languageTitle, _rulesTitle, _textSizeTitle;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("Top/Center/m_title");
            _experienceTitle = FindChildComponent<TMP_Text>(Layout + "GameExperience/m_title");
            _assistTitle = FindChildComponent<TMP_Text>(Layout + "GameAssist/m_title");
            _relatedDescription = FindChildComponent<TMP_Text>(Layout + "GameAssist/relatedAreaToggleItem/m_des");
            _languageTitle = FindChildComponent<TMP_Text>(Layout + "GameOther/LanguageItem/m_title");
            _rulesTitle = FindChildComponent<TMP_Text>(Layout + "GameOther/RuleItem/m_title");
            _textSizeTitle = FindChildComponent<TMP_Text>(Layout + "GameAssist/TextSizeToggleItem/m_title");
            ConfigureScrollContent();
            _language = FindChildComponent<TMP_Dropdown>(Layout + "GameOther/LanguageItem/dropdown");
            _language.onValueChanged.AddListener(OnLanguageSelected);
            _textSize = FindChildComponent<TMP_Dropdown>(Layout + "GameAssist/TextSizeToggleItem/dropdown");
            _textSize.onValueChanged.AddListener(index => SudokuSettingsManager.SetTextSize((SudokuTextSize)index));
            _gameVersionText = FindChildComponent<RTLTextMeshPro>(Layout + "GameOther/gameVersion");
            CreateWidget<XYButton>("Top/Back/Btn").OnAddListener(() =>
            {
                if (OwnerWindow is UIHome home) home.SelectTab(0);
                else GameModule.UI.HideUI<UISetting>();
            });
            // 游戏说明尚无对应页面，先保留入口外观。
            CreateWidget<XYButton>(Layout + "GameOther/RuleItem/btn").Interactable = false;
            BindThemes();
        }

        private void ConfigureScrollContent()
        {
            var content = FindChildComponent<UnityEngine.UI.ScrollRect>(ScrollViewPath).content;
            // 从视口顶部向下扩展，滚动范围取决于实际分组高度。
            content.anchorMin = new UnityEngine.Vector2(content.anchorMin.x, 1f);
            content.anchorMax = new UnityEngine.Vector2(content.anchorMax.x, 1f);
            content.pivot = new UnityEngine.Vector2(content.pivot.x, 1f);
            content.anchoredPosition = new UnityEngine.Vector2(content.anchoredPosition.x, 0f);

            var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuSettingsChanged, RefreshSettings);
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
            if (_displayedLanguage != GameModule.Localization.Language) RefreshLanguage();
            RefreshSettings();
        }

        private void RefreshLanguage()
        {
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.setting_title);
            _experienceTitle.text = LocalizationHelper.GetLocalText(LanguageKey.setting_game_experience);
            _assistTitle.text = LocalizationHelper.GetLocalText(LanguageKey.setting_game_assist);
            _relatedDescription.text = LocalizationHelper.GetLocalText(LanguageKey.setting_highlight_related_des);
            _languageTitle.text = LocalizationHelper.GetLocalText(LanguageKey.setting_language);
            _rulesTitle.text = LocalizationHelper.GetLocalText(LanguageKey.setting_how_to_play);
            _textSizeTitle.text = LocalizationHelper.GetLocalText(LanguageKey.setting_text_size);
            _textSize.ClearOptions();
            _textSize.AddOptions(new List<string>
            {
                LocalizationHelper.GetLocalText(LanguageKey.setting_text_size_large),
                LocalizationHelper.GetLocalText(LanguageKey.setting_text_size_medium),
                LocalizationHelper.GetLocalText(LanguageKey.setting_text_size_small)
            });
            var options = new List<TMP_Dropdown.OptionData>(_languages.Count);
            foreach (var language in _languages)
                options.Add(new TMP_Dropdown.OptionData(LocalizationHelper.GetLocalText(language.Name)));
            _language.ClearOptions();
            _language.AddOptions(options);
            foreach (var toggle in _toggles) toggle.RefreshLanguage();
            _gameVersionText.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.setting_version), GameModule.Resource.ApplicableGameVersion);
            _displayedLanguage = GameModule.Localization.Language;
        }

        private void RefreshSettings()
        {
            _textSize.SetValueWithoutNotify((int)SudokuSettingsManager.Data.textSize);
            _textSize.RefreshShownValue();
            foreach (var toggle in _toggles) toggle.RefreshDisplay();
            int selected = _languages.FindIndex(language => language.LanguageCode.ToString() == SudokuSettingsManager.Data.language);
            _language.SetValueWithoutNotify(selected < 0 ? 0 : selected);
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
                Theme(Layout + group + "/bg", SudokuThemeRole.CardOutline);
                if (group != "GameOther")
                {
                    Theme(Layout + group + "/bg/bgTitle", SudokuThemeRole.Related);
                    var title = group == "GameExperience" ? _experienceTitle : _assistTitle;
                    title.fontSizeMax = title.fontSize;
                    title.enableAutoSizing = true;
                    title.enableWordWrapping = false;
                }
                for (int i = 0; i < parent.childCount; i++)
                    if (parent.GetChild(i).name.StartsWith("line", System.StringComparison.Ordinal))
                        UIThemeColor.Bind(parent.GetChild(i).GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Matching);
            }
            UIThemeColor.Bind(_relatedDescription, SudokuThemeRole.MutedText);
            Theme(Layout + "GameOther/LanguageItem/icon", SudokuThemeRole.Accent);
            Theme(Layout + "GameOther/RuleItem/icon", SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_rulesTitle, SudokuThemeRole.MutedText);
            Theme(Layout + "GameAssist/TextSizeToggleItem/icon", SudokuThemeRole.Accent);
            BindDropdownThemes(Layout + "GameOther/LanguageItem/dropdown/");
            BindDropdownThemes(Layout + "GameAssist/TextSizeToggleItem/dropdown/");
        }
        private void BindDropdownThemes(string dropdown)
        {
            Theme(dropdown + "Arrow", SudokuThemeRole.Accent);
            Theme(dropdown + "Template", SudokuThemeRole.Surface);
            Theme(dropdown + "Template/Viewport/Content/Item/Item Background", SudokuThemeRole.Related);
            Theme(dropdown + "Template/Viewport/Content/Item/Item Checkmark", SudokuThemeRole.Accent);
        }
    }
}


using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    /// <summary>统计页只展示数据；模板在 OnCreate 复制一次，切换难度时复用。</summary>
    public sealed class UIHomeStatisticsWidget : UIWidget
    {
        public const float TabSlideDuration = 0.22f;
        private static readonly string[] SectionKeys =
        {
            LanguageKey.statistics_games, LanguageKey.statistics_time, LanguageKey.statistics_win_streaks
        };
        private static readonly string[] RowKeys =
        {
            LanguageKey.statistics_games_played, LanguageKey.statistics_games_won,
            LanguageKey.statistics_win_rate, LanguageKey.statistics_mistake_free_wins,
            LanguageKey.statistics_best_time, LanguageKey.statistics_average_time,
            LanguageKey.statistics_total_time, LanguageKey.statistics_current_streak, LanguageKey.statistics_best_streak
        };
        private static readonly string[] RowIcons =
        {
            "Sudoku_icon_stat_games", "Sudoku_icon_trophy", "Sudoku_icon_stat_win_rate", "Sudoku_icon_stat_perfect_win",
            "Sudoku_icon_stat_best_time", "Sudoku_icon_clock", "Sudoku_icon_stat_total_time",
            "Sudoku_icon_stat_current_streak", "Sudoku_icon_stat_longest_streak"
        };
        private readonly List<StatisticsTabItem> _tabs = new List<StatisticsTabItem>(6);
        private readonly List<StatisticsItem> _rows = new List<StatisticsItem>(9);
        private readonly List<TMP_Text> _sectionTitles = new List<TMP_Text>(3);
        private RectTransform _tabsRoot, _content, _handle;
        private TMP_Text _title;
        private UnityEngine.UI.ScrollRect _scroll;
        private Tween _slide;
        private bool _positioned;
        public SudokuDifficulty SelectedDifficulty { get; private set; }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("Top/Center/m_title");
            _tabsRoot = FindChildComponent<RectTransform>("Center/Tabs");
            _handle = FindChildComponent<RectTransform>("Center/SelectTabHandle");
            _content = FindChildComponent<RectTransform>("Center/ScrollView/Viewport/Content");
            _scroll = FindChildComponent<UnityEngine.UI.ScrollRect>("Center/ScrollView");
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.content = _content;
            _scroll.viewport = FindChildComponent<RectTransform>("Center/ScrollView/Viewport");
            UIThemeColor.Bind(_handle.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Accent);
            // 滑块处于按钮下方，文字和点击区域仍由 Tabs 持有。
            int tabSibling = _tabsRoot.GetSiblingIndex();
            if (_handle.GetSiblingIndex() < tabSibling) tabSibling--;
            _handle.SetSiblingIndex(tabSibling);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            var tabTemplate = _tabsRoot.Find("item");
            tabTemplate.gameObject.SetActive(false);
            for (int i = 0; i < 6; i++)
            {
                var tab = CreateWidgetByPrefab<StatisticsTabItem>(tabTemplate.gameObject, _tabsRoot);
                tab.gameObject.name = "DifficultyTab" + i;
                tab.Init((SudokuDifficulty)i, SelectDifficulty);
                _tabs.Add(tab);
            }

            var titleTemplate = _content.Find("TitleItem");
            var rowTemplate = _content.Find("StatisticsItem");
            titleTemplate.gameObject.SetActive(false);
            rowTemplate.gameObject.SetActive(false);
            int row = 0;
            int[] sectionSizes = { 4, 3, 2 };
            for (int section = 0; section < sectionSizes.Length; section++)
            {
                var title = Object.Instantiate(titleTemplate.gameObject, _content);
                title.name = "TitleItem" + section;
                title.SetActive(true);
                _sectionTitles.Add(title.transform.Find("m_title").GetComponent<TMP_Text>());
                for (int i = 0; i < sectionSizes[section]; i++, row++)
                {
                    var item = CreateWidgetByPrefab<StatisticsItem>(rowTemplate.gameObject, _content);
                    item.gameObject.name = "StatisticsItem" + row;
                    item.Init(RowKeys[row], RowIcons[row]);
                    _rows.Add(item);
                }
            }
            var fitter = _content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = _content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.verticalNormalizedPosition = 1;
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnRefresh() { base.OnRefresh(); RefreshDisplay(); }

        public void SelectDifficulty(SudokuDifficulty difficulty)
        {
            if (SelectedDifficulty == difficulty && _positioned) return;
            SelectedDifficulty = difficulty;
            RefreshDisplay();
            MoveHandle(true);
        }

        public void RefreshDisplay()
        {
            if (_tabs.Count != 6 || _rows.Count != 9) return;
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.statistics_title);
            for (int i = 0; i < _tabs.Count; i++) _tabs[i].RefreshDisplay(SelectedDifficulty);
            for (int i = 0; i < _sectionTitles.Count; i++)
                _sectionTitles[i].text = LocalizationHelper.GetLocalText(SectionKeys[i]);
            var data = SudokuPresenter.Current.Statistics.Get(SelectedDifficulty);
            string[] values =
            {
                data.played.ToString(), data.won.ToString(), data.WinRate.ToString("0.#", CultureInfo.InvariantCulture) + "%",
                data.perfectWins.ToString(), data.won == 0 ? "--:--" : SudokuUIFormatting.Time(data.bestSeconds),
                data.won == 0 ? "--:--" : SudokuUIFormatting.Time(data.AverageSeconds), SudokuUIFormatting.Time(data.totalSeconds),
                data.currentStreak.ToString(), data.bestStreak.ToString()
            };
            for (int i = 0; i < _rows.Count; i++) _rows[i].RefreshDisplay(values[i]);
            if (!_positioned && gameObject.activeInHierarchy) MoveHandle(false);
        }

        private void MoveHandle(bool animated)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_tabsRoot);
            var tab = _tabs[(int)SelectedDifficulty].rectTransform;
            Vector3 target = _handle.parent.InverseTransformPoint(tab.TransformPoint(tab.rect.center));
            target.z = _handle.localPosition.z;
            _slide?.Kill();
            if (animated)
                _slide = _handle.DOLocalMove(target, TabSlideDuration).SetEase(DG.Tweening.Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
            else _handle.localPosition = target;
            _positioned = true;
        }

        protected override void OnSetVisible(bool visible)
        {
            base.OnSetVisible(visible);
            if (visible) RefreshDisplay();
            else StopSlide();
        }

        protected override void OnClose() { StopSlide(); base.OnClose(); }
        protected override void OnDestroy() { StopSlide(); base.OnDestroy(); }
        private void StopSlide() { _slide?.Kill(); _slide = null; _positioned = false; }
    }
}

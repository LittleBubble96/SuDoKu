using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.UI, "UITrophyGallery", fullScreen: true)]
    public sealed class UITrophyGallery : UIWindow
    {
        private const string Top = "Panel/Content/Top/";
        private readonly List<TrophyGalleryItem> _items = new List<TrophyGalleryItem>();
        private SudokuGameController _game;
        private RectTransform _content;
        private UnityEngine.UI.ScrollRect _scroll;
        private TMP_Text _title, _year, _collected, _description;
        private XYButton _previous, _next;
        private UIThemeColor _previousColor, _nextColor;
        private DateTime _lastToday;
        private bool _loading;
        private Exception _loadError;
        public int SelectedYear { get; private set; }
        public IReadOnlyList<TrophyGalleryItem> Items => _items;
        public bool IsContentReady
        {
            get
            {
                if (_game == null || _loading || _loadError != null) return false;
                int count = _game.Trophies.GetForYear(SelectedYear).Count;
                if (_items.Count < count) return false;
                for (int i = 0; i < count; i++) if (!_items[i].IsIconReady) return false;
                return true;
            }
        }

        public sealed class Context
        {
            public readonly SudokuGameController Game;
            public readonly int Year;
            public Context(SudokuGameController game, int year) { Game = game; Year = year; }
        }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _content = FindChildComponent<RectTransform>("Panel/Content/Scroll/Viewport/Content");
            _scroll = FindChildComponent<UnityEngine.UI.ScrollRect>("Panel/Content/Scroll");
            _title = FindChildComponent<TMP_Text>(Top + "Center/m_title");
            _year = FindChildComponent<TMP_Text>(Top + "Year/m_title");
            _collected = FindChildComponent<TMP_Text>(Top + "Year/m_progress");
            _description = FindChildComponent<TMP_Text>(Top + "Year/m_des");
            _previous = CreateWidget<XYButton>(Top + "Year/Left/Btn");
            _next = CreateWidget<XYButton>(Top + "Year/Right/Btn");
            _previous.OnAddListener(() => MoveYear(-1));
            _next.OnAddListener(() => MoveYear(1));
            CreateWidget<XYButton>(Top + "Back/Btn").OnAddListener(() => GameModule.UI.CloseUI<UITrophyGallery>());
            UIThemeColor.Bind(gameObject.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Background);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            UIThemeColor.Bind(_year, SudokuThemeRole.Text);
            UIThemeColor.Bind(_collected, SudokuThemeRole.MutedText);
            UIThemeColor.Bind(_description, SudokuThemeRole.MutedText);
            Theme(Top + "Back/Btn/icon", SudokuThemeRole.Text);
            _previousColor = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Top + "Year/Left/Btn/icon"), SudokuThemeRole.Text);
            _nextColor = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(Top + "Year/Right/Btn/icon"), SudokuThemeRole.Text);
            Theme(Top + "Year/bg", SudokuThemeRole.Selected);
            Theme(Top + "Year/ProgressBg", SudokuThemeRole.Related);
            Theme(Top + "Year/ProgressBg/outline", SudokuThemeRole.Matching);
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(EventDefine.SudokuStateChanged, RefreshDisplay);
            AddUIEvent(EventDefine.SudokuTrophiesChanged, RefreshDisplay);
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshDisplay);
        }

        protected override void OnCreate() { base.OnCreate(); SudokuThemeManager.Changed += RefreshDisplay; }
        protected override void OnDestroy() { SudokuThemeManager.Changed -= RefreshDisplay; base.OnDestroy(); }
        protected override void OnRefresh()
        {
            base.OnRefresh();
            var context = UserData as Context;
            _game = context?.Game ?? (UserData as SudokuContext)?.Game ?? SudokuPresenter.Current;
            if (_game == null) return;
            SelectedYear = ClosestYear(context?.Year ?? _game.Today.Year);
            _loadError = null;
            _scroll.StopMovement(); _scroll.verticalNormalizedPosition = 1;
            RefreshDisplay();
        }

        public void MoveYear(int direction)
        {
            if (_game == null || !IsOpen || direction != -1 && direction != 1) return;
            var years = _game.Trophies.Years;
            int index = YearIndex(); int target = index + direction;
            if (target < 0 || target >= years.Count) return;
            SelectedYear = years[target]; _loadError = null;
            _scroll.StopMovement(); _scroll.verticalNormalizedPosition = 1;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_game == null || !IsOpen) return;
            _lastToday = _game.Today;
            var entries = _game.Trophies.GetForYear(SelectedYear);
            _title.text = LocalizationHelper.GetLocalText(LanguageKey.daily_trophy_hall);
            _year.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.trophy_year), SelectedYear);
            string count = "<color=#" + ColorUtility.ToHtmlStringRGB(SudokuThemeManager.GetColor(SudokuThemeRole.Accent)) + ">" + _game.Trophies.CollectedCount(SelectedYear) + "</color>";
            _collected.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.trophy_collected), count, entries.Count);
            _description.text = LocalizationHelper.GetLocalText(LanguageKey.trophy_gallery_desc);
            int index = YearIndex();
            _previous.Interactable = index > 0;
            _next.Interactable = index >= 0 && index < _game.Trophies.Years.Count - 1;
            _previousColor.SetRole(_previous.Interactable ? SudokuThemeRole.Text : SudokuThemeRole.MutedText);
            _nextColor.SetRole(_next.Interactable ? SudokuThemeRole.Text : SudokuThemeRole.MutedText);
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Visible = i < entries.Count;
                if (i < entries.Count) _items[i].RefreshDisplay(entries[i], CloseToken);
            }
            if (_items.Count < entries.Count && !_loading && _loadError == null) LoadItemsAsync(CloseToken).Forget();
        }

        private async UniTaskVoid LoadItemsAsync(CancellationToken token)
        {
            _loading = true; int version = OpenVersion; GameObject prefab = null;
            try
            {
                prefab = await GameModule.Resource.LoadAssetAsync<GameObject>("TrophyGalleryItem", token);
                token.ThrowIfCancellationRequested();
                if (!IsOpen || version != OpenVersion) return;
                if (prefab == null) throw new InvalidOperationException("奖杯条目预制体加载失败。");
                int count = _game.Trophies.GetForYear(SelectedYear).Count;
                while (_items.Count < count)
                {
                    var item = CreateWidgetByPrefab<TrophyGalleryItem>(prefab, _content, false);
                    if (item == null) throw new InvalidOperationException("奖杯条目绑定失败。");
                    item.gameObject.name = "TrophyGalleryItem" + _items.Count;
                    _items.Add(item);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (IsOpen && version == OpenVersion) _loadError = error; Debug.LogException(error); }
            finally
            {
                if (prefab != null) GameModule.Resource.UnloadAsset(prefab);
                _loading = false;
                if (IsOpen) RefreshDisplay();
            }
        }

        public async UniTask WaitForContentAsync(CancellationToken token)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, CloseToken))
                await UniTask.WaitUntil(() => IsContentReady || ContentError() != null, cancellationToken: linked.Token);
            var error = ContentError(); if (error != null) throw error;
        }

        private Exception ContentError()
        {
            if (_loadError != null) return _loadError;
            foreach (var item in _items) if (item.Visible && item.IconError != null) return item.IconError;
            return null;
        }
        protected override void OnUpdate()
        {
            if (IsOpen && _game != null && _lastToday != _game.Today) RefreshDisplay();
        }
        private int ClosestYear(int year)
        {
            int result = year, distance = int.MaxValue;
            foreach (int configured in _game.Trophies.Years)
                if (Math.Abs(configured - year) < distance) { result = configured; distance = Math.Abs(configured - year); }
            return result;
        }
        private int YearIndex()
        {
            for (int i = 0; i < _game.Trophies.Years.Count; i++) if (_game.Trophies.Years[i] == SelectedYear) return i;
            return -1;
        }
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

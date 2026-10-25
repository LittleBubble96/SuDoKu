using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic.Localization;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.UI, "UIHome", fullScreen: true)]
    public sealed class UIHome : UIWindow
    {
        private static readonly string[] TabNames = { "Home", "DailyChallenge", "Statistics", "Setting" };
        private static readonly string[] TabKeys =
        {
            LanguageKey.home_title, LanguageKey.daily_challenge,
            LanguageKey.home_statistics, LanguageKey.home_setting
        };
        private static readonly string[] ContentLocations = { "UIHome_Home", "UIHome_DC", "UIHome_Home", "UIHome_Home" };
        private readonly UIThemeColor[] _tabIcons = new UIThemeColor[4];
        private readonly UIThemeColor[] _tabColors = new UIThemeColor[4];
        private readonly TMP_Text[] _tabLabels = new TMP_Text[4];
        private Transform _content;
        private UIHomeHomeWidget _home;
        private UIHomeDailyWidget _daily;
        private DateTime? _pendingDailyDate;
        private bool _loading;
        private Exception _loadError;
        public int SelectedTab { get; private set; }
        public bool IsContentReady => SelectedTab == 1 ? _daily != null : _home != null;
        public UIHomeDailyWidget DailyPage => _daily;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _content = FindChild("Panel/Content");
            var background = gameObject.GetComponent<UnityEngine.UI.Image>();
            UIThemeColor.Bind(background, SudokuThemeRole.Background);
            for (int i = 0; i < TabNames.Length; i++)
            {
                int tab = i;
                string path = "Panel/Bottom/Layout/" + TabNames[i];
                CreateWidget<XYButton>(path + "/root").OnAddListener(() => SelectTab(tab));
                _tabIcons[i] = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path + "/root/icon"), SudokuThemeRole.MutedText);
                _tabLabels[i] = FindChildComponent<TMP_Text>(path + "/root/m_text");
                _tabColors[i] = UIThemeColor.Bind(_tabLabels[i], SudokuThemeRole.MutedText);
            }
        }

        protected override void RegisterEvent()
        {
            base.RegisterEvent();
            AddUIEvent(LocalizationModule.Event_LanguageChanged, RefreshLabels);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            // 如预制体内已放置页面实例，直接绑定并复用。
            for (int i = 0; i < _content.childCount; i++)
            {
                var child = _content.GetChild(i);
                if (child.name == "UIHome_DC") _daily = CreateWidget<UIHomeDailyWidget>(child.gameObject, false);
                else if (child.name == "UIHome_Home") _home = CreateWidget<UIHomeHomeWidget>(child.gameObject, false);
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            SudokuPresenter.Current.SetWindowActive(false);
            RefreshLabels();
            SelectTab(SelectedTab);
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= TabNames.Length || !IsOpen) return;
            SelectedTab = index;
            _loadError = null;
            for (int i = 0; i < TabNames.Length; i++)
            {
                var role = i == index ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText;
                _tabIcons[i].SetRole(role);
                _tabColors[i].SetRole(role);
            }
            RefreshContent();
            if (!IsContentReady) LoadContentAsync(CloseToken).Forget();
        }

        public void SelectDailyDate(DateTime date)
        {
            _pendingDailyDate = date.Date;
            SelectTab(1);
        }

        private void RefreshContent()
        {
            if (_home != null)
            {
                _home.Visible = SelectedTab != 1;
                if (_home.Visible) _home.RefreshDisplay();
            }
            if (_daily != null)
            {
                _daily.Visible = SelectedTab == 1;
                if (_daily.Visible)
                {
                    if (_pendingDailyDate.HasValue) { _daily.SelectDate(_pendingDailyDate.Value); _pendingDailyDate = null; }
                    _daily.RefreshDisplay();
                }
            }
        }

        private void RefreshLabels()
        {
            for (int i = 0; i < TabKeys.Length; i++) _tabLabels[i].text = LocalizationHelper.GetLocalText(TabKeys[i]);
        }

        private async UniTaskVoid LoadContentAsync(CancellationToken token)
        {
            if (_loading) return;
            _loading = true;
            _loadError = null;
            bool daily = SelectedTab == 1;
            int openVersion = OpenVersion;
            GameObject instance = null;
            try
            {
                instance = await GameModule.Resource.LoadGameObjectAsync(daily ? ContentLocations[1] : ContentLocations[0], _content, token);
                token.ThrowIfCancellationRequested();
                if (!IsOpen || openVersion != OpenVersion) return;
                if (instance == null) throw new InvalidOperationException("首页内容预制体加载失败。");
                instance.name = daily ? "UIHome_DC" : "UIHome_Home";
                if (daily) _daily = CreateWidget<UIHomeDailyWidget>(instance, SelectedTab == 1);
                else _home = CreateWidget<UIHomeHomeWidget>(instance, SelectedTab != 1);
                if ((daily && _daily == null) || (!daily && _home == null)) throw new InvalidOperationException("首页内容绑定失败。");
                instance = null; // 由窗口和 Widget 生命周期持有。
                RefreshContent();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (daily == (SelectedTab == 1)) _loadError = error; Debug.LogException(error); }
            finally
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                _loading = false;
                // 旧打开周期取消时，新一轮打开可能正在等待它结束。
                if (IsOpen && !IsContentReady && _loadError == null) LoadContentAsync(CloseToken).Forget();
            }
        }

        public async UniTask WaitForContentAsync(CancellationToken token)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, CloseToken))
                await UniTask.WaitUntil(() => IsContentReady || _loadError != null, cancellationToken: linked.Token);
            if (_loadError != null) throw _loadError;
        }
    }
}

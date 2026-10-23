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
        // 当前四个页签都映射到主页，并复用已经加载的同一个 Widget。
        private static readonly string[] ContentLocations = { "UIHome_Home", "UIHome_Home", "UIHome_Home", "UIHome_Home" };
        private readonly UIThemeColor[] _tabIcons = new UIThemeColor[4];
        private readonly UIThemeColor[] _tabColors = new UIThemeColor[4];
        private readonly TMP_Text[] _tabLabels = new TMP_Text[4];
        private Transform _content;
        private UIHomeHomeWidget _home;
        private bool _loading;
        private Exception _loadError;
        public int SelectedTab { get; private set; }
        public bool IsContentReady => _home != null && !_loading;

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
            for (int i = 0; i < TabNames.Length; i++)
            {
                var role = i == index ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText;
                _tabIcons[i].SetRole(role);
                _tabColors[i].SetRole(role);
            }
            if (_home != null) { _home.Visible = true; _home.RefreshDisplay(); }
            else LoadContentAsync(CloseToken).Forget();
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
            GameObject instance = null;
            try
            {
                instance = await GameModule.Resource.LoadGameObjectAsync(ContentLocations[SelectedTab], _content, token);
                token.ThrowIfCancellationRequested();
                if (instance == null) throw new InvalidOperationException("UIHome_Home 预制体加载失败。");
                _home = CreateWidget<UIHomeHomeWidget>(instance);
                if (_home == null) throw new InvalidOperationException("UIHome_Home 绑定失败。");
                instance = null; // 由窗口和 Widget 生命周期持有。
                _home.RefreshDisplay();
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { _loadError = error; Debug.LogException(error); }
            finally
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                _loading = false;
                // 旧打开周期取消时，新一轮打开可能正在等待它结束。
                if (token.IsCancellationRequested && IsOpen && _home == null) LoadContentAsync(CloseToken).Forget();
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

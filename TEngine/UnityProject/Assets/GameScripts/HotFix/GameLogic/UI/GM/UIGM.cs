#if !TE_RELEASE
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using TEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>沿用参考项目：异步创建顶部页签、分组，再用按钮/输入框条目预制体注册命令。</summary>
    [Window(UILayer.System, "UIGM")]
    public sealed class UIGM : UIWindow
    {
        private RectTransform _topButtons, _content;
        private TextMeshProUGUI _status;
        private readonly Dictionary<string, List<GmGroupWidget>> _groups = new Dictionary<string, List<GmGroupWidget>>();
        private GmGroupWidget _currentGroup;
        private string _currentTitle;
        private bool _building, _built;
        public bool IsReady => _built;

        protected override void RegisterEvent() => AddUIEvent(EventDefine.SudokuStateChanged, RefreshStatus);
        protected override void OnCreate()
        {
            _topButtons = FindChildComponent<RectTransform>("Panel/UITopBtns");
            _content = FindChildComponent<RectTransform>("Panel/ScrollView/Viewport/Content");
            _status = FindChildComponent<TextMeshProUGUI>("Panel/Status");
            FindChildComponent<Button>("Panel/CloseBtn").onClick.AddListener(() => GameModule.UI.CloseUI<UIGM>());
        }
        protected override void OnRefresh()
        {
            GMSingle.IsPanelOpen = true;
            RefreshStatus();
            if (!_built && !_building) GenerateGmItemsAsync().Forget();
        }
        protected override void OnClose() { GMSingle.IsPanelOpen = false; base.OnClose(); }
        protected override void OnDestroy() { GMSingle.IsPanelOpen = false; base.OnDestroy(); }

        private async UniTask CreateTopBtnAsync(string title)
        {
            _currentTitle = title; _groups.Add(title, new List<GmGroupWidget>());
            var widget = await CreateWidgetByPathAsync<GmTopBtnWidget>(_topButtons, "TopBtn_GM");
            widget.Init(title, OnClickTopBtn);
        }
        private async UniTask CreateGmGroupAsync(string title)
        {
            _currentGroup = await CreateWidgetByPathAsync<GmGroupWidget>(_content, "GmGroup");
            _currentGroup.SetTitle(title); _groups[_currentTitle].Add(_currentGroup);
        }
        private UniTask CreateBtnAsync(string label, Action action) => _currentGroup.CreateBtnAsync(label, () => Execute(action));
        private UniTask CreateBtnAndInputAsync(string label, Action<string> action) => _currentGroup.CreateBtnAndInputAsync(label, value => Execute(() => action(value)));

        private void Execute(Action action)
        {
            try { action(); RefreshStatus(); }
            catch (Exception e) { GMSingle.Instance.Game.SetMessage("[GM] " + e.Message); Log.Warning("[GM] " + e.Message); }
        }
        private void OnClickTopBtn(string title)
        {
            foreach (var pair in _groups) foreach (var group in pair.Value) group.Visible = pair.Key == title;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }
        private void RefreshStatus()
        {
            var game = GMSingle.IsValid ? GMSingle.Instance.Game : null;
            if (_status != null && game != null) _status.text = game.DescribeState() + "\n" + game.Message;
        }

        private async UniTaskVoid GenerateGmItemsAsync()
        {
            _building = true;
            try
            {
                var game = GMSingle.Instance.Game;
                await CreateTopBtnAsync("局内");
                await CreateGmGroupAsync("关卡");
                await CreateBtnAndInputAsync("跳转当前难度关卡", value =>
                {
                    if (!int.TryParse(value, out int number) || number < 1 || number > game.Session.LevelCount(game.Session.Difficulty)) throw new ArgumentException("请输入范围内的关卡号，从 1 开始。");
                    game.OpenLevel(game.Session.Difficulty, number - 1);
                });
                await CreateBtnAndInputAsync("切换难度：0快速～5大师", value =>
                {
                    if (!int.TryParse(value, out int difficulty) || difficulty < 0 || difficulty > 5) throw new ArgumentException("难度请输入 0～5；锁定档位需先解锁。");
                    game.OpenLevel((SudokuDifficulty)difficulty, game.Session.FirstUncompleted((SudokuDifficulty)difficulty));
                });
                await CreateBtnAsync("上一关", game.PreviousLevel);
                await CreateBtnAsync("下一关", game.NextLevel);
                await CreateBtnAsync("重新开始本关", game.Restart);
                await CreateBtnAsync("填入一个正确数字", game.DebugFillNext);
                await CreateBtnAsync("直接通关", game.DebugComplete);
                await CreateTopBtnAsync("进度");
                await CreateGmGroupAsync("难度解锁");
                await CreateBtnAsync("临时解锁全部难度", game.DebugUnlockAll);
                await CreateBtnAsync("重置数独进度", game.DebugResetProgress);
                await CreateGmGroupAsync("换色与提示");
                await CreateBtnAndInputAsync("主题颜色：0～5", value =>
                {
                    if (!int.TryParse(value, out int theme) || theme < 0 || theme >= SudokuThemeManager.ThemeCount) throw new ArgumentException("主题请输入 0～5。");
                    SudokuThemeManager.SetTheme(theme);
                });
                await CreateBtnAndInputAsync("提示数量", value =>
                {
                    if (!int.TryParse(value, out int count) || count < 0) throw new ArgumentException("请输入非负整数。");
                    game.DebugSetProps(count, game.Props.coinCount);
                });
                await CreateBtnAndInputAsync("金币数量", value =>
                {
                    if (!int.TryParse(value, out int count) || count < 0) throw new ArgumentException("请输入非负整数。");
                    game.DebugSetProps(game.Props.tipCount, count);
                });
                await CreateTopBtnAsync("工具");
                await CreateGmGroupAsync("状态与导航");
                await CreateBtnAsync("打印当前状态", () => { Log.Info("[GM] " + game.DescribeState()); game.SetMessage("[GM] 当前状态已打印到控制台。"); });
                await CreateBtnAsync("关闭 GM 面板", () => GameModule.UI.CloseUI<UIGM>());
                OnClickTopBtn("局内"); _built = true; RefreshStatus();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Log.Error("[GM] 面板生成失败: " + e); }
            finally { _building = false; }
        }
    }
}
#endif

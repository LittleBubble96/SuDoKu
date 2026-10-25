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
    public sealed class TrophyGalleryItem : UIWidget
    {
        private TMP_Text _title, _status;
        private UnityEngine.UI.Image _icon;
        private UnityEngine.UI.Outline _activeBorder;
        private GameObject _locked, _obtained, _activeOutline;
        private UIThemeColor _backgroundColor, _outlineColor, _statusColor;
        private Sprite _sprite;
        private string _loadedIcon, _requestedIcon;
        private int _iconVersion;
        private bool _loading;
        private CancellationTokenSource _iconCancellation;
        public SudokuTrophyProgress Progress { get; private set; }
        public bool IsIconReady => !_loading && _sprite != null && _loadedIcon == _requestedIcon;
        public Exception IconError { get; private set; }

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _title = FindChildComponent<TMP_Text>("m_title");
            _status = FindChildComponent<TMP_Text>("m_getState");
            _icon = FindChildComponent<UnityEngine.UI.Image>("Trophy/icon");
            _locked = FindChild("Trophy/LockBg").gameObject;
            _obtained = FindChild("Trophy/GetRoot").gameObject;
            _activeOutline = FindChild("bg/ActiveOutline").gameObject;
            _activeBorder = _activeOutline.GetComponent<UnityEngine.UI.Outline>();
            _backgroundColor = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("bg"), SudokuThemeRole.KeyBackground);
            _outlineColor = UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>("bg/outline"), SudokuThemeRole.Matching);
            UIThemeColor.Bind(_title, SudokuThemeRole.Text);
            _statusColor = UIThemeColor.Bind(_status, SudokuThemeRole.MutedText);
            Theme("bg/ActiveOutline", SudokuThemeRole.Accent);
            Theme("Trophy/LockBg", SudokuThemeRole.Surface);
            Theme("Trophy/LockBg/outLine", SudokuThemeRole.Matching);
            Theme("Trophy/LockBg/lock", SudokuThemeRole.MutedText);
            Theme("Trophy/GetRoot", SudokuThemeRole.Accent);
            Theme("Trophy/GetRoot/icon", SudokuThemeRole.Surface);
        }

        public void RefreshDisplay(SudokuTrophyProgress progress, CancellationToken token)
        {
            Progress = progress;
            bool obtained = progress.IsObtained, active = progress.State == SudokuTrophyState.InProgress;
            _title.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.trophy_month_name), progress.Config.Month, LocalizationHelper.GetLocalText(progress.Config.NameKey));
            if (active)
            {
                string textColor = ColorUtility.ToHtmlStringRGB(SudokuThemeManager.GetColor(SudokuThemeRole.Text));
                string completed = "<color=#" + textColor + ">" + progress.CompletedDays + "</color>";
                string required = "<color=#" + textColor + ">" + progress.RequiredDays + "</color>";
                _status.text = string.Format(LocalizationHelper.GetLocalText(LanguageKey.trophy_progress), completed, required);
            }
            else _status.text = LocalizationHelper.GetLocalText(obtained ? LanguageKey.trophy_obtained :
                progress.State == SudokuTrophyState.NotStarted ? LanguageKey.trophy_not_started : LanguageKey.trophy_not_obtained);
            _statusColor.SetRole(obtained || active ? SudokuThemeRole.Accent : SudokuThemeRole.MutedText);
            _backgroundColor.SetRole(obtained ? SudokuThemeRole.Related : active ? SudokuThemeRole.Surface : SudokuThemeRole.KeyBackground);
            _outlineColor.SetRole(obtained ? SudokuThemeRole.Selected : SudokuThemeRole.Matching);
            _activeOutline.SetActive(active);
            _activeBorder.effectColor = SudokuThemeManager.GetColor(SudokuThemeRole.Accent);
            _locked.SetActive(!obtained && !active);
            _obtained.SetActive(obtained);
            _icon.color = new Color(1, 1, 1, active ? .6f : 1); // 原画保留金色 / 银色，进行中淡显彩色奖杯。
            string location = obtained || active ? progress.Config.IconObtained : progress.Config.IconNotObtained;
            if (_loadedIcon == location && _sprite != null)
            {
                CancelIconLoad(); _requestedIcon = location; IconError = null; _icon.enabled = true;
                return;
            }
            if (_requestedIcon == location && (_loading || _loadedIcon == location && _sprite != null)) return;
            CancelIconLoad(); _requestedIcon = location; IconError = null; _icon.enabled = false;
            _iconCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            LoadIconAsync(location, ++_iconVersion, _iconCancellation.Token).Forget();
        }

        private async UniTaskVoid LoadIconAsync(string location, int version, CancellationToken token)
        {
            _loading = true; Sprite asset = null;
            try
            {
                asset = await GameModule.Resource.LoadAssetAsync<Sprite>(location, token);
                token.ThrowIfCancellationRequested();
                if (version != _iconVersion) return;
                if (asset == null) throw new InvalidOperationException("奖杯图标加载失败: " + location);
                ReleaseSprite(); _sprite = asset; asset = null;
                _loadedIcon = location; _icon.sprite = _sprite; _icon.enabled = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (version == _iconVersion) IconError = error; Debug.LogException(error); }
            finally
            {
                if (asset != null) GameModule.Resource.UnloadAsset(asset);
                if (version == _iconVersion) _loading = false;
            }
        }

        private void CancelIconLoad()
        {
            _iconVersion++; _iconCancellation?.Cancel(); _iconCancellation?.Dispose(); _iconCancellation = null; _loading = false;
        }
        protected override void OnClose() { CancelIconLoad(); _requestedIcon = null; base.OnClose(); }
        protected override void OnDestroy() { CancelIconLoad(); ReleaseSprite(); base.OnDestroy(); }
        private void ReleaseSprite()
        {
            if (_icon != null) _icon.sprite = null;
            if (_sprite != null) GameModule.Resource.UnloadAsset(_sprite);
            _sprite = null; _loadedIcon = null;
        }
        private void Theme(string path, SudokuThemeRole role) => UIThemeColor.Bind(FindChildComponent<UnityEngine.UI.Image>(path), role);
    }
}

using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using GameLogic.Localization;
using TEngine;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public sealed class SudokuHintWidget : UIWidget
    {
        private XYButton _button;
        private Transform _countRoot, _coinRoot, _adRoot, _loading, _ready;
        private RectTransform _spinner;
        private TMP_Text _count, _coin;
        private TMP_Text _loadingText, _readyText;
        private CanvasGroup _display;
        private SudokuGameController _game;
        private bool _using, _destroyed;
        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            _button = CreateWidget<XYButton>(gameObject);
            _countRoot = FindChild("Sign/m_num");
            _coinRoot = FindChild("Sign/m_coin");
            _adRoot = FindChild("Sign/m_Ad");
            _loading = FindChild("Sign/m_Ad/Loading");
            _ready = FindChild("Sign/m_Ad/Ready");
            _spinner = FindChildComponent<RectTransform>("Sign/m_Ad/Loading/icon");
            _count = FindChildComponent<TMP_Text>("Sign/m_num/m_text");
            _coin = FindChildComponent<TMP_Text>("Sign/m_coin/m_text");
            _loadingText = FindChildComponent<TMP_Text>("Sign/m_Ad/Loading/m_text");
            _readyText = FindChildComponent<TMP_Text>("Sign/m_Ad/Ready/m_text");
            _display = gameObject.GetComponent<CanvasGroup>();
            if (_display == null) _display = gameObject.AddComponent<CanvasGroup>();
            _button.OnAddListener(() => UseAsync().Forget());
        }
        protected override void OnCreate()
        {
            base.OnCreate();
            AdSystem.StateChanged += OnAdStateChanged;
        }
        public void Init(SudokuGameController game) { _game = game; RefreshDisplay(); }
        private void OnAdStateChanged(string adId) { if (adId == AdIds.Tip) RefreshDisplay(); }
        public void RefreshDisplay()
        {
            if (_destroyed || _game == null) return;
            bool byCount = _game.Props.tipCount > 0;
            bool byCoin = !byCount && _game.Props.coinCount >= SudokuGameController.HintCoinCost;
            bool byAd = !byCount && !byCoin;
            _countRoot.gameObject.SetActive(byCount);
            _coinRoot.gameObject.SetActive(byCoin);
            _adRoot.gameObject.SetActive(byAd);
            _count.text = _game.Props.tipCount.ToString();
            _coin.text = SudokuGameController.HintCoinCost.ToString();
            _loadingText.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_loading);
            _readyText.text = LocalizationHelper.GetLocalText(LanguageKey.sudoku_ui_ad);
            bool loading = AdSystem.IsLoading(AdIds.Tip) || AdSystem.GetState(AdIds.Tip) == RewardedAdState.Idle;
            _loading.gameObject.SetActive(loading);
            _ready.gameObject.SetActive(!loading);
            bool available = !_using && _game.CanInteract && (byCount || byCoin || AdSystem.IsAdAvailable(AdIds.Tip));
            _button.Interactable = available;
            _display.alpha = available || (byAd && loading) ? 1f : .45f;
        }
        private async UniTaskVoid UseAsync()
        {
            if (_using || _game == null || !_game.CanInteract || _game.Session.FindHintCell(_game.SelectedCell) < 0) return;
            _using = true;
            int revision = _game.LevelRevision;
            int openVersion = OwnerWindow.OpenVersion;
            bool waitingForAd = false;
            try
            {
                bool rewarded = false;
                if (_game.Props.tipCount <= 0 && _game.Props.coinCount < SudokuGameController.HintCoinCost)
                {
                    if (!AdSystem.IsAdAvailable(AdIds.Tip)) return;
                    waitingForAd = true; _game.SetAdShowing(true); RefreshDisplay();
                    rewarded = await AdSystem.ShowRewardedAdAsync(AdIds.Tip);
                    _game.SetAdShowing(false); waitingForAd = false;
                }
                if (_destroyed || OwnerWindow?.IsOpen != true || OwnerWindow.OpenVersion != openVersion || revision != _game.LevelRevision) return;
                _game.TryUseHint(rewarded, revision);
            }
            catch (System.Exception error) { Log.Error("[SudokuHint] 使用失败: " + error); }
            finally
            {
                if (waitingForAd) _game.SetAdShowing(false);
                _using = false;
                if (!_destroyed) RefreshDisplay();
            }
        }
        protected override void OnUpdate()
        {
            if (_spinner.gameObject.activeInHierarchy && AdSystem.IsLoading(AdIds.Tip))
            {
                var animation = _spinner.GetComponent<Animation>();
                var animator = _spinner.GetComponent<Animator>();
                if ((animation == null || !animation.enabled) && (animator == null || !animator.enabled))
                    _spinner.Rotate(0, 0, -240f * Time.unscaledDeltaTime);
            }
        }
        protected override void OnDestroy()
        {
            _destroyed = true;
            AdSystem.StateChanged -= OnAdStateChanged;
            base.OnDestroy();
        }
    }
}

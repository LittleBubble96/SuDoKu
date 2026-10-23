using UnityEngine;
using Cysharp.Threading.Tasks;
using TEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace GameLogic.Sudoku
{
    /// <summary>与参考项目相同的生命周期桥接；额外处理微信 onHide 和定时保存用时。</summary>
    public sealed class SudokuAppBridge : MonoBehaviour
    {
        private bool _paused, _unfocused;
#if UNITY_WEBGL && !UNITY_EDITOR
        private bool _wxHidden;
#else
        private const bool _wxHidden = false;
#endif
        private float _saveElapsed;
        private void Awake()
        {
            GameEvent.AddEventListener(EventDefine.SudokuBackRequested, OnBackRequested);
#if UNITY_WEBGL && !UNITY_EDITOR
            WX.OnHide(OnWeChatHide); WX.OnShow(OnWeChatShow);
#endif
        }
        private void OnBackRequested() => SudokuPresenter.ShowHomeAsync().Forget();
        private void OnApplicationPause(bool paused) { _paused = paused; UpdateSuspended(); }
        private void OnApplicationFocus(bool focused) { _unfocused = !focused; UpdateSuspended(); }
        private void UpdateSuspended() => SudokuPresenter.Current?.SetSuspended(_paused || _unfocused || _wxHidden);
        private void OnApplicationQuit() => SudokuPresenter.SaveOnQuit();
        private void Update()
        {
            _saveElapsed += Time.unscaledDeltaTime;
            if (_saveElapsed < 30f) return;
            _saveElapsed = 0; SudokuPresenter.Current?.SaveGameProgress();
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnWeChatHide(GeneralCallbackResult result) { _wxHidden = true; UpdateSuspended(); }
        private void OnWeChatShow(OnShowListenerResult result) { _wxHidden = false; UpdateSuspended(); }
#endif
        private void OnDestroy()
        {
            GameEvent.RemoveEventListener(EventDefine.SudokuBackRequested, OnBackRequested);
#if UNITY_WEBGL && !UNITY_EDITOR
            WX.OffHide(OnWeChatHide); WX.OffShow(OnWeChatShow);
#endif
        }
    }
}

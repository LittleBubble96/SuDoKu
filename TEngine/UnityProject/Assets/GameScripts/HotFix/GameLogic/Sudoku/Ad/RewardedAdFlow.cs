using GameSDK;
using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using TEngine;

namespace GameLogic
{
    /// <summary>一个广告位一个执行流；实例版本阻止旧实例回调影响新请求。</summary>
    internal sealed class RewardedAdFlow
    {
        private readonly string _adId;
        private bool _disposed, _pageActive, _loadPending;
        private IRewardedVideoAd _ad;
        // 每次销毁/重建递增。异步闭包捕获版本，迟到回调必须先校验。
        private int _instanceVersion, _loadVersion, _retryCount;
        private string _loadId;
        private double _loadStarted;
        private Request _request;
        public RewardedAdFlow(string adId) { _adId = adId; }
        public RewardedAdState State { get; private set; }
        public bool IsAdAvailable => !_disposed && State == RewardedAdState.Ready && _request == null;
        public bool IsLoading => State == RewardedAdState.Loading;
        public bool IsBusy => _request != null;
        public string LastRewardRequestId { get; private set; }
        private string Placement => _adId == AdIds.Tip ? "hint" : _adId == AdIds.DailySignIn ? "sign_in" :
            _adId == AdIds.DailyTask ? "daily_task" : "settlement";
        private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        private sealed class Request
        {
            public AdTelemetry.Attempt Telemetry;
            public string BiId => Telemetry.Id;
            public double Started;
            public bool Shown, Retried, Overlay = true;
            public IDisposable InputLock;
            public readonly UniTaskCompletionSource<bool> Result = new UniTaskCompletionSource<bool>();
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void Trace(string name, string status, string reason = "", Request request = null, double duration = 0)
        {
            Log.Info($"[Ad][{_adId}][instance={_instanceVersion}][request={request?.BiId}][load={_loadId}] {name} {status} {reason} duration={duration:F3}s");
        }

        private void SetState(RewardedAdState state)
        {
            if (State == state) return;
            Trace("state", state.ToString());
            State = state;
            AdSystem.NotifyStateChanged(_adId);
        }
        private bool IsCurrent(int version)
        {
            if (!_disposed && version == _instanceVersion) return true;
            Trace("stale_callback", "ignored");
            return false;
        }
        public void EnterPage() { _pageActive = true; _retryCount = 0; Preload(); }
        public void ExitPage()
        {
            _pageActive = false;
            if (_request != null) Finish(_request, false, true, "page_exit");
        }
        public void Preload()
        {
            if (_disposed || IsBusy || IsLoading || IsAdAvailable) return;
            int load = ++_loadVersion;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _loadId = Guid.NewGuid().ToString("N");
            _loadStarted = Now;
#endif
            _loadPending = true;
            SetState(RewardedAdState.Loading);
            Trace("ad_load", "start");
            try
            {
                if (_ad == null)
                {
                    int version = ++_instanceVersion;
                    _ad = SDK.CreateRewardedVideoAd(_adId,
                        () =>
                        {
                            if (!IsCurrent(version) || _request != null) return;
                            if (_loadPending) FinishLoad(true, "onLoad");
                            else SetState(RewardedAdState.Ready);
                        },
                        (code, message) =>
                        {
                            if (!IsCurrent(version)) return;
                            string error = code + ": " + message;
                            Trace("ad_error", "error", error, _request);
                            if (_request == null) FinishLoad(false, error);
                            else if (_request.Shown) Finish(_request, false, true, error);
                            // 展示尚未成功时由 Show 的失败回调重试；无回调则由15秒计时兜底。
                        },
                        rewarded =>
                        {
                            if (!IsCurrent(version) || _request == null) return;
                            var request = _request;
                            Trace("ad_close", rewarded ? "completed" : "interrupted", "onClose", request, Now - request.Started);
                            Finish(request, rewarded && request.Shown, false, rewarded ? "completed" : "interrupted");
                        });
                }
                int current = _instanceVersion;
                _ad.Load(() =>
                {
                    if (IsCurrent(current) && load == _loadVersion && _request == null) FinishLoad(true, "load_success");
                }, error =>
                {
                    if (IsCurrent(current) && load == _loadVersion && _request == null) FinishLoad(false, error);
                });
                WatchLoadAsync(load, current).Forget();
            }
            catch (Exception error) { FinishLoad(false, error.Message); }
        }
        private void FinishLoad(bool success, string reason)
        {
            if (!_loadPending) return;
            _loadPending = false;
            Trace("ad_load", success ? "success" : "failed", reason, null, Now - _loadStarted);
            SetState(success ? RewardedAdState.Ready : RewardedAdState.Failed);
            if (!success && _pageActive && _retryCount++ < 2) RetryLoadAsync(_instanceVersion).Forget();
        }
        private async UniTaskVoid WatchLoadAsync(int load, int version)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(AdSystem.LoadingTimeoutSeconds), DelayType.Realtime);
            if (!_disposed && version == _instanceVersion && load == _loadVersion && _loadPending)
                FinishLoad(false, "load_callback_timeout");
        }
        private async UniTaskVoid RetryLoadAsync(int version)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(10), DelayType.Realtime);
            if (!_disposed && _pageActive && version == _instanceVersion && State == RewardedAdState.Failed) Preload();
        }
        public async UniTask<bool> ShowRewardedAdAsync()
        {
            if (!IsAdAvailable)
            {
                new AdTelemetry.Attempt("rewarded_ad", _adId, Placement).Finish("failed", "not_ready");
                return false;
            }
            var request = new Request { Started = Now, InputLock = UIInteractionLock.Acquire(),
                Telemetry = new AdTelemetry.Attempt("rewarded_ad", _adId, Placement) };
            _request = request;
            AdSystem.OpenOverlay(request.BiId);
            SetState(RewardedAdState.Showing);
            Trace("ad_request", "start", "", request);
            WatchRequestAsync(request).Forget();
            OpenAndShowAsync(request).Forget();
            return await request.Result.Task;
        }
        private async UniTaskVoid OpenAndShowAsync(Request request)
        {
            try
            {
                await GameModule.UI.ShowUIAsyncAwait<UIAd>(request.BiId);
                if (_request != request || !request.Overlay) return;
                ShowNative(request);
            }
            catch (Exception error) { Finish(request, false, true, "open_or_show_exception: " + error.Message); }
        }
        private void ShowNative(Request request)
        {
            Trace("ad_show", "start", request.Retried ? "retry" : "initial", request);
            _ad.Show(() =>
            {
                if (_request != request || request.Shown) return;
                request.Shown = true;
                request.Telemetry.Shown();
                Trace("ad_show", "success", "", request, Now - request.Started);
            }, error =>
            {
                if (_request != request) return;
                Trace("ad_show", "failed", error, request, Now - request.Started);
                if (request.Retried) { Finish(request, false, true, error); return; }
                request.Retried = true;
                Trace("ad_reload", "start", "show_failed", request);
                try
                {
                    _ad.Load(() =>
                    {
                        if (_request != request) return;
                        Trace("ad_reload", "success", "", request);
                        try { ShowNative(request); }
                        catch (Exception ex) { Finish(request, false, true, ex.Message); }
                    }, failure =>
                    {
                        if (_request != request) return;
                        Trace("ad_reload", "failed", failure, request);
                        Finish(request, false, true, failure);
                    });
                }
                catch (Exception ex) { Finish(request, false, true, ex.Message); }
            });
        }
        private async UniTaskVoid WatchRequestAsync(Request request)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(AdSystem.LoadingTimeoutSeconds), DelayType.Realtime);
            if (_request != request) return;
            CloseOverlay(request);
            if (!request.Shown) { Finish(request, false, true, "show_callback_timeout"); return; }
            // 正常视频可超过15秒；展示成功但永远没有close/error，也必须有限时结束请求。
            double remaining = AdSystem.CloseCallbackTimeoutSeconds - (Now - request.Started);
            if (remaining > 0) await UniTask.Delay(TimeSpan.FromSeconds(remaining), DelayType.Realtime);
            if (_request == request) Finish(request, false, true, "close_callback_timeout");
        }
        private void CloseOverlay(Request request)
        {
            if (!request.Overlay) return;
            request.Overlay = false;
            request.InputLock?.Dispose();
            request.InputLock = null;
            AdSystem.CloseOverlay(request.BiId);
        }
        private void Finish(Request request, bool rewarded, bool resetNative, string reason)
        {
            if (_request != request) return;
            _request = null;
            string outcome = rewarded ? "completed" : request.Shown && reason == "interrupted" ? "interrupted" : "failed";
            string failure = rewarded ? "" : reason == "interrupted" ? "user_close" :
                reason == "page_exit" ? "page_exit" : reason == "show_callback_timeout" ? "show_timeout" :
                reason == "close_callback_timeout" ? "close_timeout" : "sdk_error";
            request.Telemetry.Finish(outcome, failure);
            if (rewarded) LastRewardRequestId = request.BiId;
            Trace("ad_result", rewarded ? "rewarded" : "failed", reason, request, Now - request.Started);
            CloseOverlay(request);
            if (resetNative) ResetNative();
            SetState(RewardedAdState.Idle);
            request.Result.TrySetResult(rewarded);
            // 离开原生回调栈后再加载，避免在onClose里同步开始下一轮。
            if (_pageActive && !_disposed) PreloadNextAsync(_instanceVersion).Forget();
        }
        private async UniTaskVoid PreloadNextAsync(int version)
        {
            await UniTask.Delay(TimeSpan.FromMilliseconds(250), DelayType.Realtime);
            if (!_disposed && _pageActive && version == _instanceVersion) Preload();
        }
        public void Dispose()
        {
            _disposed = true;
            ExitPage();
            ResetNative();
            SetState(RewardedAdState.Idle);
        }
        private void ResetNative()
        {
            ++_instanceVersion;
            ++_loadVersion;
            _loadPending = false;
            var old = _ad;
            _ad = null;
            try { old?.Dispose(); }
            catch (Exception error) { Log.Warning($"[Ad][{_adId}] dispose failed: {error.Message}"); }
        }
    }
}

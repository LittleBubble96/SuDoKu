using System;
using System.Collections.Generic;
using TEngine;
using WeChatWASM;

namespace GameSDK
{
    public class WXSDK : ISdk
    {
        private WXInterstitialAd _interstitial;
        private string _interstitialId;
        private bool _interstitialReady;
        private Action _interstitialFinished;
        private AdTelemetry.Attempt _interstitialTelemetry;
        private bool _bannerErrorReported;
        private WXBannerAd _banner;
        private string _bannerId;
        private bool _bannerRequested;
        private bool _nativeReady;
        private int _loginAttempt;
        private int _pendingCompletedLevel;
        private int _uploadedCompletedLevel;
        private bool _completedLevelUploading;
        private const string RecommendOpenLink =
            "TWFRCqV5WeM2AkMXhKwJ03MhfPOieJfAsvXKUbWvQFQtLyyA5etMPabBehga950uzfZcH3Vi3QeEh41xRGEVFw";
        private WXPageManager _recommendPageManager;
        private bool _recommendRequested;
        private bool _recommendLoading;
        private bool _recommendLoaded;
        private bool _recommendShowing;
        private bool _recommendShown;
        private bool _recommendUnsupported;
        private Func<bool> _recommendCanShow;
        [Serializable]
        private sealed class OpenIdResponse { public string openid; }

        public void Login(Action<string> onSuccess, Action<string> onFailure)
        {
            int attempt = ++_loginAttempt;
            Action login = () => WX.Login(new LoginOption {
                success = result =>
                {
                    if (attempt != _loginAttempt) return;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(result.code)) { onFailure("empty_login_code"); return; }
                        WXBase.cloud.Init(new ICloudConfig { env = "cloud1-d8gh27cku5807f11b", traceUser = true });
                        WXBase.cloud.CallFunction(new CallFunctionParam {
                            name = "getOpenid", data = new {},
                            success = response =>
                            {
                                if (attempt != _loginAttempt) return;
                                try
                                {
                                    var user = UnityEngine.JsonUtility.FromJson<OpenIdResponse>(response.result);
                                    if (user == null || string.IsNullOrWhiteSpace(user.openid)) { onFailure("empty_openid"); return; }
                                    onSuccess(user.openid);
                                    // 只保存本次会话接受的身份；旧尝试的迟到回调不能覆盖它。
                                    if (SDK.IsLoggedIn)
                                    {
                                        PlayerPrefs.SetString(PlayerPrefsOpenIdKey, SDK.OpenId);
                                        PlayerPrefs.Save();
                                    }
                                }
                                catch (Exception e) { onFailure(e.Message); }
                            },
                            fail = error => { if (attempt == _loginAttempt) onFailure("cloud: " + error.errMsg); }
                        });
                    }
                    catch (Exception e) { onFailure(e.Message); }
                },
                fail = error => { if (attempt == _loginAttempt) onFailure("login: " + error.errMsg); }
            });
            if (_nativeReady) { login(); return; }
            WXBase.InitSDK(_ =>
            {
                if (attempt != _loginAttempt) return;
                _nativeReady = true;
                PreloadInterstitial(_interstitialId);
                FlushCompletedLevel();
                if (_bannerRequested) ShowBanner(_bannerId);
                if (_recommendRequested) LoadRecommend();
                try { login(); } catch (Exception e) { onFailure(e.Message); }
            });
        }

        public IRewardedVideoAd CreateRewardedVideoAd(string adId, Action onLoaded,
            Action<int ,string> onError, Action<bool> onClosed)
        {
            if (string.IsNullOrWhiteSpace(adId)) throw new ArgumentException("微信广告位 ID 不能为空", nameof(adId));
            return new WeChatRewardedVideoAd(adId, onLoaded, onError, onClosed);
        }

        private sealed class WeChatRewardedVideoAd : IRewardedVideoAd
        {
            private readonly WXRewardedVideoAd _native;
            private readonly string _adId;
            [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
            private void Trace(string message) => TEngine.Log.Info($"[AdSDK][{_adId}] {message}");
            private readonly Action<WXADLoadResponse> _loaded;
            private readonly Action<WXADErrorResponse> _error;
            private readonly Action<WXRewardedVideoAdOnCloseResponse> _closed;
            private bool _disposed;
            public WeChatRewardedVideoAd(string adId, Action onLoaded, Action<int ,string> onError, Action<bool> onClosed)
            {
                _adId = adId;
                Trace("create multiton=true");
                _native = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam { adUnitId = adId, multiton = true });
                _loaded = _ => { Trace($"onLoad disposed={_disposed}"); if (!_disposed) onLoaded?.Invoke(); };
                _error = e => { Trace($"onError disposed={_disposed} code={e.errCode} message={e.errMsg}"); if (!_disposed) onError?.Invoke(e.errCode , e.errMsg); };
                _closed = result => { Trace($"onClose disposed={_disposed} isEnded={result?.isEnded} legacyNull={result == null}"); if (!_disposed) onClosed?.Invoke(result == null || result.isEnded); };
                _native.OnLoad(_loaded);
                _native.OnError(_error);
                _native.OnClose(_closed);
            }
            public void Load(Action onLoaded, Action<string> onFailed)
            {
                if (_disposed) { onFailed?.Invoke("广告实例已释放"); return; }
                Trace("load begin");
                _native.Load(_ => { Trace($"load success disposed={_disposed}"); if (!_disposed) onLoaded?.Invoke(); },
                    e => { Trace($"load fail code={e.errCode} message={e.errMsg}"); if (!_disposed) onFailed?.Invoke($"{e.errCode}: {e.errMsg}"); });
            }
            public void Show(Action onShown, Action<string> onFailed)
            {
                if (_disposed) { onFailed?.Invoke("广告实例已释放"); return; }
                Trace("show begin");
                _native.Show(_ => { Trace($"show success disposed={_disposed}"); if (!_disposed) onShown?.Invoke(); },
                    e => { Trace($"show fail {e.errMsg}"); if (!_disposed) onFailed?.Invoke(e.errMsg); });
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Trace("dispose: offLoad/offError/offClose/destroy");
                try
                {
                    _native.OffLoad(_loaded);
                    _native.OffError(_error);
                    _native.OffClose(_closed);
                }
                finally { _native.Destroy(); }
            }
        }

        public void ShowBanner(string adId)
        {
            if (string.IsNullOrWhiteSpace(adId)) return;
            if (_bannerId != adId) DestroyBanner();
            _bannerId = adId;
            _bannerRequested = true;
            _bannerErrorReported = false;
            if (!_nativeReady) return;
            try
            {
                if (_banner == null)
                {
                    var window = WX.GetWindowInfo();
                    int width = (int)(window.windowWidth * 0.6f);
                    _banner = WX.CreateBannerAd(new WXCreateBannerAdParam
                    {
                        adUnitId = adId,
                        adIntervals = 30,
                        style = new Style
                        {
                            left = 0,
                            top = (int)window.windowHeight - 70,
                            width = width,
                        }
                    });
                    Log.Info($"[Banner] create adId={adId} width={width} window=({window.windowWidth},{window.windowHeight})");
                    _banner.OnResize(OnBannerResize);
                    _banner.OnError(OnBannerError);
                }
                var banner = _banner;
                banner.Show(_ =>
                {
                    Log.Info($"[Banner] show adId={adId}");
                    // A pending native show may finish after leaving the board.
                    if (!ReferenceEquals(_banner, banner)) return;
                    if (!_bannerRequested) banner.Hide();
                    else AdTelemetry.BannerShown(adId);
                }, error =>
                {
                    if (ReferenceEquals(_banner, banner))
                        OnBannerError(new WXADErrorResponse { errMsg = error.errMsg });
                });
            }
            catch (Exception error)
            {
                OnBannerError(new WXADErrorResponse { errMsg = error.Message });
            }
        }

        private void OnBannerResize(WXADResizeResponse size)
        {
            if (_banner == null) return;
            var window = WX.GetWindowInfo();
            double bottom = window.safeArea == null ? window.windowHeight :
                Math.Min(window.windowHeight, window.safeArea.bottom - window.screenTop);
            _banner.style.left = (int)((window.windowWidth - size.width) / 2);
            _banner.style.top = (int)Math.Max(0, bottom - size.height);
        }

        private void OnBannerError(WXADErrorResponse error)
        {
            if (!_bannerErrorReported)
            {
                _bannerErrorReported = true;
                AdTelemetry.BannerError(_bannerId, error.errCode.ToString(System.Globalization.CultureInfo.InvariantCulture), error.errMsg);
            }
            AdTelemetry.BannerHidden("ad_error");
            _banner?.Hide();
            UnityEngine.Debug.LogWarning($"[Banner] {error.errCode}: {error.errMsg}");
        }

        public void HideBanner()
        {
            Log.Info($"[Banner] hide adId={_bannerId}");
            AdTelemetry.BannerHidden("hide");
            _bannerRequested = false;
            _banner?.Hide();
        }

        public void DestroyBanner()
        {
            AdTelemetry.BannerHidden("destroy");
            _bannerRequested = false;
            var banner = _banner;
            _banner = null;
            _bannerId = null;
            if (banner == null) return;
            banner.OffResize(OnBannerResize);
            banner.OffError(OnBannerError);
            banner.Destroy();
        }

        public void PreloadInterstitial(string adId)
        {
            if (string.IsNullOrWhiteSpace(adId)) return;
            if (_interstitialId != adId) DestroyInterstitial();
            _interstitialId = adId;
            if (!_nativeReady || _interstitial != null) return;
            try
            {
                // Creation loads the first ad; WeChat loads the next after closing.
                _interstitial = WX.CreateInterstitialAd(new WXCreateInterstitialAdParam { adUnitId = adId });
                _interstitial.OnLoad(OnInterstitialLoaded);
                _interstitial.OnClose(OnInterstitialClosed);
                _interstitial.OnError(OnInterstitialError);
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogWarning($"[Interstitial] preload failed: {error.Message}");
            }
        }

        private void OnInterstitialLoaded(WXADLoadResponse _) => _interstitialReady = true;

        public bool TryShowInterstitial(Action onFinished)
        {
            if (!_interstitialReady || _interstitialFinished != null) return false;
            _interstitialReady = false;
            _interstitialFinished = onFinished;
            var ad = _interstitial;
            var telemetry = _interstitialTelemetry = new AdTelemetry.Attempt("interstitial_ad", _interstitialId, "board_interstitial");
            try
            {
                ad.Show(_ => telemetry.Shown(), error =>
                {
                    if (ReferenceEquals(_interstitial, ad) && ReferenceEquals(_interstitialTelemetry, telemetry))
                        OnInterstitialError(new WXADErrorResponse { errMsg = error.errMsg });
                });
                return true;
            }
            catch (Exception error)
            {
                Log.Warning($"[Interstitial] show failed: {error.Message}");
                _interstitialTelemetry?.Finish("failed", "sdk_error");
                FinishInterstitial();
                return false;
            }
        }

        private void OnInterstitialError(WXADErrorResponse error)
        {
            _interstitialReady = false;
            _interstitialTelemetry?.Finish("failed", "sdk_error");
            Log.Warning($"[Interstitial] {error.errCode}: {error.errMsg}");
            FinishInterstitial();
        }

        private void OnInterstitialClosed()
        {
            _interstitialTelemetry?.Finish("closed");
            FinishInterstitial();
        }

        private void FinishInterstitial()
        {
            _interstitialTelemetry = null;
            var finished = _interstitialFinished;
            _interstitialFinished = null;
            finished?.Invoke();
        }

        public void DestroyInterstitial()
        {
            _interstitialTelemetry?.Finish("failed", "destroy");
            var ad = _interstitial;
            _interstitial = null;
            _interstitialId = null;
            _interstitialReady = false;
            if (ad != null)
            {
                ad.OffLoad(OnInterstitialLoaded);
                ad.OffClose(OnInterstitialClosed);
                ad.OffError(OnInterstitialError);
                ad.Destroy();
            }
            FinishInterstitial();
        }

        private const string PlayerPrefsOpenIdKey = "WX_openid";

        private const string GameCenterOpenLink =
            "-SSEykJvFV3pORt5kTNpS2E11-BKl-yDwkVN7YbIrwh8mrf-jNsbrYF5YAUq8VwUNohREU1hHQqZcpWAy4XH1jQY2UsgiBWh1YrMP4lhVxguTjCy8itFafKwftjHeRoZbCY4CzWmYPdRSQpVPaLdLn5XdRGIzqZO2evCNsDBILfRCqg3Nf7eGR-RYCMtLDeSEh2RbdeBcwJlvH3ty3RNXqzdJsOnIMNp6c8evobp7RQm920OojrgU75wakxNXW6wBe80qPlxI7guUMwKYwgUYzJzOwognSfL7fu34IxtRF7adHz_7yJ3pMg51ko_xt8Dvk9Ej5tfBVZdX0K4fYmFLw";
        
        public string GetOpenId()
        {
            return SDK.OpenId;
        }

        public void ShareAppMessage(string title)
        {
            WX.ShareAppMessage(new ShareAppMessageOption {
                title=title,
                query=$"inviter={GetOpenId()}&sid={Guid.NewGuid():N}"
            });
        }

        public void NavigateToMiniGame(string appId)
        {
            string target = appId == "wx308052a1ea617d67" ? "xunzi" : appId;
            WX.NavigateToMiniProgram(new NavigateToMiniProgramOption
            {
                appId = appId,
                envVersion = "release",
                success = _ => ReportNavigation(target, "success"),
                fail = error => {
                    bool canceled = error.errMsg != null && error.errMsg.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) >= 0;
                    ReportNavigation(target, canceled ? "cancel" : "failed", canceled ? "canceled" : "sdk_error");
                    UnityEngine.Debug.LogWarning($"[OtherGame] {error.errMsg}");
                }
            });
        }

        private static void ReportNavigation(string target, string result, string reason = null)
        {
            var bi = BiManager.OtherGameNavigateResult;
            bi.SetData(target, result, reason);
            bi.Report();
        }

        public void CreateGameClubButton()
        {
            WXCreateGameClubButtonParam param = new WXCreateGameClubButtonParam();
            GameClubButtonStyle clubButtonStyle = new GameClubButtonStyle();
            clubButtonStyle.left = 10;
            clubButtonStyle.top = 76;
            clubButtonStyle.width = 40;
            clubButtonStyle.height = 40;
            param.style = clubButtonStyle;
            param.type = GameClubButtonType.text;
            WX.CreateGameClubButton(param);
        }

        public void OpenGameClub()
        {
            WXPageManager pageManager = WX.CreatePageManager();
            pageManager.Load(new LoadOption()
            {
                openlink = GameCenterOpenLink,
                success = (result) =>
                {
                    pageManager.Show(new ShowOption()
                    {
                        openlink = GameCenterOpenLink,
                    });
                },
            });
        }

        public void ReportGameStart()
        {
            WX.ReportGameStart();
        }

        public void UploadCompletedLevel(int levelId)
        {
            if (levelId < 1) return;
            // 同一会话只上传最高通关号，串行写入，避免旧请求晚到覆盖新进度。
            _pendingCompletedLevel = Math.Max(_pendingCompletedLevel, levelId);
            FlushCompletedLevel();
        }

        private void FlushCompletedLevel()
        {
            if (!_nativeReady || _completedLevelUploading || _pendingCompletedLevel <= _uploadedCompletedLevel) return;
            int levelId = _pendingCompletedLevel;
            _completedLevelUploading = true;
            try
            {
                WX.SetUserCloudStorage(new SetUserCloudStorageOption
                {
                    KVDataList = new[]
                    {
                        new KVData { key = "ToLevel", value = levelId.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                    },
                    success = _ =>
                    {
                        _completedLevelUploading = false;
                        _uploadedCompletedLevel = levelId;
                        FlushCompletedLevel();
                    },
                    fail = error => CompletedLevelUploadFailed(levelId, error.errMsg)
                });
            }
            catch (Exception e) { CompletedLevelUploadFailed(levelId, e.Message); }
        }

        private void CompletedLevelUploadFailed(int levelId, string message)
        {
            _completedLevelUploading = false;
            Log.Warning($"[Leaderboard] ToLevel={levelId} 上传失败: {message}");
            // 保留待上传值，下次通关再尝试；已有更高关卡等待时直接提交最新值。
            if (_pendingCompletedLevel > levelId) FlushCompletedLevel();
        }

        public void LoadRecommend()
        {
            _recommendRequested = true;
            if (!_nativeReady || _recommendUnsupported || _recommendLoading || _recommendLoaded || _recommendShown) return;
            try
            {
                if (!WX.CanIUse("createPageManager"))
                {
                    _recommendUnsupported = true;
                    _recommendCanShow = null;
                    Log.Warning("[Recommend] 当前基础库不支持评价与推荐组件");
                    return;
                }
                _recommendPageManager ??= WX.CreatePageManager();
                _recommendLoading = true;
                _recommendPageManager.Load(new LoadOption
                {
                    openlink = RecommendOpenLink,
                    success = _ =>
                    {
                        _recommendLoading = false;
                        _recommendLoaded = true;
                        TryShowRecommend();
                    },
                    fail = error => RecommendFailed("load", error.errMsg)
                });
            }
            catch (Exception e) { RecommendFailed("load", e.Message); }
        }

        public void ShowRecommend(Func<bool> canShow)
        {
            if (_recommendShown || _recommendShowing || _recommendUnsupported || canShow == null || !canShow()) return;
            _recommendCanShow = canShow;
            if (_recommendLoaded) TryShowRecommend();
            else LoadRecommend();
        }

        private void TryShowRecommend()
        {
            if (!_recommendLoaded || _recommendShowing || _recommendShown || _recommendCanShow == null) return;
            var canShow = _recommendCanShow;
            _recommendCanShow = null;
            if (!canShow()) return; // 结算页已关闭或正在离场，不补弹。
            _recommendShowing = true;
            try
            {
                _recommendPageManager.Show(new ShowOption
                {
                    openlink = RecommendOpenLink,
                    success = _ => { _recommendShowing = false; _recommendShown = true; },
                    fail = error => RecommendFailed("show", error.errMsg)
                });
            }
            catch (Exception e) { RecommendFailed("show", e.Message); }
        }

        private void RecommendFailed(string stage, string message)
        {
            _recommendLoading = false;
            _recommendShowing = false;
            _recommendCanShow = null;
            Log.Warning($"[Recommend] {stage} failed: {message}");
        }

        public void ReportEvent(string eventId , Dictionary<string,string> data)
        {
            WX.ReportEvent(eventId,data);
        }
    }
}

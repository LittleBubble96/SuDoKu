using System;
using System.Diagnostics;

namespace GameSDK
{
    public static class AdTelemetry
    {
        public const long BannerIntervalMs = 30 * 60 * 1000;
        private static RoundContext _round, _bannerRound;
        private static bool _bannerVisible;
        private static string _bannerId, _exposureId;
        private static long _bannerReported;
        private static double _bannerStarted;
        private static int _segment;
        private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

        // 不随换局改变，广告异步回调仍关联展示时的对局。
        private readonly struct RoundContext
        {
            public readonly string Id, Mode, Date;
            public readonly int Difficulty, LevelId;
            public RoundContext(string id, int difficulty, int levelIndex, bool daily, string date)
            {
                Id = id;
                Difficulty = difficulty;
                LevelId = levelIndex + 1;
                Mode = daily ? "daily" : "normal";
                Date = daily ? date : null;
            }
        }
        public static void SetRound(string id, int difficulty = 0, int levelIndex = 0, bool daily = false, string date = null) =>
            _round = string.IsNullOrEmpty(id) ? default : new RoundContext(id, difficulty, levelIndex, daily, date);

        private static RoundContext PlacementRound(string placement) =>
            placement == "hint" || placement == "board_banner" || placement == "board_interstitial" || placement == "settlement"
                ? _round : default;

        public sealed class Attempt
        {
            public readonly string Id = Guid.NewGuid().ToString("N");
            private readonly string _adId, _placement;
            private readonly RoundContext _round;
            private readonly bool _rewarded;
            private readonly double _started = Now;
            private bool _shown, _finished;
            public Attempt(string prefix, string adId, string placement)
            {
                _adId = adId;
                _placement = placement;
                _round = PlacementRound(placement);
                _rewarded = prefix == "rewarded_ad";
            }
            public void Shown()
            {
                if (_finished || _shown) return;
                _shown = true;
                if (_rewarded)
                    BiManager.RewardedAdShow.SetData(_adId, _placement, Id,
                        _round.Id, _round.Difficulty, _round.LevelId, _round.Mode, _round.Date).Report();
                else
                    BiManager.InterstitialAdShow.SetData(_adId, _placement, Id,
                        _round.Id, _round.Difficulty, _round.LevelId, _round.Mode, _round.Date).Report();
            }
            public void Finish(string result, string reason = null)
            {
                if (_finished) return;
                _finished = true;
                if (result == "completed" && !_shown) result = "failed";
                if (result == "failed" && string.IsNullOrEmpty(reason)) reason = "show_unconfirmed";
                long elapsed = (long)Math.Max(0, (Now - _started) * 1000);
                if (string.IsNullOrEmpty(reason)) reason = null;
                if (_rewarded)
                    BiManager.RewardedAdResult.SetData(_adId, _placement, Id, _shown ? 1 : 0, result, elapsed, reason,
                        _round.Id, _round.Difficulty, _round.LevelId, _round.Mode, _round.Date).Report();
                else
                    BiManager.InterstitialAdResult.SetData(_adId, _placement, Id, _shown ? 1 : 0, result, elapsed, reason,
                        _round.Id, _round.Difficulty, _round.LevelId, _round.Mode, _round.Date).Report();
            }
        }
        public static void BannerError(string adId, string code, string message)
        {
            var round = _bannerVisible ? _bannerRound : _round;
            BiManager.BannerError.SetData(adId, code, message,
                round.Id, round.Difficulty, round.LevelId, round.Mode, round.Date).Report();
        }
        public static void BannerShown(string adId)
        {
            if (_bannerVisible) return;
            _bannerVisible = true;
            _bannerRound = _round;
            _bannerId = adId;
            _exposureId = Guid.NewGuid().ToString("N");
            _bannerStarted = Now;
            _bannerReported = 0;
            _segment = 0;
            BiManager.BannerImpression.SetData(_bannerId, _exposureId,
                _bannerRound.Id, _bannerRound.Difficulty, _bannerRound.LevelId, _bannerRound.Mode, _bannerRound.Date).Report();
        }
        public static void Tick()
        {
            if (!_bannerVisible) return;
            long elapsed = (long)Math.Max(0, (Now - _bannerStarted) * 1000);
            while (elapsed - _bannerReported >= BannerIntervalMs)
                BannerSegment(BannerIntervalMs, "interval");
        }
        private static void BannerSegment(long milliseconds, string reason)
        {
            if (milliseconds <= 0) return;
            BiManager.BannerVisibleTime.SetData(_bannerId, _exposureId, ++_segment, milliseconds, reason,
                _bannerRound.Id, _bannerRound.Difficulty, _bannerRound.LevelId, _bannerRound.Mode, _bannerRound.Date).Report();
            _bannerReported += milliseconds;
        }
        public static void BannerHidden(string reason)
        {
            if (!_bannerVisible) return;
            Tick();
            long elapsed = (long)Math.Max(0, (Now - _bannerStarted) * 1000);
            BannerSegment(elapsed - _bannerReported, reason);
            _bannerVisible = false;
        }
    }
}

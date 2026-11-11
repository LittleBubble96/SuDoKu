using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameSDK
{
    public static class StartupTelemetry
    {
        private static readonly string[] Steps = { "", "launch", "init_package", "request_version",
            "update_manifest", "create_downloader", "download_patch", "clear_cache", "preload",
            "load_assembly", "init_sdk", "init_config", "init_manager", "init_system", "load_top_ui",
            "load_home", "finalize_startup", "complete" };
        private sealed class StepState
        {
            public int Attempt;
            public long StartedAt;
            public bool Running;
        }
        private static readonly Dictionary<int, StepState> States = new Dictionary<int, StepState>();
        private static readonly Stopwatch Clock = new Stopwatch();
        private static string _startupId, _appVersion, _playMode;
        private static bool _completed;
        public static bool Enabled { get; set; } = true;
        public static string ResourceVersion { get; set; } = "";

        public static void Begin(string appVersion, string playMode)
        {
            States.Clear();
            _completed = false;
            ResourceVersion = "";
            _startupId = Guid.NewGuid().ToString("N");
            _appVersion = appVersion;
            _playMode = playMode;
            Clock.Restart();
            Start(1);
        }
        public static void Start(int index)
        {
            if (_startupId == null || _completed || index < 1 || index >= 17 || index == 10) return;
            if (!States.TryGetValue(index, out var state)) States[index] = state = new StepState();
            if (state.Running) return;
            state.Attempt++;
            state.StartedAt = Clock.ElapsedMilliseconds;
            state.Running = true;
            Report(index, "start", state);
        }
        public static void Success(int index) => Finish(index, "success");
        public static void Success(int index, int downloadCount, long downloadBytes) =>
            Finish(index, "success", downloadCount: downloadCount, downloadBytes: downloadBytes);
        public static void Fail(int index, string code, string message) =>
            Finish(index, "fail", code ?? "",
                string.IsNullOrEmpty(message) ? "" : message.Substring(0, Math.Min(256, message.Length)));

        private static void Finish(int index, string status,
            string errorCode = null, string errorMessage = null, int downloadCount = -1, long downloadBytes = -1)
        {
            if (_completed || !States.TryGetValue(index, out var state) || !state.Running) return;
            state.Running = false;
            Report(index, status, state, errorCode: errorCode, errorMessage: errorMessage,
                downloadCount: downloadCount, downloadBytes: downloadBytes);
        }
        public static void Skip(int index, string reason)
        {
            if (_startupId == null || _completed || index < 1 || index >= 17 || index == 10 || States.ContainsKey(index)) return;
            var state = new StepState { StartedAt = Clock.ElapsedMilliseconds };
            States[index] = state;
            Report(index, "skip", state, reason: reason);
        }
        public static void Complete()
        {
            if (_startupId == null || _completed) return;
            _completed = true;
            if (!Enabled) return;
            long now = Clock.ElapsedMilliseconds;
            BiManager.StartupComplete.SetData(_startupId, 17, Steps[17], "success", now, 1,
                _appVersion, ResourceVersion, _playMode, now).Report();
        }
        private static void Report(int index, string status, StepState state,
            string reason = null, string errorCode = null, string errorMessage = null,
            int downloadCount = -1, long downloadBytes = -1)
        {
            if (!Enabled) return;
            long now = Clock.ElapsedMilliseconds;
            BiManager.StartupStep.SetData(_startupId, index, Steps[index], status, now, state.Attempt,
                _appVersion, ResourceVersion, _playMode, status == "start" ? -1 : now - state.StartedAt,
                reason, errorCode, errorMessage, downloadCount, downloadBytes).Report();
        }
    }
}

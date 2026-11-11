using System;
using System.Collections.Generic;

namespace GameSDK
{
    public enum SdkLoginState { NotStarted, LoggingIn, LoggedIn, Failed }

    /// <summary>主线程使用。正常直发，登录前或失败事件保留快照并有限重试。</summary>
    internal sealed class SdkSession
    {
        private sealed class Event
        {
            public string Name;
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>(20);
            public int Failures;
            public bool NativeGameStart;
        }
        private readonly Queue<Event> _pending = new Queue<Event>();
        private readonly Stack<Event> _available = new Stack<Event>();
        private readonly Dictionary<string, string> _sendData = new Dictionary<string, string>(20);
        private readonly Func<double> _now;
        private readonly Action<string> _warn;
        private ISdk _platform;
        private int _generation;
        private int _attempt;
        private int _cycleAttempts;
        private double _loginDeadline;
        private double _retryLoginAt = double.PositiveInfinity;
        private double _retryReportAt;
        private bool _flushing;
        private int _reportBudget = 8;
        public const int ReportsPerTick = 8;
        public SdkLoginState LoginState { get; private set; }
        public bool IsLoggedIn => LoginState == SdkLoginState.LoggedIn;
        public string OpenId { get; private set; } = "";
        public int PendingCount => _pending.Count;

        public SdkSession(Func<double> now, Action<string> warn) { _now = now; _warn = warn; }
        public void Initialize(ISdk platform)
        {
            if (_platform != null) return;
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            StartLogin();
        }
        public void RetryLogin()
        {
            if (_platform == null || IsLoggedIn || LoginState == SdkLoginState.LoggingIn) return;
            _cycleAttempts = 0;
            StartLogin();
        }
        private void StartLogin()
        {
            LoginState = SdkLoginState.LoggingIn;
            OpenId = "";
            int token = ++_generation;
            _attempt++;
            _cycleAttempts++;
            _retryLoginAt = double.PositiveInfinity;
            _loginDeadline = _now() + 60;
            LoginEvent("start", "");
            try
            {
                _platform.Login(id =>
                {
                    if (token != _generation || LoginState != SdkLoginState.LoggingIn) return;
                    if (string.IsNullOrWhiteSpace(id)) { LoginFailed(token, "empty_openid"); return; }
                    OpenId = id;
                    LoginState = SdkLoginState.LoggedIn;
                    _retryReportAt = 0;
                    LoginEvent("success", "");
                    Flush();
                }, error => LoginFailed(token, error));
            }
            catch (Exception e) { LoginFailed(token, e.Message); }
        }
        private void LoginFailed(int token, string error)
        {
            if (token != _generation || LoginState != SdkLoginState.LoggingIn) return;
            LoginState = SdkLoginState.Failed;
            LoginEvent("fail", error);
            _warn("[SDK] Login failed: " + error);
            if (_cycleAttempts < 3) _retryLoginAt = _now() + Math.Pow(2, _cycleAttempts);
        }
        private void LoginEvent(string status, string error)
        {
            var bi = BiManager.SdkLogin;
            bi.SetData(status, _attempt,
                string.IsNullOrEmpty(error) ? "" : error.Substring(0, Math.Min(256, error.Length)));
            ReportEvent(bi.EventName, bi.Data);
        }
        /// <returns>接口已调用或已保留待发送快照，不代表服务端确认。</returns>
        public bool ReportEvent(string name, Dictionary<string, string> data) => Send(name, data, false);
        public bool ReportGameStart() => Send("native_game_start", null, true);

        private bool Send(string name, Dictionary<string, string> data, bool nativeGameStart)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            // 有积压时维持顺序；补发每帧最多 8 条，不限制正常直发。
            if (!IsLoggedIn || _flushing || _pending.Count > 0)
            {
                bool accepted = Enqueue(name, data, nativeGameStart);
                Flush();
                return accepted;
            }
            try
            {
                // 微信同步序列化，编辑器同步读取；返回后即可复用 Data。
                if (nativeGameStart) _platform.ReportGameStart();
                else _platform.ReportEvent(name, data);
                return true;
            }
            catch (Exception e)
            {
                if (!Enqueue(name, data, nativeGameStart)) return false;
                _pending.Peek().Failures = 1;
                _retryReportAt = _now() + 2;
                _warn("[BI] Report failed; event retained: " + e.Message);
                return true;
            }
        }
        private bool Enqueue(string name, Dictionary<string, string> data, bool nativeGameStart)
        {
            if (_pending.Count >= 512) { _warn("[BI] Queue full; dropped new event: " + name); return false; }
            var item = _available.Count > 0 ? _available.Pop() : new Event();
            item.Data.Clear();
            if (data != null)
                foreach (var pair in data) item.Data.Add(pair.Key, pair.Value ?? "");
            item.Name = name;
            item.NativeGameStart = nativeGameStart;
            item.Failures = 0;
            _pending.Enqueue(item);
            return true;
        }
        public void Tick()
        {
            _reportBudget = ReportsPerTick;
            if (LoginState == SdkLoginState.LoggingIn && _now() >= _loginDeadline)
                LoginFailed(_generation, "login_timeout");
            if (LoginState == SdkLoginState.Failed && _now() >= _retryLoginAt) StartLogin();
            Flush();
        }
        public void RetryPendingEvents()
        {
            if (_pending.Count > 0) _pending.Peek().Failures = 0;
            _retryReportAt = 0;
            Flush();
        }
        private void Flush()
        {
            if (_pending.Count == 0 || _reportBudget == 0 || !IsLoggedIn || _flushing || _now() < _retryReportAt) return;
            _flushing = true;
            try
            {
                while (_pending.Count > 0 && _reportBudget > 0)
                {
                    _reportBudget--;
                    var item = _pending.Peek();
                    try
                    {
                        if (item.NativeGameStart) _platform.ReportGameStart();
                        else
                        {
                            _sendData.Clear();
                            foreach (var pair in item.Data) _sendData.Add(pair.Key, pair.Value);
                            _platform.ReportEvent(item.Name, _sendData);
                        }
                    }
                    catch (Exception e)
                    {
                        item.Failures++;
                        _retryReportAt = item.Failures < 3 ? _now() + Math.Pow(2, item.Failures) : double.PositiveInfinity;
                        _warn("[BI] Report failed; event retained: " + e.Message);
                        break;
                    }
                    finally { _sendData.Clear(); }
                    _pending.Dequeue();
                    item.Data.Clear();
                    item.Name = null;
                    _available.Push(item);
                    _retryReportAt = 0;
                }
            }
            finally { _flushing = false; }
        }
    }
}

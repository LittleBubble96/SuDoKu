using System.Collections.Generic;
using System.Globalization;

namespace GameSDK
{
    /// <summary>主线程复用。SetData 与 Report 之间不跨 await。</summary>
    public abstract class BiEvent
    {
        public abstract string EventName { get; }
        public Dictionary<string, string> Data { get; } = new Dictionary<string, string>(20);

        protected void Put(string key, string value) => Data[key] = value ?? "";
        protected void Put(string key, int value) => Data[key] = value.ToString(CultureInfo.InvariantCulture);
        protected void Put(string key, long value) => Data[key] = value.ToString(CultureInfo.InvariantCulture);

        public bool Report() => SDK.ReportEvent(EventName, Data);
    }
}

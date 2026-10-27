using System;
using UnityEngine;

namespace GameLogic.Data
{
    /// <summary>设置页共用的缓存数据，跟随 GameCacheData 一起保存。</summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public string language = "Ch";
        public int themeId;
        public float MusicVolume = 1f;
        public float SoundVolume = 1f;
        public bool vibration;
        public bool highlightSameNumber = true;
        public bool highlightSameNote = true;
        public bool highlightRelated = true;
        public bool dimCompletedDigits = true;
        public bool showRemainingCount;

        public void SetMusicVolume(float value) => MusicVolume = Mathf.Clamp01(value);
        public void SetSoundVolume(float value) => SoundVolume = Mathf.Clamp01(value);
    }
}


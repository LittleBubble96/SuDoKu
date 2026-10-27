using System;
using GameLogic.Data;
using TEngine;

namespace GameLogic.Sudoku
{
    /// <summary>设置的即时应用和保存；首页与局内共用同一份数据。</summary>
    public static class SudokuSettingsManager
    {
        private static GameCacheManager _cache;
        private static Action _save;
        public static GameSettingsData Data => _cache.CacheData.gameSettingsData;

        public static void Initialize(GameCacheManager cache, Action save)
        {
            _cache = cache;
            _save = save;
            ApplyAudio();
            ApplyLanguage();
        }

        public static void SetAudioVolumes(float music, float sound)
        {
            Data.SetMusicVolume(music);
            Data.SetSoundVolume(sound);
            ApplyAudio();
            Save();
        }

        public static void SetSoundEnabled(bool enabled) => SetAudioVolumes(Data.MusicVolume, enabled ? 1f : 0f);

        public static void SetVibration(bool enabled)
        {
            Data.vibration = enabled;
            Save();
            if (enabled) Vibrate();
        }

        public static void SetHighlightSameNumber(bool enabled) { Data.highlightSameNumber = enabled; Save(); }
        public static void SetHighlightSameNote(bool enabled) { Data.highlightSameNote = enabled; Save(); }
        public static void SetHighlightRelated(bool enabled) { Data.highlightRelated = enabled; Save(); }
        public static void SetDimCompletedDigits(bool enabled) { Data.dimCompletedDigits = enabled; Save(); }
        public static void SetShowRemainingCount(bool enabled) { Data.showRemainingCount = enabled; Save(); }

        public static void SetLanguage(string language)
        {
            Data.language = language;
            ApplyLanguage();
            Save();
        }

        private static void ApplyAudio()
        {
            AudioSystem.Instance.SetMusicVolume(Data.MusicVolume);
            AudioSystem.Instance.SetSoundVolume(Data.SoundVolume);
        }

        private static void ApplyLanguage() =>
            GameModule.Localization.Language = Data.language == "En" ? Language.English : Language.ChineseSimplified;

        private static void Save()
        {
            _save();
            GameEvent.Send(EventDefine.SudokuStateChanged);
        }

        public static void Vibrate()
        {
            if (!Data.vibration) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            WeChatWASM.WX.VibrateShort(new WeChatWASM.VibrateShortOption { type = "light" });
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            UnityEngine.Handheld.Vibrate();
#endif
        }

        public static void Release() { _cache = null; _save = null; }
    }
}


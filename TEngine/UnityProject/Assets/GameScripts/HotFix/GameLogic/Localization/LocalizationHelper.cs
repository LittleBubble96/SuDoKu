using GameConfig;
using TEngine;

namespace GameLogic.Localization
{
    public static class LocalizationHelper
    {
        public static string GetLocalText(string key)
        {
            var entry = ConfigSystem.Instance.Tables.TbLanguageContent.GetOrDefault(key);
            if (entry?.Value == null || entry.Value.Length == 0) return key;
            int index = GameModule.Localization.Language == Language.English ? 1 : 0;
            return entry.Value[index < entry.Value.Length ? index : 0] ?? key;
        }
    }
}

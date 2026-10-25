#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig.trophy;
using GameLogic;
using GameLogic.Data;
using GameLogic.Localization;
using GameLogic.Sudoku;
using Luban;
using TEngine;
using TMPro;
using UnityEngine;

namespace SudokuEditor
{
    public static class SudokuTrophyUiValidation
    {
        private const string Top = "Panel/Content/Top/";
        public static async UniTask RunAsync(string output, CancellationToken token)
        {
            Application.runInBackground = true;
            var home = await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
            home.SelectDailyDate(new DateTime(2026, 1, 22)); await home.WaitForContentAsync(token);
            home.DailyPage.FindChildComponent<UnityEngine.UI.Button>("Top/Trophy/Btn").onClick.Invoke();
            var gallery = await GameModule.UI.GetUIAsyncAwait<UITrophyGallery>(token);
            await gallery.WaitForContentAsync(token);
            Require(gallery.IsOpen && gallery.Items.Count == 12 && gallery.SelectedYear == 2026, "daily entry opens twelve actual prefab widgets");
            Require(!gallery.FindChildComponent<UnityEngine.UI.Button>(Top + "Year/Left/Btn").interactable &&
                !gallery.FindChildComponent<UnityEngine.UI.Button>(Top + "Year/Right/Btn").interactable, "single configured year disables both navigation bounds");
            gallery.FindChildComponent<UnityEngine.UI.Button>(Top + "Back/Btn").onClick.Invoke();
            Require(!gallery.IsOpen && home.IsOpen && home.SelectedTab == 1 && home.DailyPage.SelectedDate == new DateTime(2026, 1, 22), "back preserves daily page and selected date");

            DateTime today = new DateTime(2026, 10, 25);
            var storage = new MemoryStorage(); var cache = new GameCacheManager(storage); await cache.LoadAsync();
            foreach (int month in new[] { 3, 7, 9 }) AddDays(cache.CacheData, 2026, month, DateTime.DaysInMonth(2026, month));
            AddDays(cache.CacheData, 2026, 10, 12);
            var configs = new List<ConfTrophy>(ConfigSystem.Instance.Tables.TbTrophy.DataList);
            foreach (var config in ConfigSystem.Instance.Tables.TbTrophy.DataList) configs.Add(NextYear(config));
            var game = new SudokuGameController(LoadPacks(), cache, () => today, configs);
            var oldLanguage = GameModule.Localization.Language; int oldTheme = SudokuThemeManager.CurrentThemeId;
            try
            {
                GameModule.Localization.Language = Language.ChineseSimplified; SudokuThemeManager.SetTheme(0);
                gallery = await GameModule.UI.ShowUIAsyncAwait<UITrophyGallery>(new UITrophyGallery.Context(game, 2026));
                await gallery.WaitForContentAsync(token);
                int firstItem = gallery.Items[0].gameObject.GetInstanceID();
                Require(gallery.FindChildComponent<TMP_Text>(Top + "Year/m_progress").text.Contains(">3</color> / 12"), "yearly collection count is real fixture data");
                var grid = gallery.FindChildComponent<UnityEngine.UI.GridLayoutGroup>("Panel/Content/Scroll/Viewport/Content");
                Require(grid.constraint == UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount && grid.constraintCount == 3 && grid.transform.childCount == 12, "three-column list has twelve items without template duplication");
                Check(gallery.Items[0], SudokuTrophyState.NotObtained, "未获得", "trophy_01_capricorn_gray", true, false, false);
                Check(gallery.Items[2], SudokuTrophyState.Obtained, "已获得", "trophy_03_pisces", false, true, false);
                Check(gallery.Items[9], SudokuTrophyState.InProgress, "进行中", "trophy_10_libra", false, false, true);
                Check(gallery.Items[10], SudokuTrophyState.NotStarted, "待开启", "trophy_11_scorpio_gray", true, false, false);
                Require(gallery.Items[2].FindChildComponent<TMP_Text>("m_getState").color == SudokuThemeManager.GetColor(SudokuThemeRole.Accent) &&
                    gallery.Items[0].FindChildComponent<TMP_Text>("m_getState").color == SudokuThemeManager.GetColor(SudokuThemeRole.MutedText), "obtained and locked status colors differ by theme role");
                Canvas.ForceUpdateCanvases(); UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.transform);
                Require(((RectTransform)grid.transform).rect.height >= 1440 && gallery.FindChildComponent<UnityEngine.UI.ScrollRect>("Panel/Content/Scroll").vertical, "content height and vertical scrolling cover fourth row");
                var bottom = new Vector3[4]; var viewport = new Vector3[4];
                gallery.Items[11].FindChildComponent<RectTransform>("bg").GetWorldCorners(bottom);
                gallery.FindChildComponent<RectTransform>("Panel/Content/Scroll/Viewport").GetWorldCorners(viewport);
                Require(bottom[0].y >= viewport[0].y - 1, "fourth row fits standard portrait viewport without clipping");
                SudokuDemoValidation.CaptureCanvas(gallery.gameObject, Path.Combine(output, "trophy-chinese.png"));
                SudokuThemeManager.SetTheme(1);
                Require(gallery.Items[2].FindChildComponent<TMP_Text>("m_getState").color == SudokuThemeManager.GetColor(SudokuThemeRole.Accent) &&
                    gallery.Items[9].FindChildComponent<UnityEngine.UI.Image>("bg/ActiveOutline").color == SudokuThemeManager.GetColor(SudokuThemeRole.Accent) &&
                    gallery.Items[9].FindChildComponent<UnityEngine.UI.Outline>("bg/ActiveOutline").effectColor == SudokuThemeManager.GetColor(SudokuThemeRole.Accent), "palette updates badges labels and active outline live");
                Require(gallery.Items[2].FindChildComponent<UnityEngine.UI.Image>("Trophy/icon").color == Color.white, "palette does not recolor trophy artwork");
                Require(gallery.FindChildComponent<TMP_Text>(Top + "Year/m_progress").text.Contains(ColorUtility.ToHtmlStringRGB(SudokuThemeManager.GetColor(SudokuThemeRole.Accent))), "rich text count follows palette");
                SudokuDemoValidation.CaptureCanvas(gallery.gameObject, Path.Combine(output, "trophy-mint.png"));
                GameModule.Localization.Language = Language.English;
                Require(gallery.FindChildComponent<TMP_Text>(Top + "Center/m_title").text == "Trophy Room" && gallery.Items[0].FindChildComponent<TMP_Text>("m_getState").text == "Not Obtained" &&
                    gallery.Items[9].FindChildComponent<TMP_Text>("m_getState").text.StartsWith("In Progress"), "live English localization for header and all item states");
                SudokuDemoValidation.CaptureCanvas(gallery.gameObject, Path.Combine(output, "trophy-english.png"));
                gallery.MoveYear(1); await gallery.WaitForContentAsync(token);
                Require(gallery.SelectedYear == 2027 && gallery.Items.Count == 12 && gallery.Items[0].gameObject.GetInstanceID() == firstItem && gallery.Items[0].Progress.State == SudokuTrophyState.NotStarted, "year switch reuses widgets and refreshes states");
                gallery.MoveYear(1); Require(gallery.SelectedYear == 2027, "cannot navigate beyond configured years");
                gallery.MoveYear(-1); gallery.MoveYear(1); gallery.MoveYear(-1);
                await gallery.WaitForContentAsync(token);
                Require(gallery.Items[2].FindChildComponent<UnityEngine.UI.Image>("Trophy/icon").sprite.name == "trophy_03_pisces", "rapid year switches reject stale icon completion");
                gallery.MoveYear(1); GameModule.UI.CloseUI<UITrophyGallery>();
                gallery = await GameModule.UI.ShowUIAsyncAwait<UITrophyGallery>(new UITrophyGallery.Context(game, 2026));
                await gallery.WaitForContentAsync(token);
                Require(gallery.IsContentReady && gallery.Items.Count == 12 && gallery.Items[0].gameObject.GetInstanceID() == firstItem, "close while loading and cached reopen complete without duplicate items");
                GameModule.UI.DestroyUI<UITrophyGallery>();
                gallery = await GameModule.UI.ShowUIAsyncAwait<UITrophyGallery>(new UITrophyGallery.Context(game, 2026));
                await gallery.WaitForContentAsync(token);
                Require(gallery.IsContentReady && gallery.Items.Count == 12 && gallery.Items[0].gameObject.GetInstanceID() != firstItem, "destroy and recreate restore prefab bindings and resource leases");
                GameModule.UI.CloseUI<UITrophyGallery>();
                File.WriteAllText(Path.Combine(output, "trophy-ui-validation.txt"), "PASS: daily entry/back context, actual TrophyGalleryItem prefab loading, three-column list, 12 unique reusable widgets, obtained/locked/current/future visuals, icons, real collection counts, Chinese/English, live palette and rich text colors, year boundaries, rapid year switching, async close/reopen and destroy/recreate.\n");
            }
            finally { GameModule.UI.CloseUI<UITrophyGallery>(); GameModule.Localization.Language = oldLanguage; SudokuThemeManager.SetTheme(oldTheme); }
        }
        private static void Check(TrophyGalleryItem item, SudokuTrophyState state, string label, string icon, bool locked, bool obtained, bool active)
        {
            Require(item.Progress.State == state && item.FindChildComponent<TMP_Text>("m_getState").text.Contains(label) && item.FindChildComponent<UnityEngine.UI.Image>("Trophy/icon").sprite.name == icon &&
                item.FindChild("Trophy/LockBg").gameObject.activeSelf == locked && item.FindChild("Trophy/GetRoot").gameObject.activeSelf == obtained && item.FindChild("bg/ActiveOutline").gameObject.activeSelf == active, "visual state " + state);
        }
        private static void AddDays(GameCacheData data, int year, int month, int days) { for (int day = 1; day <= days; day++) data.dailyChallenges.Add(new SudokuDailySaveData { date = SudokuDailyRules.DateKey(new DateTime(year, month, day)), completed = true }); }
        private static ConfTrophy NextYear(ConfTrophy source)
        {
            var buf = new ByteBuf(); buf.WriteInt(source.Id + 100); buf.WriteInt(source.Year + 1); buf.WriteInt(source.Month);
            buf.WriteString(source.IconObtained); buf.WriteString(source.IconNotObtained); buf.WriteString(source.NameKey); buf.WriteString(source.DescriptionKey);
            buf.WriteInt((int)source.UnlockType); buf.WriteInt(source.UnlockValue); buf.WriteInt(source.SortOrder); return new ConfTrophy(buf);
        }
        private static SudokuPack[] LoadPacks() { var packs = new SudokuPack[6]; for (int d = 0; d < 6; d++) packs[d] = SudokuPack.Read(File.ReadAllBytes("Assets/AssetRaw/Configs/Sudoku/sudoku_" + ((SudokuDifficulty)d).ToString().ToLowerInvariant() + ".bytes")); return packs; }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException("Trophy UI validation failed: " + label); }
        private sealed class MemoryStorage : IReadCacheFileSystem
        {
            public string Json;
            public UniTask<string> ReadCacheAsync() => UniTask.FromResult(Json);
            public UniTask WriteCacheAsync(string json) { Json = json; return UniTask.CompletedTask; }
            public void FlushOnQuit(string json) { Json = json; }
        }
    }
}
#endif

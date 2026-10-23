using System;
using GameLogic.Data;
using UnityEngine;

namespace GameLogic.Sudoku
{
    public enum SudokuThemeRole
    {
        Background, Surface, Text, MutedText, Accent, Line, Related, Matching, Selected, Error, KeyBackground, StrongLine, CompletionFlash
    }

    /// <summary>统一主题入口。调色盘调用 SetTheme，UIThemeColor 监听 Changed 刷新。</summary>
    public static class SudokuThemeManager
    {
        public const int ThemeCount = 6;

        // 配色集中在这里修改。每项为 Color32(R, G, B, A)，四个通道范围均为 0～255。
        // 每套主题独立配置，修改主色不会自动影响背景、高亮或其他角色的颜色。
        // UIThemeColor 勾选 Preserve Alpha 时，仍使用图形组件原来的透明度。
        private static readonly ThemeColors[] Themes =
        {
            // 0：经典
            new ThemeColors
            {
                Background    = new Color32(249, 252, 255, 255), // #F9FCFF 页面背景
                Surface       = new Color32(252, 253, 255, 255), // #FCFDFF 棋盘格和卡片底色
                Text          = new Color32(11, 32, 79, 255), // #0B204F 正文和题面数字
                MutedText     = new Color32(126, 145, 155, 255), // #7E919B 次要文字和笔记
                Accent        = new Color32(32, 134, 255, 255), // #2086FF 主题主色和输入数字
                Line          = new Color32(136, 155, 163, 255), // #889BA3 细网格线
                Related       = new Color32(235, 244, 255, 255), // #EBF4FF 同行、同列、同宫高亮
                Matching      = new Color32(213, 232, 255, 255), // #D5E8FF 相同数字高亮
                Selected      = new Color32(184, 216, 255, 255), // #B8D8FF 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(245, 250, 255, 255), // #F5FAFF 数字键底色
                StrongLine    = new Color32(72, 93, 122, 255), // #485D7A 粗宫格线
                CompletionFlash = new Color32(146, 198, 255, 255), // #92C6FF 完成行、列、宫时的闪光颜色
            },

            // 1：薄荷
            new ThemeColors
            {
                Background    = new Color32(242, 247, 245, 255), // #F2F7F5 页面背景
                Surface       = new Color32(252, 253, 253, 255), // #FCFDFD 棋盘格和卡片底色
                Text          = new Color32(12, 32, 70, 255), // #0C2046 正文和题面数字
                MutedText     = new Color32(128, 145, 137, 255), // #809189 次要文字和笔记
                Accent        = new Color32(51, 134, 108, 255), // #33866C 主题主色和输入数字
                Line          = new Color32(138, 155, 145, 255), // #8A9B91 细网格线
                Related       = new Color32(237, 244, 242, 255), // #EDF4F2 同行、同列、同宫高亮
                Matching      = new Color32(216, 232, 227, 255), // #D8E8E3 相同数字高亮
                Selected      = new Color32(190, 216, 208, 255), // #BED8D0 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(246, 250, 248, 255), // #F6FAF8 数字键底色
                StrongLine    = new Color32(73, 93, 110, 255), // #495D6E 粗宫格线
                CompletionFlash = new Color32(151, 205, 185, 255), // #97CDB9 完成行、列、宫时的闪光颜色
            },

            // 2：天空
            new ThemeColors
            {
                Background    = new Color32(243, 246, 250, 255), // #F3F6FA 页面背景
                Surface       = new Color32(252, 253, 254, 255), // #FCFDFE 棋盘格和卡片底色
                Text          = new Color32(14, 31, 75, 255), // #0E1F4B 正文和题面数字
                MutedText     = new Color32(131, 143, 146, 255), // #838F92 次要文字和笔记
                Accent        = new Color32(72, 124, 181, 255), // #487CB5 主题主色和输入数字
                Line          = new Color32(141, 154, 154, 255), // #8D9A9A 细网格线
                Related       = new Color32(239, 243, 248, 255), // #EFF3F8 同行、同列、同宫高亮
                Matching      = new Color32(220, 230, 241, 255), // #DCE6F1 相同数字高亮
                Selected      = new Color32(196, 213, 231, 255), // #C4D5E7 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(247, 249, 252, 255), // #F7F9FC 数字键底色
                StrongLine    = new Color32(75, 92, 116, 255), // #4B5C74 粗宫格线
                CompletionFlash = new Color32(156, 191, 226, 255), // #9CBFE2 完成行、列、宫时的闪光颜色
            },

            // 3：蜜桃
            new ThemeColors
            {
                Background    = new Color32(251, 246, 245, 255), // #FBF6F5 页面背景
                Surface       = new Color32(254, 253, 253, 255), // #FEFDFD 棋盘格和卡片底色
                Text          = new Color32(21, 30, 70, 255), // #151E46 正文和题面数字
                MutedText     = new Color32(145, 142, 136, 255), // #918E88 次要文字和笔记
                Accent        = new Color32(189, 112, 101, 255), // #BD7065 主题主色和输入数字
                Line          = new Color32(155, 152, 144, 255), // #9B9890 细网格线
                Related       = new Color32(249, 242, 241, 255), // #F9F2F1 同行、同列、同宫高亮
                Matching      = new Color32(242, 228, 226, 255), // #F2E4E2 相同数字高亮
                Selected      = new Color32(234, 209, 206, 255), // #EAD1CE 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(252, 249, 248, 255), // #FCF9F8 数字键底色
                StrongLine    = new Color32(84, 91, 109, 255), // #545B6D 粗宫格线
                CompletionFlash = new Color32(236, 176, 166, 255), // #ECB0A6 完成行、列、宫时的闪光颜色
            },

            // 4：浅紫
            new ThemeColors
            {
                Background    = new Color32(247, 246, 249, 255), // #F7F6F9 页面背景
                Surface       = new Color32(253, 253, 254, 255), // #FDFDFE 棋盘格和卡片底色
                Text          = new Color32(17, 30, 74, 255), // #111E4A 正文和题面数字
                MutedText     = new Color32(138, 142, 144, 255), // #8A8E90 次要文字和笔记
                Accent        = new Color32(133, 110, 167, 255), // #856EA7 主题主色和输入数字
                Line          = new Color32(148, 152, 152, 255), // #949898 细网格线
                Related       = new Color32(244, 242, 247, 255), // #F4F2F7 同行、同列、同宫高亮
                Matching      = new Color32(232, 227, 238, 255), // #E8E3EE 相同数字高亮
                Selected      = new Color32(216, 209, 227, 255), // #D8D1E3 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(250, 248, 251, 255), // #FAF8FB 数字键底色
                StrongLine    = new Color32(80, 91, 115, 255), // #505B73 粗宫格线
                CompletionFlash = new Color32(196, 175, 222, 255), // #C4AFDE 完成行、列、宫时的闪光颜色
            },

            // 5：杏色
            new ThemeColors
            {
                Background    = new Color32(250, 247, 243, 255), // #FAF7F3 页面背景
                Surface       = new Color32(254, 253, 252, 255), // #FEFDFC 棋盘格和卡片底色
                Text          = new Color32(20, 32, 68, 255), // #142044 正文和题面数字
                MutedText     = new Color32(144, 145, 132, 255), // #909184 次要文字和笔记
                Accent        = new Color32(181, 135, 69, 255), // #B58745 主题主色和输入数字
                Line          = new Color32(154, 155, 140, 255), // #9A9B8C 细网格线
                Related       = new Color32(248, 244, 238, 255), // #F8F4EE 同行、同列、同宫高亮
                Matching      = new Color32(241, 232, 220, 255), // #F1E8DC 相同数字高亮
                Selected      = new Color32(231, 217, 195, 255), // #E7D9C3 当前选中格
                Error         = new Color32(206, 77, 77, 255), // #CE4D4D 冲突提示
                KeyBackground = new Color32(252, 250, 247, 255), // #FCFAF7 数字键底色
                StrongLine    = new Color32(83, 93, 107, 255), // #535D6B 粗宫格线
                CompletionFlash = new Color32(229, 197, 148, 255), // #E5C594 完成行、列、宫时的闪光颜色
            }
        };

        private sealed class ThemeColors
        {
            public Color32 Background;
            public Color32 Surface;
            public Color32 Text;
            public Color32 MutedText;
            public Color32 Accent;
            public Color32 Line;
            public Color32 Related;
            public Color32 Matching;
            public Color32 Selected;
            public Color32 Error;
            public Color32 KeyBackground;
            public Color32 StrongLine;
            public Color32 CompletionFlash;
        }

        private static GameCacheManager _cache;
        private static Action _save;
        public static int CurrentThemeId { get; private set; }
        public static event Action Changed;

        public static void Initialize(GameCacheManager cache, Action save)
        {
            _cache = cache; _save = save;
            CurrentThemeId = Mathf.Clamp(cache.CacheData.gameSettingsData.themeId, 0, ThemeCount - 1);
            Changed?.Invoke();
        }

        public static Color GetAccent(int themeId) => Themes[Mathf.Clamp(themeId, 0, ThemeCount - 1)].Accent;

        public static bool SetTheme(int themeId)
        {
            if (themeId < 0 || themeId >= ThemeCount || CurrentThemeId == themeId) return false;
            CurrentThemeId = themeId;
            if (_cache != null) _cache.CacheData.gameSettingsData.themeId = themeId;
            _save?.Invoke();
            Changed?.Invoke();
            return true;
        }

        public static Color GetColor(SudokuThemeRole role)
        {
            var colors = Themes[CurrentThemeId];
            switch (role)
            {
                case SudokuThemeRole.Background: return colors.Background;
                case SudokuThemeRole.Surface: return colors.Surface;
                case SudokuThemeRole.Text: return colors.Text;
                case SudokuThemeRole.MutedText: return colors.MutedText;
                case SudokuThemeRole.Accent: return colors.Accent;
                case SudokuThemeRole.Line: return colors.Line;
                case SudokuThemeRole.Related: return colors.Related;
                case SudokuThemeRole.Matching: return colors.Matching;
                case SudokuThemeRole.Selected: return colors.Selected;
                case SudokuThemeRole.Error: return colors.Error;
                case SudokuThemeRole.KeyBackground: return colors.KeyBackground;
                case SudokuThemeRole.StrongLine: return colors.StrongLine;
                case SudokuThemeRole.CompletionFlash: return colors.CompletionFlash;
                default: return Color.white;
            }
        }

        public static void Release()
        {
            _cache = null; _save = null; CurrentThemeId = 0;
            // 订阅由组件 OnDisable/OnDestroy 释放，不跨存活 UI 强制清空委托。
        }
    }
}

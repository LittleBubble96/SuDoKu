# 数独 UI 散图使用说明

散图目录：`Assets/AssetRaw/UIRaw/Atlas/Sudoku`。可换色素材使用纯白 RGB + 透明 Alpha；金币及奖杯彩色/灰色素材保留固有色。清单记录在 `Sprites.json`。Sprite 使用 PPU 100、Full Rect，无 Mipmap、不缩放 NPOT、不压缩。新增散图、尺寸和复用方式详见 [补切说明](ExpandedSprites/README.md)；奖杯底部独立柔影见 [柔影说明](TrophyShadow/README.md)。

## 参考图

- `Home_Classic_1080x1920.png`：经典首页。
- `Game_Classic_1080x1920.png`：经典对局，棋盘为 990×990，每格 110×110。
- `Home_Peach_1080x1920.png`、`Game_Peach_1080x1920.png`：同布局换色示例。
- `Sprites_ContactSheet.png`：深色底散图预览，仅供检查白色图形。
- `Hint_States_1080x1920.png`：已审核的数量、金币、广告和广告加载状态。
- `Hint_Sprites_ContactSheet.png`：新增提示按钮散图预览。
- `Source/Approved_Overview.png`：用户确认过的原始总览。

独立页面的尺寸均为宽 1080 × 高 1920。图标按参考图比例紧凑裁切，保留约 1 像素抗锯齿余量。九宫底板保留圆角和描边，其余区域压缩为小图，使用下表推荐绘制尺寸拉伸。

## 使用方法

- 带 `_9s` 的 11 张图已经写好 Sprite Border；UGUI Image 的 Type 选 `Sliced`。圆角和描边不需要放大整张原图。
- 普通图标选 `Simple`、勾选 Preserve Aspect，按 PNG 原尺寸放置或等比缩放。
- 白图通过 Image.color 控制颜色和透明度；底板、边框、文字分别着色。
- 胶囊底板与描边可按需叠加两层 Image；圈与勾、配色圆点也可分别设置颜色。
- `bg_white_quad_9s` 可用于格子和细线；网格线用 RectTransform 设置宽高和位置。
- 文字、数字、金币金额、日期和状态标签未烘焙到散图，继续使用项目的 `FeiHuaSongTi-2 SDF`。

建议色值：经典主色 `#2086FF`，浅色填充 `#EEF6FF`，选中格 `#B3DEFD`，正文 `#0A1944`；蜜桃主色 `#FF8470`，浅色填充 `#FFF4F1`。卡片/描边还可通过 Alpha 控制强度。

## 文件清单

九宫边距顺序为左 / 下 / 右 / 上，单位像素。推荐绘制尺寸基于 1080×1920 画布；可以随布局调整。

| 文件 | 用途 | PNG 尺寸 | 九宫边距 | 推荐绘制尺寸 |
|---|---|---:|---:|---:|
| icon_back.png | 返回 | 41×75 | — | 41×75 |
| icon_pause.png | 暂停（含圆圈） | 123×124 | — | 123×124 |
| icon_palette.png | 调色盘 | 86×83 | — | 86×83 |
| icon_settings.png | 设置 | 86×87 | — | 86×87 |
| icon_clock.png | 计时 | 68×68 | — | 68×68 |
| icon_undo.png | 撤销 | 85×94 | — | 85×94 |
| icon_erase.png | 擦除 | 87×85 | — | 87×85 |
| icon_notes.png | 笔记 | 85×83 | — | 85×83 |
| icon_hint.png | 提示 | 110×114 | — | 110×114 |
| icon_sign_in.png | 签到 | 78×86 | — | 78×86 |
| icon_daily_challenge.png | 每日挑战 | 170×178 | — | 170×178 |
| icon_home_active.png | 首页选中 | 71×73 | — | 71×73 |
| icon_other_games.png | 其他游戏 | 83×64 | — | 83×64 |
| icon_statistics.png | 统计 | 67×73 | — | 67×73 |
| icon_chevron_right.png | 向右箭头 | 36×57 | — | 36×57 |
| icon_daily_tasks.png | 每日任务 | 72×88 | — | 72×88 |
| icon_close.png | 关闭 | 48×48 | — | 48×48 |
| icon_check.png | 勾选 | 48×36 | — | 48×36 |
| icon_plus.png | 加号（换色用） | 42×42 | — | 42×42 |
| icon_home_outline.png | 首页未选中 | 72×69 | — | 72×69 |
| icon_lock.png | 未解锁 | 38×48 | — | 38×48 |
| coin_gold.png | 金币（保留金色） | 75×76 | — | 75×76 |
| coin_plus_gold.png | 金币加号（保留金色） | 84×86 | — | 84×86 |
| icon_theme_selected.png | 配色选中圈 | 112×112 | — | 112×112 |
| bg_button_fill_9s.png | 主按钮底板 | 96×96 | 34/34/34/34 | 936×268 |
| bg_button_outline_9s.png | 描边按钮 | 96×96 | 34/34/34/34 | 936×180 |
| bg_card_fill_9s.png | 卡片底板 | 88×88 | 32/32/32/32 | 936×780 |
| bg_card_outline_9s.png | 卡片边框 | 88×88 | 32/32/32/32 | 936×780 |
| bg_number_key_9s.png | 数字键底板 | 64×64 | 22/22/22/22 | 106×152 |
| bg_capsule_fill_9s.png | 胶囊按钮/状态标签 | 128×128 | 62/62/62/62 | 624×156 |
| bg_capsule_outline_9s.png | 金币栏/胶囊边框 | 128×128 | 62/62/62/62 | 360×110 |
| bg_sheet_top_9s.png | 底部弹出面板（仅顶部圆角） | 128×128 | 46/2/46/46 | 1080×1200 |
| bg_drag_handle_9s.png | 弹层拖动提示条 | 20×20 | 9/9/9/9 | 120×12 |
| bg_circle_fill.png | 主题色圆点/圆形徽标 | 128×128 | — | 128×128 |
| bg_circle_outline.png | 圆形描边 | 128×128 | — | 128×128 |
| bg_white_quad_9s.png | 格子底色/高亮/网格线/分隔线 | 4×4 | 1/1/1/1 | 110×110 |

| icon_hint_video.png | 提示广告标识（可换色） | 32×26 | — | 32×26 |
| icon_hint_loading.png | 广告加载弧线（可换色，中心轴旋转） | 36×36 | — | 36×36 |
| icon_hint_coin.png | 提示金币标识（保留金色） | 28×28 | — | 28×28 |
| bg_hint_badge_9s.png | 提示角标底板（可换色） | 44×44 | 21/21/21/21 | 高44；宽54/118/132/144 |

提示灯泡复用 `icon_hint`。数量、价格、广告和加载中文字用 TMP，角标底板使用 Sliced；加载时只旋转弧线，文字与底板保持不动。

## 首页 v2（已确认）

- `Home_Classic_v2_1080x1920.png`：选定的较小数独 icon 和标题版本，保留新游戏下方留白；底栏为首页、每日挑战、统计、设置。
- `Source/Home_Classic_v2_Approved.png`：审核原图；`Home_v2_SpritePreview.png`：新增白图与蓝色着色预览。
- 新增 `icon_sudoku_logo.png`，166×164，白色 RGB + Alpha，普通 Sprite，不九宫拉伸，建议原尺寸使用。
- 数独标题使用现有 TMP 字体；其他图标和九宫底板复用，本次不拼 UI。

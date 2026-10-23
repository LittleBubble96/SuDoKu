# 按效果图补切的散图

导出目录：`Assets/AssetRaw/UIRaw/Atlas/Sudoku`。

本次新增 **37张PNG**：13张白色图标/圆环，12款奖杯的彩色和灰色状态各一套。新增PNG合计 **2,776,097字节（约2.65 MiB）**。原有41张PNG与对应meta保持原文件；更新后总清单为78张。

## 需要新切的白图

这些文件RGB统一为白色，透明Alpha，按1080×1920效果图的显示比例紧凑裁切。普通图标使用Simple，开启Preserve Aspect，Image.color控制主题色；不做九宫拉伸。

| 文件 | 用途 | PNG尺寸 | 建议显示尺寸 |
| --- | --- | --- | --- |
| icon_daily_challenge_grid_star.png | 每日挑战新图标：九宫格＋星标；首页卡片、底部栏、挑战卡共用 | 136×149 | 136×149 |
| icon_trophy.png | 奖杯馆入口、本月奖杯标题小图标 | 68×68 | 68×68 |
| icon_sound.png | 设置：音效 | 70×62 | 70×62 |
| icon_vibration.png | 设置：震动 | 78×67 | 78×67 |
| icon_highlight_same.png | 设置：相同数字高亮 | 66×66 | 66×66 |
| icon_highlight_notes.png | 设置：相同笔记高亮 | 71×67 | 71×67 |
| icon_related_area.png | 设置：关联区域高亮 | 73×73 | 73×73 |
| icon_completed_digit.png | 设置：弱化已完成数字（图中的1是图标图形） | 67×67 | 67×67 |
| icon_remaining_count.png | 设置：显示剩余数量 | 67×68 | 67×68 |
| icon_language.png | 设置：语言 | 73×74 | 73×74 |
| icon_help.png | 设置：游戏说明 | 62×73 | 62×73 |
| icon_ad_video.png | 大按钮的广告播放图标；签到双倍、免费奖励双倍共用 | 85×71 | 85×71 |
| bg_progress_ring.png | 日期格圆形进度：背景轨道和进度共用一张完整圆环 | 128×128 | 96×96 |

### 圆形进度的搭法

- 用两张相同的`bg_progress_ring`：底层为浅色完整轨道，上层Image Type设为Filled、Fill Method设Radial360、Origin为Top、Clockwise开启，用Fill Amount设置0～1进度。
- 圆环不九宫、不做非等比拉伸。选中日期仍用原蓝色圆底，环改为白色。
- 如需效果图中的圆形端点，使用已有`bg_circle_fill`作为端点小圆，直径等于环厚；位置随进度角度变化，不需要另切各个百分比的图片。
- 已完成状态复用`bg_circle_outline`、`bg_circle_fill`和`icon_check`；未开始只放日期文字。日期、百分比等文字不在PNG内。

## 需要新切的奖杯

效果图里有多款只有未获得时的灰色预览，已按相同造型补齐彩色透明源图，并分别输出正常、灰色两态。保留把手与底座完整轮廓；同一奖杯两态的尺寸、Alpha完全一致，切换不跳位。

奖杯保留固有蓝金/银灰颜色，Image.color用白色，不挂主题变色组件。锁、对号、卡片底板和月份文字均单独复用，未烘焙到奖杯中。月度奖杯大图与奖杯馆卡片共用同一PNG，卡片中等比缩小即可。

| 主题 | 正常状态文件 | 灰色状态文件 | PNG尺寸 |
| --- | --- | --- | --- |
| 1月·摩羯 | trophy_01_capricorn.png | trophy_01_capricorn_gray.png | 320×263 |
| 2月·水瓶 | trophy_02_aquarius.png | trophy_02_aquarius_gray.png | 320×256 |
| 3月·双鱼 | trophy_03_pisces.png | trophy_03_pisces_gray.png | 320×252 |
| 4月·白羊 | trophy_04_aries.png | trophy_04_aries_gray.png | 320×258 |
| 5月·金牛 | trophy_05_taurus.png | trophy_05_taurus_gray.png | 313×300 |
| 6月·双子 | trophy_06_gemini.png | trophy_06_gemini_gray.png | 320×274 |
| 7月·巨蟹 | trophy_07_cancer.png | trophy_07_cancer_gray.png | 320×253 |
| 8月·狮子 | trophy_08_leo.png | trophy_08_leo_gray.png | 320×299 |
| 9月·处女 | trophy_09_virgo.png | trophy_09_virgo_gray.png | 320×288 |
| 10月·天秤 | trophy_10_libra.png | trophy_10_libra_gray.png | 320×267 |
| 11月·天蝎 | trophy_11_scorpio.png | trophy_11_scorpio_gray.png | 320×282 |
| 12月·射手 | trophy_12_sagittarius.png | trophy_12_sagittarius_gray.png | 318×300 |

月份与星座是效果图中的主题编排。奖杯彩色源图使用内置 image_gen 补全，[生成提示词](TrophySourcePrompt.md)、源图和预览保存在本目录，不放进Assets。

## 直接复用的资源

补切了可供所有奖杯共用的底部柔影 `bg_trophy_shadow.png`（320×37，纯白透明底）。将它作为独立 Image 放在奖杯下层，Simple 类型，颜色和透明度分别调节；不做九宫拉伸。详见 [柔影补切说明](../TrophyShadow/README.md)。

| UI部位 | 复用文件/做法 |
| --- | --- |
| 每日签到入口 | `icon_sign_in`（日历对号） |
| 每日任务入口 | `icon_daily_tasks`（夹板清单） |
| 每日挑战入口 | 使用本次新增`icon_daily_challenge_grid_star`；原`icon_daily_challenge`是旧版日历造型 |
| 首页数独品牌标识 | `icon_sudoku_logo`，与每日挑战功能图标区分 |
| 金币与加号 | `coin_gold`、`coin_plus_gold`；其他加号用`icon_plus` |
| 提示奖励、数量/金币/广告/加载状态 | `icon_hint`、`icon_hint_coin`、`icon_hint_video`、`icon_hint_loading`、`bg_hint_badge_9s` |
| 返回、左右翻页、关闭 | `icon_back`、`icon_chevron_right`（左箭头翻转复用）、`icon_close` |
| 对号与锁 | `icon_check`、`icon_lock`；圆形徽章用`bg_circle_fill` |
| 按钮、选中边框 | `bg_button_fill_9s`、`bg_button_outline_9s` |
| 卡片、居中弹窗 | `bg_card_fill_9s`、`bg_card_outline_9s` |
| 难度选择底部弹层、拖动条 | `bg_sheet_top_9s`、`bg_drag_handle_9s` |
| 日期格、签到小奖励格 | `bg_number_key_9s`或`bg_card_fill_9s`；颜色由Image设置 |
| 横向任务/奖杯进度条、标签、开关轨道 | `bg_capsule_fill_9s`；开关滑块用`bg_circle_fill`，用位置和颜色区分开/关 |
| 背景遮罩、分隔线、网格线 | `bg_white_quad_9s`，用RectTransform和颜色/Alpha控制 |
| 文字、日期、奖励数量、进度、星座名称 | TMP使用现有`FeiHuaSongTi-2 SDF`，不切字图 |

原有11张九宫底板继续用Sliced。图标、圆环、奖杯都用Simple或Filled，不做九宫。胶囊缩到较小高度时，可调整Image的Pixels Per Unit Multiplier，使半圆端帽保持合适比例。

## 界面组合说明

- 首页：签到、每日任务、每日挑战分别使用日历对号、夹板清单、九宫星标，三类图标不混用。
- 难度选择：已有底部面板＋卡片底板＋锁/对号＋文字。完成进度小点复用圆底，无需为每档单独切整张卡片。
- 设置：新增设置图标，底板、开关、色点和箭头复用。
- 签到：一套弹窗底板与7个奖励格，图标和数量分别放置，按钮使用普通领取/广告双倍两种文案。
- 每日礼包：免费奖励区有免费领取和广告双倍；双倍只作用于免费奖励。下面两项任务分别普通领取；“玩一局游戏”用数独品牌标识，“完成今日挑战”用新增九宫星标图标。
- 每日挑战：固定6行×7列共42个位置。没有说明图例与“每月一款星座主题”标签，日期状态通过图标本身区分。
- 奖杯馆：12组奖杯两态＋已有锁/对号/边框；月份与状态文字用TMP。

## 检查与预览

- `New_Icons_ContactSheet.png`：13张白图的深色底预览。
- `Trophies_ContactSheet.png`：12款彩色/灰色奖杯预览。
- `NewSprites.json`：新图名称、像素尺寸、来源裁切范围和推荐显示尺寸。
- `PixelValidation.txt`：透明底、白色RGB、完整边界和奖杯两态Alpha检查。
- `UnityImportValidation.txt`：Unity实际Sprite导入检查。
- `ExistingAssets.before.json`与`ExistingAssetsValidation.txt`：原有PNG与meta的哈希核对。

本次交付为散图与导入设置，UI层级、预制体布局与业务逻辑由后续界面接入处理。

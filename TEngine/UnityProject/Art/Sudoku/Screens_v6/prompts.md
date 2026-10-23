# 界面效果图 · 第六版

方式：内置 image_gen。交付尺寸：1080×1920。

## 本版变更

- 每日挑战：去除月历底部所有状态图例；日期格本身的对号、进度环和选中样式保留。
- 每日挑战：去除奖杯卡右上“每月一款星座主题”标签和胶囊背景。
- 每日签到：日历加对号，沿用已有 icon_sign_in。
- 每日任务：夹板清单，沿用已有 icon_daily_tasks；修正旧效果图中误用的日历。
- 每日挑战：独立的九宫格加右上小星标，用于首页卡片、底部导航及所选挑战卡。
- 中央数独品牌图标仍保留数字1、5、9，与每日挑战功能图标区分。

## 首页提示词

```text
Use case: precise-object-edit / ui-mockup.
Edit target image1: the approved BLUE/WHITE Sudoku HOME screen. Supporting image2: our existing SPRITE CONTACT SHEET, only for original icon shapes. Output ONE portrait UI screenshot, 1080 x 1920, no phone frame/status bar/watermark/external annotations.
Preserve the home layout, all typography, white background, coin balance and plus, centered Sudoku logo+“数独”, other-games row, daily challenge card, continue button, new-game button, palette icon, bottom tab labels and every existing position/proportion.

The ONLY changes are to distinguish THREE FEATURES using clearly different silhouettes, in the same thin rounded mono-blue line style:
1. SIGN-IN (top “签到”): KEEP its existing two-ring calendar with one checkmark. Match “icon_sign_in” in the sprite contact sheet. This is the ONLY feature allowed to use a calendar-with-check icon.
2. DAILY TASKS (top “每日任务”): REPLACE the mistakenly repeated calendar/check icon with our original CLIPBOARD CHECKLIST icon from the sprite sheet entry “icon_daily_tasks”. One central top clip, a rounded rectangular board, three tiny round bullets and three short horizontal text lines. NO calendar binder rings, NO date grid, NO single big check. It must visibly read as a task list, not sign-in.
3. DAILY CHALLENGE: use a distinct small SUDOKU GRID + STAR emblem. A clean rounded square split into a 3-by-3 grid, no digits or letters, with a modest small five-point outline star attached at its upper-right corner. No binder rings, calendar header or checkmark. Keep stroke weight and optical size consistent with other icons.
Apply this EXACT SAME grid+star challenge emblem in BOTH places on the home: the large left icon of the “每日挑战” card, and the muted inactive bottom navigation icon above “每日挑战”.
Do NOT change the large central BRAND Sudoku logo with digits1,5,9; it is a separate logo and must remain unchanged.
All icon text labels unchanged. Retain single-blue accent for icons, muted gray-blue for inactive nav; gold only for coin. Do not redraw any unrelated UI or add new labels, legends or decoration.
```

## 每日挑战提示词

```text
Use case: precise-object-edit / ui-mockup.
Edit target image1: the existing SIX-ROW blue/white DAILY CHALLENGE page. Supporting image2: the corrected home screen; it is the exact reference for the newly unified DAILY CHALLENGE GRID+STAR icon. Use the home ONLY as an icon/style reference, not as the page layout. Output ONE portrait 1080 x 1920 UI screenshot with no phone frame, status bar, watermark, or external annotations.

Make ONLY the following requested revisions:
A. DELETE the ENTIRE bottom legend from INSIDE the calendar card: remove all four sample symbols/numerals AND their labels “未开始”, “进行中”, “已完成”, “已选中”. Delete the legend row completely; do not leave an empty placeholder strip or move it elsewhere. Preserve the actual date-cell status markers themselves. Use the freed space for slightly roomier six calendar rows and tidy bottom padding.
B. DELETE the small top-right badge “每月一款星座主题” from the trophy achievement card, including its pill background. Leave the rest of the trophy card unchanged: “本月星座奖杯”, “狮子之杯”, the blue/gold LEO trophy and small lock, “完成本月全部挑战即可获得”, monthly progress12/31.
C. DAILY CHALLENGE ICON CONSISTENCY: replace the old calendar/check navigation icon above active bottom tab “每日挑战” with the SAME blue outline 3x3 Sudoku-grid-with-small-upper-right-star icon visible on supporting corrected home. Replace the numeric Sudoku logo at the left of the selected-day card “今日 · 8月23日” with that SAME grid+star feature icon. Keep the icon size, stroke weight and blue tint consistent. Do NOT use the sign-in calendar/check or tasks clipboard here. The small trophy icon and “奖杯馆” entry stay trophy icons.

Everything else stays the same, in particular:
- heading “每日挑战”, back chevron and “奖杯馆” entry.
- month heading “2026年8月”, weekday headings Monday-first “一 二 三 四 五 六 日”.
- EXACTLY SIX week rows, SEVEN columns, 42 reserved tiles:
row1 [blank blank blank blank blank 1 2]
row2 [3 4 5 6 7 8 9]
row3 [10 11 12 13 14 15 16]
row4 [17 18 19 20 21 22 23]
row5 [24 25 26 27 28 29 30]
row6 [31 blank blank blank blank blank blank].
- Completed dates EXACTLY3,4,5,7,8,9,11,12,15,17,20,22 keep their thin blue circles and small blue badges with white checks.
- Date13 and16 keep their PARTIAL blue circular progress rings and dark numerals; no checks.
- Selected date23 remains a SOLID BLUE disc with white23 and WHITE half-progress ring inside it.
- Other dates show plain numerals only, future24-31 muted, outside-month blank tiles have no text or icons.
- selected-day card keeps “今日 · 8月23日”, “中级”, “进行中 · 50%” and primary button “继续今日挑战”.
- bottom nav labels “首页 / 每日挑战 / 统计 / 设置”.
Respect the bright white/pale-blue backgrounds, clean navy Chinese type, fine pale-blue borders, existing spacing balance. No added explanations, feature legends, extra badges, or unrelated layout redesign.
```

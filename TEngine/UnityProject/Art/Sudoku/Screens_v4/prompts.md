# 每日挑战日期格 · 状态版

生成方式：内置 image_gen。目标尺寸：1080×1920。示例进度为美术展示数据。

| 游戏进度 | 未选中 | 已选中 |
| --- | --- | --- |
| 未开始 | 原有日期数字，无对号和进度环 | 原有蓝底白字，无状态图标 |
| 进行中 | 日期外围显示蓝色圆形进度条，无对号 | 保留蓝底白字，叠加白色圆形进度条 |
| 已完成 | 保留原有对号样式 | 保留蓝底白字，并保留对号 |

选中状态与进度状态独立。选中进行中的日期时，底部按钮展示“继续今日挑战”。本次仅为效果图，未接入运行时逻辑。

## 生成提示词

```text
Use case: precise-object-edit / ui-mockup.
Edit target: the supplied approved daily Sudoku challenge page. Keep its EXACT portrait composition and blue/white design system, top navigation, Libra trophy achievement card, 12/31 monthly progress, month header, all calendar positions, bottom challenge card and bottom navigation. Output ONE finished portrait UI screen, 1080 x 1920, no phone frame, no status bar, no watermark, no external annotations.

Primary change: make each CALENDAR DATE BUTTON support three independent progress states, with SELECTION as an additional overlay.
1. NOT STARTED: the exact normal pale-blue date tile with ONLY its dark date numeral. No circular ring, check, progress icon or extra badge.
2. STARTED / IN PROGRESS: date numeral encircled by a clearly PARTIAL blue circular progress ARC on a much paler blue circular track. Start the arc at 12 o'clock and fill clockwise. NO check mark on these dates. Keep ring thickness restrained and numeral clearly readable.
3. COMPLETED: preserve the reference's existing completed treatment EXACTLY: thin blue outline circle around date and a small blue badge with white CHECK at lower right.
SELECTION: preserve the original SOLID BLUE circle and WHITE date numeral, not an outline-only selection. Selection does NOT erase the underlying progress status: when a started date is selected, show its partial circular progress arc in WHITE with a pale translucent white remaining track, inset from the edge of the solid blue selected circle. A selected completed date would retain a small check badge; a selected not-started date would be just solid blue and white number with no status icon.

Apply these exact illustrative dates:
- Keep completed dates EXACTLY 3,4,5,7,8,9,11,12,15,17,20,22. Their old circles and check badges are unchanged. The total stays 12/31.
- Newly show date13 STARTED with 35% blue partial ring, date16 STARTED with 65% blue partial ring, date21 STARTED with 80% blue partial ring. None has a check.
- Date23 is the SELECTED date and STARTED at 50%: keep its original solid blue circle and white 23, add a thin half-completed WHITE progress ring inside that blue disc. It must be visibly half a ring, not a full white border or check.
- Other noncompleted dates stay NOT STARTED, only plain numerals. Future dates 24-31 stay muted exactly as before, without rings or check marks.

The month is still “2026年10月”, columns Monday-first “一 二 三 四 五 六 日”, rows EXACTLY:
[blank blank blank 1 2 3 4]
[5 6 7 8 9 10 11]
[12 13 14 15 16 17 18]
[19 20 21 22 23 24 25]
[26 27 28 29 30 31 blank].
Do not add, repeat, remove or reorder dates.

In the small legend under calendar, show concise samples for “未开始” (plain numeral), “进行中” (partial ring), “已完成” (check badge), and “已选中” (solid blue dot). Keep legend quiet and readable, fit inside original calendar card.
Because the selected date23 is now started, change ONLY the bottom card's subtitle to “进行中 · 50%” and its button to “继续今日挑战”. Retain heading “今日 · 10月23日”, the 中级 pill, Sudoku icon, button proportions, and all bottom navigation items.
All other content and artwork stays unchanged, including the Libra trophy. Clean exact Chinese text, same bright background and line weight, no new decoration.
```

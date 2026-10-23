# 每日挑战 · 固定六行月历

生成方式：内置 image_gen。交付尺寸：1080×1920。效果图演示情境：2026年8月23日。

- 月历固定预留6行×7列，共42个位置；切月不改变整体高度。
- 一周从周一开始。实际日期按月份排列，不在本月的位置留空且不可点击。
- 月份实际可能占4至6行，并非每个月都占满6行。
- 示例：2026年8月1日为周六，31日为周一，位于第6行第1列。
- 首日偏移：((DayOfWeek + 6) % 7)；某日格子索引：首日偏移 + 日期 - 1。
- 保留未开始、进行中、已完成三种状态，并独立叠加选中效果。

## 生成提示词

```text
Use case: precise-object-edit / UI mockup.
Edit target image 1: our approved blue-and-white daily Sudoku challenge screen. Supporting image 2: our zodiac trophy gallery, ONLY a reference for the LEO trophy design (8月 · 狮子). Preserve the daily challenge screen's style and page composition, not the gallery layout.
Output ONE polished portrait screen, 1080 x 1920, front-on, no device frame/status bar/watermark/external annotations.

REQUIRED FIX: calendar must have EXACTLY SIX equal-height WEEK ROWS and SEVEN Monday-first columns, i.e. 42 reserved date slots. The current screen only had five rows. Fit six rows by reducing vertical cell padding and spacing within the existing calendar card, while keeping numerals, circular rings and check badges clearly legible and truly circular. Keep the overall page within the same portrait bounds, keep the bottom action and navigation fully visible.

Use AUGUST 2026 as the illustrative month, since August 1 is SATURDAY and August 31 falls in WEEK ROW SIX. Month heading EXACTLY “2026年8月”. Weekday headings EXACTLY “一 二 三 四 五 六 日”.
The 6x7 date slots MUST be EXACTLY:
ROW 1: [blank, blank, blank, blank, blank, 1, 2]
ROW 2: [3, 4, 5, 6, 7, 8, 9]
ROW 3: [10, 11, 12, 13, 14, 15, 16]
ROW 4: [17, 18, 19, 20, 21, 22, 23]
ROW 5: [24, 25, 26, 27, 28, 29, 30]
ROW 6: [31, blank, blank, blank, blank, blank, blank]
Show each blank slot as the same subtle pale tile with NO date text, NO 0, NO other-month numeral, NO icon. Never merge the sixth row with the fifth. 31 appears ONCE and ONLY in the SIXTH row MONDAY column. Exactly the 31 different numerals 1 through 31, no duplicates or omissions.

Retain the established per-date STATE DESIGN, now in their correct August positions:
COMPLETED exactly dates 3,4,5,7,8,9,11,12,15,17,20,22: existing thin blue circle with small lower-right blue badge and white CHECK. Total stays 12 / 31.
IN PROGRESS date13 has one continuous blue partial progress arc about35%; date16 has one continuous arc about65%. Pale blue track, dark numeral. No check.
SELECTED date23 is Sunday of ROW4: original SOLID BLUE circle with WHITE 23 and a thin WHITE half-filled circular progress arc inset inside the disc. No check.
All other dates NOT STARTED: plain numeral only, no circle/ring/check. Future dates24-31 muted exactly like the original. Keep circular artwork small enough to fit every row without overlap.
Keep the compact four-item state legend “未开始 / 进行中 / 已完成 / 已选中” below the SIXTH calendar row, inside the same card.

MONTH-CONSISTENT TROPHY: change the top monthly collectible from Libra to the BLUE/GOLD LEO cup from image2, with a lion-mane medallion and the same elegant stem and pedestal; name EXACTLY “狮子之杯”. It is an unearned monthly reward preview with the existing small lock. Retain all achievement card text and progress “12 / 31”. No Libra scales on this August cup.
Bottom day's card heading EXACTLY “今日 · 8月23日”, level “中级”, subtitle “进行中 · 50%”, primary action “继续今日挑战”.
Preserve top heading “每日挑战”, back chevron, “奖杯馆” entry, and bottom navigation “首页 / 每日挑战 / 统计 / 设置”. Bright white, primary-blue accent, pale-blue rounded cards, navy typography, exact same icon family. All Chinese crisp and correct. Change only what's necessary for the SIX-ROW calendar and month-consistent labels/artwork.
```

# 每日挑战布局调整

方式：内置 image_gen 编辑，保留蓝白风格、现有图标、六行日历与日期状态。

原图：用户提供的 `codex-clipboard-e04df1e5-1546-43f5-bbc7-a4c0a9f6b610.png`。

初稿：`03_DailyChallenge_Flexible.png`。第二版：`03_DailyChallenge_Flexible_v2.png`。

改动：奖杯区取消大卡片边框并收紧排布；底部挑战区取消外层卡片和大图标，拆成日期/难度/进度信息行与独立继续按钮。本次只生成效果图，没有修改项目 UI 或切图。

## 生成提示词

Use case: ui-mockup / precise layout edit.
Edit target: the supplied portrait Daily Challenge Sudoku screen. Produce ONE finished flat 1080 x 1920 portrait app UI mockup, edge to edge, no phone device frame, no presentation sheet. Preserve the existing light blue / white Sudoku art direction and the exact icon family.

User-approved change: make the top monthly trophy region and bottom continue-challenge region much lighter and less dependent on a tall fixed card. Change those two regions' composition; retain the calendar and navigation.

TOP: Keep the existing header with back arrow, centered “每日挑战”, and upper-right outlined trophy icon + “奖杯馆”. Under it, replace the large outlined trophy card with an airy compact borderless horizontal information section. Left: small muted label “本月奖杯”, medium bold navy heading “狮子之杯”, smaller explanatory line “完成本月全部挑战即可获得”, and a thin blue monthly progress bar with “12 / 31” alongside. Right: the SAME blue and gold lion zodiac cup, proportionally scaled to a tasteful smaller thumbnail, not stretched. Keep its small locked badge. Use a very subtle pale blue ambient wash if needed, but NO enclosing rounded-rectangle outline around this whole section. The cup and text are independent, leaving generous clear breathing room. This section should occupy approximately y=175 to 435, substantially less height than the original.

CENTER: The existing calendar remains the main visual anchor, in ONE soft rounded white card with a faint blue hairline. Its width, 7-column organization, circular state indicators and typography should stay close to the source. Translate it upward into the space made available by the compact trophy section; do not make cells abnormally tall. Approximate bounds x=40..1040 and y=475..1370. Month heading exactly “2026年8月”, with existing left and right chevrons; weekdays exactly “一 二 三 四 五 六 日”. Exactly SIX rows of SEVEN date slots, Monday first:
row1: blank blank blank blank blank 1 2
row2: 3 4 5 6 7 8 9
row3: 10 11 12 13 14 15 16
row4: 17 18 19 20 21 22 23
row5: 24 25 26 27 28 29 30
row6: 31 blank blank blank blank blank blank.
Do not omit row6, duplicate dates or add extra dates. Preserve date states from reference: dates 3,4,5,7,8,9,11,12,15,17,20,22 have outlined blue circles and a small check badge. Dates 13 and 16 have partial circular blue progress rings. Date 23 is selected, a solid blue disk with white number and white partial progress ring. Unstarted dates have just their numerals, without additional badges. Future dates 24-31 use muted text. Retain the quiet pale blue tile backgrounds. NO state legend below the calendar.

BOTTOM: Remove the entire large lower challenge card, remove its large calendar/star illustration, remove its mini rounded difficulty pill. Replace it with a compact freely placed metadata row on the page background: on the left “8月23日 · 中级” in navy medium weight, on the right “进行中 50%” in muted blue gray, optionally a tiny blue status dot. Below that, a separate full-width blue rounded button with the exact white label “继续今日挑战”. Its normal tap-height is around 105 px and it is NOT embedded in an outer card. Keep balanced air between calendar, information row, CTA and bottom navigation, rather than filling empty space with stretched components.

BOTTOM NAV: Preserve four existing icons, order, style and labels exactly “首页 | 每日挑战 | 统计 | 设置”, without literal separator bars; second tab active bright blue and others muted navy gray. Preserve the thin separator above this bar. Do not add phone status bar, extra top icons, new rewards, badges, legends, paragraphs, watermarks, numbered levels, or any technical annotations. All text must be accurate Simplified Chinese. The final should feel like the same polished, bright minimalist game UI, with only those two information regions becoming compact, open and adaptable.

## 第二版生成提示词

Precise UI edit. Output exactly 1080 pixels wide and 1920 pixels tall, matching Image 1's canvas dimensions.
Image 1 is the 1080x1920 original canvas and the reference for the existing Sudoku icon/art family ONLY.
Image 2 is the FINISHED new layout to preserve. Reproduce Image 2's new composition, text, proportions and spacing faithfully at the 1080x1920 canvas size. Do not revert to Image 1's original large cards.

The final design must follow Image 2: header 每日挑战 and 奖杯馆; compact BORDERLESS monthly award section, small label 本月奖杯, heading 狮子之杯, explanation 完成本月全部挑战即可获得, progress 12 / 31 with thin blue bar, blue/gold lion cup at right; a single central rounded card with the exact six-row August 2026 calendar and date states from Image 2; underneath, a free-standing row 8月23日 · 中级 at left and 进行中 50% at right with tiny blue dot, then an independent blue button 继续今日挑战. NO bottom challenge card, NO large lower calendar illustration. Preserve light blue-white background, navy text, blue active accents, bottom tabs 首页 每日挑战 统计 设置, second selected. Every date and text in Image 2 should remain unchanged, including 31 alone in sixth row, completed check marks, partial progress on 13 and 16, and selected blue 23 with white partial progress. Maintain all circle and trophy aspect ratios. Crisp polished app UI, no phone frame, no new elements, no annotations.

# 数独界面效果图 · 第二版

方式：内置 image_gen。分辨率：1080×1920。仅供视觉审核，奖励数值与任务进度为示意。

- 难度选择：底部弹层，参考附件中间的两列三行格局；保留现有蓝白配色与首页。
- 每日挑战：增加月度全勤奖杯；先用天秤款做星座系列示意，每月主题映射待确认。
- 签到：简洁的居中弹窗。
- 每日礼包：左上金币、右上关闭，玩一局与完成今日挑战两项任务。

## 难度选择·底部弹层

```text
Use case: ui-mockup. Produce ONE polished Chinese mobile Sudoku interface concept, portrait 1080 x 1920, no phone frame, no perspective, no status bar or WeChat chrome, no annotations or watermark. Match our approved bright blue/white game style: #F9FCFF background, #2086FF accent, #0B204F navy typography, #EBF4FF soft surfaces, fine light-blue borders, softly rounded cards, clean Chinese type, restrained soft shadows, the same thin rounded line icons and familiar gold S coin. All Chinese text must be crisp and correct. Keep the interface flat and practical for Unity, generous spacing but compact content. No dark theme, no green palette, no giant 3D artwork. Every requested control and all content must be fully visible.
Input 1 is our approved HOME UI and is the exact visual identity and underlay to retain. Input 2 is LAYOUT REFERENCE ONLY: follow its MIDDLE PHONE's bottom-sheet pattern and two-column six-card layout. Do not copy its green colors, its home redesign, its numbered-stage labels, or its typography.
Show our existing blue home screen under a moderately translucent gray backdrop. Its top coin bar, sign-in/task/palette icons, and centered Sudoku logo+“数独” should remain recognizable behind the backdrop, with unchanged styling.
A WHITE bottom sheet slides up from the actual bottom edge and covers approximately the lower TWO THIRDS of the display. Top corners rounded about 40px. No screen-wide new page. A small light gray horizontal drag handle at top center; a thin X close button INSIDE the sheet at its upper right.
Inside, left-aligned heading “选择难度”, small supporting text “初级完成1/2，中级尚未解锁”. Compact TWO COLUMNS x THREE ROWS of selectable cards, in this reading order:
row1 “快速” (unlocked) and “初级” (selected);
row2 “中级” (locked) and “高级” (locked);
row3 “专家” (locked) and “大师” (locked).
Cards must be COMPACT and text-first like the middle layout reference, not large logo posters. Each has a title, two tiny completion-progress dots, and one short supporting line.
快速: both progress dots solid blue; supporting “轻松热身”.
初级: blue outline and pale-blue fill, check in upper right, one solid blue dot and one pale dot; supporting “基础逻辑”.
中级: small lock, two pale dots, supporting “初级完成2题解锁”.
高级: small lock, two pale dots, supporting “中级完成2题解锁”.
专家: small lock, two pale dots, supporting “高级完成2题解锁”.
大师: small lock, two pale dots, supporting “专家完成2题解锁”.
Below grid one full-width BLUE primary button “开始游戏”. Quiet small footer “完成前一难度2道不同题，解锁下一档”. Keep the six cards, the button, and the footer comfortably within the bottom sheet with no clipping. No bottom navigation on top of the sheet. Retain our blue/white style and existing home composition.
```

## 每日挑战·星座月度奖杯

```text
Use case: ui-mockup. Produce ONE polished Chinese mobile Sudoku interface concept, portrait 1080 x 1920, no phone frame, no perspective, no status bar or WeChat chrome, no annotations or watermark. Match our approved bright blue/white game style: #F9FCFF background, #2086FF accent, #0B204F navy typography, #EBF4FF soft surfaces, fine light-blue borders, softly rounded cards, clean Chinese type, restrained soft shadows, the same thin rounded line icons and familiar gold S coin. All Chinese text must be crisp and correct. Keep the interface flat and practical for Unity, generous spacing but compact content. No dark theme, no green palette, no giant 3D artwork. Every requested control and all content must be fully visible.
Edit the daily challenge page in input 1. Input 2 supports our established home design language. Preserve the page identity, accurate calendar, selected day, challenge controls, and four bottom navigation items.
NEW hierarchy: top header “每日挑战” with back chevron on left and a small trophy-outline entry “奖杯馆” on right. Replace the old introductory sentence with a compact monthly achievement card ABOVE the calendar.
Achievement card: left title “本月星座奖杯”, bold name “天秤之杯”, compact text “完成本月全部挑战即可获得”, a clear blue progress bar and “12 / 31”. On the RIGHT show a modest original LIBRA CONSTELLATION TROPHY, about 170px high: an elegant blue enamel cup with a small gold rim and a white/blue pedestal; integrate TWO balanced scale pans into the cup handles and a tiny Libra constellation of connected star points in the cup face. Recognizably a collectible TROPHY with a base, not a flat zodiac symbol. The trophy is shown softly muted with a tiny outline lock since it is still unearned; no huge hero art. Add a small note in the card “每月一款星座主题”. This is a monthly art theme, not a birth-date selector.
Below: a compact but readable rounded calendar card. Month controls “2026年10月” with left/right chevrons; weekday columns Monday first “一 二 三 四 五 六 日”.
EXACT grid rows:
[blank blank blank 1 2 3 4]
[5 6 7 8 9 10 11]
[12 13 14 15 16 17 18]
[19 20 21 22 23 24 25]
[26 27 28 29 30 31 blank].
Exactly these 12 completed days show small blue rings/checks: 3,4,5,7,8,9,11,12,15,17,20,22. Today 23 is solid blue with white number; 24-31 muted. Legend “已完成” and “今日”.
Under calendar a compact today's card “今日 · 10月23日”, “中级”, “今日尚未完成”, with one wide blue button “开始今日挑战”.
Bottom nav exactly “首页” | “每日挑战” | “统计” | “设置”, daily challenge active blue. Rebalance spacing so the new trophy achievement card, entire calendar, today's action, and bottom nav all fit. No giant trophy modal, no radial rays, no competitor look.
```

## 每日签到·简版弹窗

```text
Use case: ui-mockup. Produce ONE polished Chinese mobile Sudoku interface concept, portrait 1080 x 1920, no phone frame, no perspective, no status bar or WeChat chrome, no annotations or watermark. Match our approved bright blue/white game style: #F9FCFF background, #2086FF accent, #0B204F navy typography, #EBF4FF soft surfaces, fine light-blue borders, softly rounded cards, clean Chinese type, restrained soft shadows, the same thin rounded line icons and familiar gold S coin. All Chinese text must be crisp and correct. Keep the interface flat and practical for Unity, generous spacing but compact content. No dark theme, no green palette, no giant 3D artwork. Every requested control and all content must be fully visible.
Input 1 is the approved home underlay; input 2 supplies reward icon styling ONLY. Change daily sign-in from a full screen into a SMALL SIMPLE CENTERED MODAL over our dimmed home. Preserve the home visible around the modal: coin bar and Sudoku logo above, home controls/nav recognizable below the translucent gray backdrop.
A single white rounded popup about 900px wide and roughly 850-950px tall centered in the portrait screen. Do NOT make the modal nearly full screen. No back arrow or separate page header. Thin X close INSIDE top right of the popup. Center title “每日签到”, one short muted line “已连续签到2天”. No giant calendar illustration, no oversized slogan, no extra currencies, no paragraphs.
Below, one compact tidy 7-day reward block: FOUR equal small tiles in the first row, then tiles for day5 and day6 followed by one DOUBLE-WIDTH tile for day7 in the second row.
Each tile has only a small day label, one reward icon, and its reward number.
“第1天”: gold S coin, “20”, tiny claimed check.
“第2天”: gold S coin, “30”, tiny claimed check.
“第3天”: gold S coin, “50”, TODAY blue border/pale-blue fill, tiny tag “今日”.
“第4天”: our thin blue bulb, “×1”.
“第5天”: gold S coin, “80”.
“第6天”: gold S coin, “100”.
“第7天”: our thin blue bulb, “×2”. This wide tile remains small and clean, not a large bonus banner.
One wide blue button “领取奖励” at bottom inside the popup. All other decoration removed. All text readable, reward tiles balanced, generous modal margins. The user requested a plain quick sign-in popup.
```

## 每日礼包·每日任务弹窗

```text
Use case: ui-mockup. Produce ONE polished Chinese mobile Sudoku interface concept, portrait 1080 x 1920, no phone frame, no perspective, no status bar or WeChat chrome, no annotations or watermark. Match our approved bright blue/white game style: #F9FCFF background, #2086FF accent, #0B204F navy typography, #EBF4FF soft surfaces, fine light-blue borders, softly rounded cards, clean Chinese type, restrained soft shadows, the same thin rounded line icons and familiar gold S coin. All Chinese text must be crisp and correct. Keep the interface flat and practical for Unity, generous spacing but compact content. No dark theme, no green palette, no giant 3D artwork. Every requested control and all content must be fully visible.
Redesign the daily gift into a TASK REWARD POPUP over our dimmed approved home (input 1). Input 2 supplies our coin and hint visual style only; replace its large gift illustration and store-style claim layout.
One clean white rounded centered popup about 940px wide and 950px high, comfortably smaller than the full portrait screen. Top INSIDE popup: familiar GOLD COIN BALANCE “S 200 +” on the LEFT; thin X CLOSE on the RIGHT. No back arrow. Centered title “每日礼包” directly below that utility row, and a short muted subtitle “完成每日任务，领取奖励”.
Inside are exactly TWO clear wide horizontal rounded task cards stacked vertically.
Task1: small blue outline sudoku-grid icon at left, title “玩一局游戏”; clear progress “1 / 1” with a small full blue progress bar, small reward group with gold S coin and “50”; at the RIGHT a compact blue button “领取”. This task is completed but the reward is not yet collected.
Task2: small blue outline daily-calendar icon at left, title “完成今日挑战”; clear progress “0 / 1” and an empty pale-blue progress bar, small reward group with our blue line lightbulb and “×1”; at the RIGHT a compact blue-outline button “前往”. This task is unfinished.
Keep reward amounts visually subordinate and separate from progress so 1/1 and 50 cannot be confused. Enough card padding to avoid crowding. At the bottom inside popup only a subtle line “每日任务次日刷新”.
The popup should NOT include a giant gift box, free-gift product tiles, ad-claim buttons, a giant overall claim CTA, gems, shops, extra tasks, or a separate full-page footer. The two per-task buttons are the only task actions. Preserve the blue/white design system and original home behind the dim layer.
```

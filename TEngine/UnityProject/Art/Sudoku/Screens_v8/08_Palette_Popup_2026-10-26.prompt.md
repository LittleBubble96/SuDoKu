# 调色盘 UI 效果图（2026-10-26）

工具：内置 image_gen（imagegen skill）。
实际图片尺寸：941x1672；竖屏。
状态：效果图预览；未切图，未修改 UI 或业务代码。
依据：既有蓝白游戏页、设置页六色入口及 SudokuThemeManager 的六套固定主题色。

## 完整生成提示词

Use case: ui-mockup.
Create one polished portrait mobile Sudoku THEME PALETTE PICKER mockup, 1080 x 1920 composition, edge-to-edge flat UI screenshot with NO phone device frame and NO explanatory design board.
Input image 1 is the existing game's blue-white gameplay style reference and background layout. Input image 2 is the settings style reference only. This is a new standalone theme-picker mockup, not the settings screen.
Match the project's bright, simple, spacious white/light-blue UI, delicate rounded blue outlines, navy typography, consistent rounded line icons. White background #F9FCFF, surface #FCFDFF, main blue #2086FF, text #0B204F. Crisp readable Chinese sans-serif, minimal soft shadow, no glassmorphism or textures.
Background: same existing Sudoku gameplay screen, top back arrow, pause centered, palette and gear at right; faintly visible 9x9 board, bottom tools and 1-9 keys. Overlay a uniform subtle navy translucent dimmer so it is recognizably a popup without looking gloomy.
Main subject: centered white rounded rectangular dialog, around 900px wide, generously padded, approximately 1120px tall. At the top, a small blue outlined palette icon next to the bold title “主题配色”; close X on the right. A short muted subtitle “选择喜欢的颜色，即刻焕新”.
Below, exactly SIX theme option cards in TWO columns and THREE rows. All cards same size, softly rounded with a thin pastel outline. Each card contains a tidy MINIATURE 3x3 SUDOKU PREVIEW at left (simple digits 1, 5, 9, one pale selected cell, thin grid), and the theme name with a small solid accent-color swatch at right. Use these exact row-by-row labels and hues:
row1 left “经典亮色”: accent #2086FF, pale tint #EBF4FF;
row1 right “薄荷”: accent #33866C, pale tint #EDF4F2;
row2 left “天空”: accent #487CB5, pale tint #EFF3F8;
row2 right “蜜桃”: accent #BD7065, pale tint #F9F2F1;
row3 left “浅紫”: accent #856EA7, pale tint #F4F2F7;
row3 right “杏色”: accent #B58745, pale tint #F8F4EE.
Every option has a bright near-white base; colored swatches and pale cell highlights make the themes distinct, never dark themes. All text remains clearly legible.
Selected state: ONLY the upper-left “经典亮色” card has a stronger blue border, very pale blue fill, and a blue circular badge with a white check near its top-right. Other five cards have no check. Do not use lock, ads, price, purchase or rarity badges.
At the bottom of the popup a single understated muted sentence “主题将同步应用到所有界面”. No redundant apply/confirm button; options imply instant selection. No arbitrary extra icons, no watermark, no developer labels or color codes rendered on image.
Preserve the existing game's clean understated art direction; this image must look ready for approval as an actual Unity mobile game UI.
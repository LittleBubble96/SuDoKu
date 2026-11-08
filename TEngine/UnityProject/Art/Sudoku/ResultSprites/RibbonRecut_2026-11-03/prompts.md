# 横幅重切

日期：2026-11-03。使用内置 image_gen，透明背景；以原效果图的横幅裁切为输入，去字和补全被文字遮挡的蓝色表面。采用第一版生成图；第二版清理候选因外沿更宽未采用。

## 输入与提示词

输入：Art/Sudoku/ResultSprites/Source/RibbonReference.png。

Use case: background-extraction / precise-object-edit. Asset type: production game UI ribbon sprite. The provided tight crop is the EDIT TARGET. Re-extract exactly this existing BLUE victory ribbon. Remove ALL Chinese white lettering completely and smoothly reconstruct the blue face behind it. Remove ALL dark sudoku background, digits and grid lines. Keep the original full silhouette, exact wide-to-short proportions (roughly 4.1:1), broad gently arched front with a curved top and shallow concave lower edge, pointed V-cut side tails, small dark-blue curled loops below the left/right ends, cyan thin rim highlights and brilliant azure-to-deep-blue vertical gradients. Do NOT redesign it, inflate it, add fabric weave, noise, mottled texture, extra folds, decorations, gold, stars, symbols or text. The original material is smooth and glossy with clean continuous gradients. Output one complete centered horizontal ribbon, face entirely BLANK, tails and rolled ends intact. Genuine transparent RGBA background with clean antialiased contours and tight margins; no checkerboard baked into the image, no shadow outside the silhouette, no background glow. Preserve blue color in this clean source asset, to be used for subsequent palette-ready export.

## 导出

- Source/Ribbon_Blue_Generated.png：选用的内置工具透明源图。
- Ribbon_Blue.png：824×203，无字蓝色参考成品。
- RibbonPreview.png：蓝色参考和三套主题叠色检查。
- 游戏内仍使用原 Raw/UI/Guide 下的三张同名白图；RGB 全白、Alpha 保存形状及明暗，尺寸及共同边界保持 824×203。
- 以机械 Alpha 剪裁去除低透明杂点并保留最大连通横幅，原色不重绘，再统一裁边和缩放。三层使用相同位置与尺寸，底形跟随主题主色，阴影黑色、高光白色。
- 导出源码保存在 work/ribbon-recut/ExportRibbon.cs。替换前的 PNG 与 meta 已备份；meta 没有修改。

该横幅是去字后补全的散图，不是原始分层美术源文件。普通 Image 使用 Simple + Preserve Aspect，不设置九宫。

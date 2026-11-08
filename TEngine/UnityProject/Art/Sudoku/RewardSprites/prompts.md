# 通用领奖散图提示词

日期：2026-11-03。使用内置 image_gen，`transparent_background=true`，未使用 CLI/API。输入是原效果图局部裁切，不修改原图。

## gift

输入：Source/GiftReference.png

Use case: background-extraction / precise-object-edit. This image is the EDIT TARGET from an approved mobile game UI. Extract ONLY the exact OPEN red-and-gold gift box: the glossy square red base with gold vertical ribbon, and the complete floating tilted red lid with its full looped gold bow. Keep the precise camera angle, red panel shapes, gold trims, lid tilt, relative base/lid alignment and gap, all shiny material highlights. Entire base and lid must be retained as one composition, two separate physical pieces with a transparent gap. Remove ALL golden rays, external glow, sparkle stars, confetti, dark background and ground shadow. Preserve the pale gold reflective interior of the open box as opaque material; no external light beams. Do not close the box, redesign the bow, add objects or add text. Production sprite with real RGBA transparency, clean antialiased outlines and small fully transparent margins, no checkerboard or black matte.

## light

输入：Source/LightReference.png

Use case: precise-object-edit / background-extraction. Input is the EDIT TARGET: golden opening light in a red-gold gift reward UI. Extract and reconstruct ONLY the warm golden light emerging upwards from the open box. Completely REMOVE the red box, red lid, gold bow, stars, blue/gold confetti and dark background, then restore the light rays behind those objects. Light originates at one bright warm-white/gold core near the lower center, fans upwards in several soft golden shafts through a triangular opening cone, with a subtle soft halo. Keep the gentle game UI lighting style and the original upward direction. No full circular sunburst, no hard polygon edges, no objects, no box, no particles, no text, no dark rectangle. The core should remain visible for layering. True transparent RGBA background with softly feathered alpha, no baked checkerboard. Standalone light effect.

## coin

输入：Source/CoinReference.png

Use case: background-extraction. Input is the EDIT TARGET: one glossy GOLD COIN with an embossed FIVE-POINT STAR from an approved game reward UI. Extract exactly that single coin, preserving the raised star, thin rim rings, orange-gold beveled edge, slight right-side depth, orientation and glossy highlights. Remove the number 100, every letter/digit, dark card background, border, and external glow/shadow. Do NOT change the star to an S, add coins or redesign the emblem. Solid opaque gold coin on genuine RGBA transparency, clean antialiased circular outline, small fully transparent margin. No checkerboard, no text.

## hint

输入：Source/HintReference.png

Use case: background-extraction. Input is the EDIT TARGET: one 3D golden-yellow hint light bulb from an approved game reward UI. Extract exactly the same gold bulb with its round glass, white/yellow inner filament, silver-blue screw base and five short gold light dashes above and beside it. Preserve the original polished soft 3D icon style, proportions, outline and all five light dashes. Remove the number 1, all text, dark background, blue card border and diffuse external background glow. No coins, no new props, no redesign into a flat line icon. Fully opaque gold and silver material, genuine transparent RGBA background between and outside all pieces, clean antialiased edges, small transparent margins. No checkerboard or black matte.

## 机械导出

生成的彩色源图保留在 Source。通过 C# 切图导出共同规格、紧凑轮廓、清理低 Alpha 杂点；礼盒保留盒盖与盒身两部分，灯泡保留主体和五个光线短条。金币/灯泡去掉奖励数量，礼盒与外部光效分开。星光、彩纸直接提取原图轮廓，RGB 统一为白色，Alpha 保留形状与边缘，未重新生成。

生成补全不是原始分层美术源文件。导出代码：work/reward-sprites/ExportRewardSprites.cs。

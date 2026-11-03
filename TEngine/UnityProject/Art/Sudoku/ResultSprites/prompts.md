# 内置 image_gen 提示词

日期：2026-10-29。全部使用内置工具，`transparent_background=true`；未使用 CLI/API。

三份输入分别为原参考图中横幅、礼盒和光芒的紧凑裁切图。原参考图为 `Art/最终效果图/10_结算_蓝色横幅_红金礼包_金色光芒.png`。

## 无字横幅

Use case: background-extraction / precise-object-edit. Input image is the edit target, a tightly cropped blue game victory ribbon from an approved UI mockup. Extract ONLY this same blue ribbon, faithfully preserving its wide shallow arched center, perspective, curled folds under both sides, V-cut tails, cyan rim highlights and smooth rich blue shaded material. Remove ALL white Chinese letters completely and repair the blue cloth behind them smoothly; the front face must be entirely blank with NO letters or symbols. Remove the dark sudoku background and numbers. Produce the SAME single ribbon silhouette and proportions, NOT a new banner design. Genuine transparent background, no checker pattern, no outer drop shadow. Entire ribbon centered, all tails and folds intact, minimal transparent margins. This is a clean production sprite for an existing game.

生成后按用户切图规则转换为共用边界的三张纯白 Alpha 图，分别提供底形、阴影和高光，适配调色盘。

## 红金礼盒

Use case: background-extraction. Input is the edit target: a red and gold gift box cropped from an approved game UI. Extract ONLY the very same glossy red gift box with golden ribbon and the complete looping gold bow, matching its front view, small right-side face, proportions, red panels, gold straps and specular highlights exactly. Keep the entire gift silhouette, no redesign or additional decorative elements. REMOVE all golden rays/glow behind it and the background. No detached particles, no outside illumination halo, no ground shadow. Genuine transparent background, one centered full gift, tight transparent margins. Red and gold opaque material, transparent only outside the gift. Do not add any text.

## 金色光芒

Use case: precise-object-edit / background-extraction. Input is the edit target, cropped golden sunburst behind a gift in an approved game UI. Extract and clean ONLY the golden celebratory radial light rays and soft center glow from this image. Completely REMOVE the gift box and restore the rays behind it at the center, remove all sudoku grid lines, dark numbers, white numbers at top edge, and blue line at bottom edge. Preserve the original ray directions, irregular narrow and broad rays, rich warm golden color, compact centered radial distribution, soft fading edges. All rays originate at a single warm luminous center. No star particles, no ring, no gift, no objects, no text, no black background or grey rectangle. Genuine RGBA transparency with softly feathered alpha all around, one standalone light effect for layering behind the existing gift.

生成后按目标显示比例缩放，并对裁切边沿作短距离透明收边；金色和中央光心保留。

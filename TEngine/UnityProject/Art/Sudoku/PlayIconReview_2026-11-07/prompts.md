# 播放图标审核稿

生成方式：内置 `image_gen`，透明背景。中心三角形按下列提示词生成；固定导出时清除低透明杂点、保留最大连通图形、统一为白色 RGB + Alpha，缩放到 38×44。外圈逐像素复用既有 `Assets/AssetRaw/UIRaw/Atlas/Sudoku/icon_pause.png`，最终画布保持 123×124。

`icon_play.png` 为已审核透明 PNG；`Preview.png` 是与现有暂停图标的着色对比。2026-11-07 用户确认后已复制到 `Assets/AssetRaw/UIRaw/Atlas/Sudoku/icon_play.png`，资源地址 `Sudoku_icon_play`。`UISudoku` 暂停时显示三角形，继续时恢复双竖线；两态统一保留主题正文色，不随暂停状态变色。图标异步加载、窗口缓存时复用、销毁时释放。

已在实际 Play 模式验证暂停/继续的按钮点击、两态颜色一致、暂停计时与题面隐藏、继续计时、快速连点、换主题后的颜色一致、窗口缓存重开。玩家存档与测试前备份逐字节一致。Unity 导入验证为 Single / FullRect / PPU100 / Alpha / 无 Mipmap / NPOT 不缩放 / 不压缩，原暂停与新继续图标尺寸、Pivot 一致；发布与 WebGL 条件编译通过。报告见 `work/pause-icon`。

## 最终提示词

Use case: logo-brand. Asset type: Unity uGUI single-color playback symbol, transparent PNG.
The supplied image is only a visual style/scale reference: a very simple white pause icon with a fine circular outline and two solid vertical bars. Generate ONLY the corresponding central PLAY TRIANGLE, no outer circle. A single compact solid right-pointing equilateral-ish triangle, with very slightly rounded corners, an upright vertical left edge, clean straight diagonal edges, flat pure white RGB. Its width-to-height ratio should be about 0.86 (38 wide by 44 high). The symbol should feel like the same family as the reference's solid pause bars, readable when tiny. Center it on a square transparent canvas with about 25 percent empty margins. Actual transparent background, including all space outside the triangle. No outline-only triangle, no circular frame, no gray or colored fill, no gradient, no shadow, no glow, no text, no extra shapes. Production-ready crisp antialiasing. The outer circle will be reused pixel-for-pixel from the existing project icon separately.


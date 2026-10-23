# 奖杯底部柔影补切

文件：`Assets/AssetRaw/UIRaw/Atlas/Sudoku/bg_trophy_shadow.png`，320×37，7,235 字节。纯白 RGB、真实透明 Alpha，249 档透明度，四周保留少量透明余量。

这是单独的底部投影，不与任何奖杯合并，可供 12 款奖杯共用。使用 Image 的 Simple 类型，放在奖杯后方，中心位于底座下缘附近。以 1080×1920 画布上的 320 像素宽奖杯为参考，可从 320×37 开始调整；阴影按自身比例缩放，不使用 Sliced，避免拉出矩形边缘。

建议颜色 `#3F739F`、Image Alpha 约 0.24；灰色奖杯也可共用。需要跟随主题时，可挂现有 UIThemeColor，Role 使用 MutedText 或 StrongLine，并保留你设置的 Alpha。选择 Accent 会更鲜艳，可按实际画面决定。

本次仅新增散图和导入设置，未改动 UI 预制体。`TrophyShadow_Preview.png` 是配合奖杯的组合预览，位于 Art 内，不作为运行资源。

源图通过内置 image_gen 分离，再沿用工程 C# 导出流程提取最大阴影连通区、等比裁切并规范为纯白 Alpha 遮罩。源图保存在 Source，导出源码位于 `TEngine/Tests/Sudoku/ArtExport/CutTrophyShadow.cs`。

## 生成提示词

Asset extraction for a Unity UI sprite. Reference image: existing blue-white Sudoku Daily Challenge UI. Extract ONLY the very soft, wide elliptical ground/contact shadow directly beneath the blue-and-gold lion trophy's base. Do not include the trophy, base, lock badge, panel, background, calendar, text or other icons.
Deliver ONE isolated horizontal soft oval alpha-mask sprite on an ACTUALLY TRANSPARENT background. All foreground RGB must be pure white (#FFFFFF); its soft shadow shape is encoded only in smoothly graded alpha. The UI will tint this white mask muted blue-gray at runtime. The oval is roughly six times wider than tall, gently asymmetric like the reference floor shadow, darkest/strongest just under the center of the trophy, fading smoothly in all directions to complete transparency. It should be broad, flat, subtle and feathered, with no hard contour, no ring, no rectangle, no bevel, no halo glow. Do not paint any checkerboard. Center it compactly with a small clear transparent margin. Aim for an asset equivalent to about 320px wide by 50px tall when trimmed; do not bake a dark or blue RGB color into it. This is a reusable independent shadow, not a new UI mockup.

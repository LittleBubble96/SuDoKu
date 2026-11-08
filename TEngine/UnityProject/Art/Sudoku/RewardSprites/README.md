# 通用领奖补切散图

参考：`Art/最终效果图/11_通用领奖_红金礼包.png`。日期：2026-11-03。

本次仅补缺少的 **6 张透明 PNG**，输出到 `Assets/AssetRaw/UIRaw/Atlas/Sudoku/`。不拼 UI，不修改已有素材、预制体、代码或图集设置。原图为 941×1672，建议显示尺寸按 1080 宽竖屏画布换算，保留原图元素比例。文字、数量和按钮文案不切为图片。

## 新增资源

| 文件 / 资源地址 | PNG / 建议显示尺寸 | 配色 | 使用方式 |
| --- | --- | --- | --- |
| `gift_box_open_red_gold.png` / `Sudoku_gift_box_open_red_gold` | 312×400 | 红金固有色 | 打开状态礼盒，浮起的盒盖和盒身保留同一构图，不含外部光芒、星光或彩纸 |
| `fx_reward_open_light.png` / `Sudoku_fx_reward_open_light` | 384×256 | 金色固有色 | 独立向上发散的开盒光效，软透明边缘，置于礼盒后面并把光心对齐盒口 |
| `icon_reward_coin_star_gold.png` / `Sudoku_icon_reward_coin_star_gold` | 120×120 | 金色固有色 | 五角星款金币，无奖励数量；原 `coin_gold` 是字母款，保留原素材 |
| `icon_reward_hint_gold.png` / `Sudoku_icon_reward_hint_gold` | 122×126 | 金色 / 银色固有色 | 金色提示灯泡和五条短光线，不含数量；原线稿 `icon_hint` 继续用于局内按钮 |
| `fx_reward_sparkle.png` / `Sudoku_fx_reward_sparkle` | 38×46 | 纯白 RGB + Alpha | 四角星光，可通过 Image.color 设为金色 / 白色，旋转、缩放和渐隐复用 |
| `fx_reward_confetti.png` / `Sudoku_fx_reward_confetti` | 34×38 | 纯白 RGB + Alpha | 弯曲彩纸，可通过 Image.color 随调色盘换色，复制后旋转 / 缩放复用 |

六张均使用 **Image / Simple + Preserve Aspect**，不设置九宫。彩色礼盒与奖励图标的 Image.color 保持白色。星光和彩纸通过 Alpha 保留轮廓与抗锯齿，不把蓝色固定到图中。

`ContactSheet.png` 为独立资源与组合预览。`NewSprites.json` 提供尺寸、路径、资源地址、来源和 SHA256。

## 叠放建议

1. 全屏遮罩使用已有白色底图和黑色 Image.color / Alpha。
2. 开盒光效放在礼盒后方，把光心对齐盒口。光效内部光心约为左上原点 `(0.48, 0.78)`，换成 Unity Pivot 约 `(0.48, 0.22)`；这里只是建议，不修改导入 Pivot。
3. 礼盒是盒盖与盒身两块可见形状，共同保存在一张透明图内。两者之间透明，可透出后方光效。当前不是可分别动画的两张图，不要把数量文案放进此图。
4. 星光和彩纸分别叠放，可用多个 Image 实例实现效果图中的不同位置、大小、角度与颜色。
5. 两张奖励图标放到复用的卡片中；数量、金币 / 提示名称和标题使用 TMP。

## 复用现有资源

| 画面元素 | 复用 |
| --- | --- |
| 奖励卡片底板、圆角描边 | `bg_card_fill_9s`、`bg_card_outline_9s`，Sliced；分别调节底色透明度与主题描边 |
| 领取、双倍领取按钮底板与描边 | `bg_button_fill_9s`、`bg_button_outline_9s`，Sliced；颜色跟随主题 |
| 右上角关闭 | `icon_close` |
| 广告视频标识 | `icon_ad_video` / 小号使用 `icon_hint_video` |
| 黑色遮罩 | `bg_white_quad_9s` 或现有全屏 Image |
| 已有闭合礼盒与圆形放射光 | `gift_box_red_gold`、`fx_reward_gold_rays`，继续保留，未覆盖 |
| 标题、奖励数量、图标下方名称、按钮及广告说明 | 现有 TMP 与多语言，不烘焙图片 |

通用底板沿用项目已有样式，没有为参考图另做固定蓝色按钮或整张奖励卡片，保证调色盘可换色。

## 来源及验证

- 礼盒、开盒光效、金币和灯泡使用内置 image_gen 提取并补全，`transparent_background=true`。提示词见 `prompts.md`，原参考裁切与生成源图在 `Source/`。
- 遮挡和去背景区域经过补全，这些是可用切图，不是原始分层美术源文件。
- 星光、彩纸直接从效果图提取轮廓，转为白色 RGB + Alpha；没有重新生成或加入额外图案。
- 已检查尺寸、RGBA、纯白图规范、透明外边缘与形状连通性：礼盒两块、灯泡主体加五条光线，未丢失独立部分。
- `PixelValidation.txt` 记录六张检查；`ExistingAssetsValidation.txt` 验证原 192 个 PNG / meta 文件及参考图哈希未改变。
- 没有手写 Sprite meta，没有添加导入脚本。PNG 由工程已有导入流程处理；本次未驱动 Unity 刷新，也未验证 Unity 内实际导入或播放。

Sprite 导入沿用项目规范：Single、PPU 100、Full Rect、透明 Alpha、无 Mipmap、NPOT 不缩放、不压缩。新素材不是九宫图，Sprite Border 为零。

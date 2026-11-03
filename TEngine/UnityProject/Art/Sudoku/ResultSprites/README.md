# 结算页补切散图

参考：`Art/最终效果图/10_结算_蓝色横幅_红金礼包_金色光芒.png`。

只补当前图集中不存在的部分，共 **6 张 PNG**，导出至 `Assets/AssetRaw/UIRaw/Atlas/Sudoku`。没有拼接 UI，没有修改预制体或业务代码。原参考图实际为 941×1672，按宽 1080 的竖屏画布等比换算，保持图中各元素的显示比例。

## 新增资源

| 文件／资源地址 | PNG／建议显示尺寸 | 调色盘 | 使用方式 |
| --- | --- | --- | --- |
| `bg_result_ribbon.png`／`Sudoku_bg_result_ribbon` | 824×203 | 白图，用主题主色 | 无字横幅底形 |
| `bg_result_ribbon_shade.png`／`Sudoku_bg_result_ribbon_shade` | 824×203 | 白图，Image 固定黑色 | 横幅褶皱、暗部，叠在底形上 |
| `bg_result_ribbon_highlight.png`／`Sudoku_bg_result_ribbon_highlight` | 824×203 | 白图，Image 固定白色 | 横幅高光，叠在阴影上 |
| `gift_box_red_gold.png`／`Sudoku_gift_box_red_gold` | 176×171 | 保留红金固有色 | Image.color 用白色 |
| `fx_reward_gold_rays.png`／`Sudoku_fx_reward_gold_rays` | 446×343 | 保留金色固有色 | 放在礼盒后方，透明外沿，可独立旋转、缩放或渐显 |
| `icon_share.png`／`Sudoku_icon_share` | 59×67 | 白图，使用主题色／主题浅色 | 挑战好友分享图标 |

三张横幅图 **RGB 全白，细节在 Alpha 内**。三层必须使用相同的 RectTransform 尺寸、位置和缩放，不能分别 Set Native Size 后再各自裁边。导出已统一共同边界，只有 1 像素透明外沿。图标与礼盒同样紧凑裁切，保留抗锯齿。

## 横幅与调色盘的搭法

从下往上：

1. 底形 `bg_result_ribbon`：挂现有 `UIThemeColor`，角色使用 `SudokuThemeRole.Accent`。
2. 阴影 `bg_result_ribbon_shade`：Image.color 为黑色，Alpha 设 1；PNG 自带阴影透明度，不需要跟随主题修改黑色。
3. 高光 `bg_result_ribbon_highlight`：Image.color 为白色，Alpha 设 1；PNG 自带高光透明度，可按需要减弱。
4. “挑战完成”使用单独 TMP 文本，白色。横幅中没有烘焙文字。

这样只需改变底形主色，明暗就会自然叠加到新主题上。阴影和高光资源本身也是白图，没有固定蓝色。`ContactSheet.png` 提供经典蓝、薄荷绿和蜜桃三种叠色预览。

分享图标使用 `UIThemeColor` 的主色或所需浅色角色。礼盒、金色光芒保留红金配色，Image.color 为白色，不挂主题变色组件。

光芒的光心在图内约 `(0.51, 0.56)`，以左上角为原点；使用 Unity Pivot 时可设约 `(0.51, 0.44)`，并与礼盒中心对齐。此处只是摆放建议，没有修改 Sprite 的导入 Pivot。

## 九宫和复用

新增的横幅有完整弧线、卷边和燕尾，不能用普通九宫任意拉伸；礼盒、放射光芒和分享图标也应保持形状。因此这 6 张都使用 **Simple + Preserve Aspect**，不要设置九宫边距。横幅三层建议一起整体等比缩放。

可九宫的部分直接复用已有资源及其 Sprite Border，不再重复切图：

| 结算页部件 | 复用 |
| --- | --- |
| 继续游戏按钮底板、描边 | `bg_button_fill_9s`、`bg_button_outline_9s`，Image Type 设 Sliced，颜色跟随调色盘 |
| 本局得分、历史最高奖杯 | `icon_trophy` |
| 本局用时 | `icon_clock` |
| 最快用时 | `icon_stat_best_time` |
| 返回主页图标 | `icon_home_outline` |
| 全屏黑底遮罩 | `bg_white_quad_9s`／现有全屏 Image，使用黑色和所需 Alpha |
| 标题、统计名称、数字、时间及按钮文案 | 现有 TMP 字体，不切成图片 |

复用按钮底板为现有通用样式；本次未新增或烘焙参考图中按钮的蓝色渐变整图，避免把蓝色固定在调色盘素材中。

## 来源及检查

- 横幅去字、礼盒去背景、光芒去除礼盒遮挡：使用内置 `image_gen` 清理／补全透明源图。具体提示词见 `prompts.md`，生成的透明源图保存在 `Source` 下。源图里被文字或礼盒遮挡的区域经过补全，不是原始分层美术源文件。
- 分享图标直接从原参考图提取轮廓并转成纯白 Alpha 图。
- `NewSprites.json` 记录尺寸、资源地址、来源区域和层级。
- `PixelValidation.txt` 记录透明边缘、纯白 RGB、尺寸和共用边界检查。
- `ExistingAssetsValidation.txt` 记录原有 PNG、meta 与参考图的哈希比对。
- 没有手写或修改 Sprite meta，也没有添加导入脚本。新 PNG 由项目已有图集导入流程处理。
- 本次没有驱动 Unity 刷新、修改图集或进入播放。Unity 内的实际导入未验证。

参考图仍保留在原来的“最终效果图”目录；预览和生成源图只放在本补切目录，不混入游戏图集。

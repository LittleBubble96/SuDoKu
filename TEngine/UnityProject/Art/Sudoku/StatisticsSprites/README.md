# 统计页补切图标

来源：`Art/最终效果图/14_统计信息_最新待确认.png`，2026-10-28 的中性灰列表版。源图实际尺寸为 941×1672，本次按宽 1080 的竖屏画布等比换算，保留与效果图相同的比例。

只补缺少的图标，共 8 张。PNG 位于 `Assets/AssetRaw/UIRaw/Atlas/Sudoku`，RGB 全白、透明 Alpha、紧凑边界加 1 像素透明余量，可用 Image.color 随主题换色。保留原图轮廓和抗锯齿，没有重画图标，没有烘焙文字、数字或灰色卡片。

## 新增文件

| 文件 | 用途 | PNG／建议显示尺寸 |
| --- | --- | --- |
| icon_stat_games.png | 已玩游戏：无数字九宫格 | 67×71 |
| icon_stat_win_rate.png | 胜率：普通旗帜 | 57×75 |
| icon_stat_perfect_win.png | 无误胜出：格纹旗帜 | 62×72 |
| icon_stat_best_time.png | 最快用时：秒表 | 62×73 |
| icon_stat_total_time.png | 累计用时：沙漏 | 52×72 |
| icon_stat_current_streak.png | 当前连胜记录：闪电火焰 | 62×75 |
| icon_stat_longest_streak.png | 最长连胜：星形奖章 | 63×81 |
| icon_statistics_active.png | 底栏统计选中态：实心柱状图 | 62×65 |

普通图标使用 **Image / Simple + Preserve Aspect**，不要九宫拉伸。建议尺寸基于 1080 宽画布；其他分辨率整体等比缩放。

## 复用现有素材

| 页面元素 | 复用文件 |
| --- | --- |
| 已通关游戏 | icon_trophy.png |
| 平均用时 | icon_clock.png |
| 首页未选中 | icon_home_outline.png |
| 每日挑战 | icon_daily_challenge_grid_star.png |
| 统计未选中 | icon_statistics.png |
| 设置 | icon_settings.png |
| 行背景、描边 | bg_card_fill_9s.png、bg_card_outline_9s.png，用中性灰着色 |
| 难度页签及选中底板 | 现有九宫底板／胶囊底板 |
| 所有文案、数字 | 继续用 TMP，不切成图片 |

## 检查与导入

- `ContactSheet.png`：白图和蓝色着色的检查预览，仅保存在 Art，不进入图集。
- `NewSprites.json`：每张图的尺寸、源图裁切位置和资源路径。
- `PixelValidation.txt`：全白 RGB、透明外沿、抗锯齿和紧凑边界检查。
- `ExistingAssetsValidation.txt`：原有 PNG 和 meta 的哈希比对结果。
- 未修改或手写任何 Sprite meta。项目现有 SpritePostprocessor 会在 Unity 正常刷新后按 Atlas 配置导入新 PNG；当前已开启自动图集处理并关闭 Mipmap。
- 本次没有控制编辑器刷新或进入播放，也没有声称完成 Unity 实际导入验证。

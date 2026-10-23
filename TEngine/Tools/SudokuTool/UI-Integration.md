# UISudoku 接入说明

目标项目为 `E:/xx/Sudoku/TEngine/UnityProject`。参考项目只用于阅读，没有写入。

## 首页与难度选择（2026-10-23 已确认）

沿用 `E:/xx/NewText/TEngine/UnityProject` 的启动和 UI 生命周期流程，启动进入 `UIHome`。保留用户搭建的 `UIHome`、`UIHome_Home`、`UIHomeNewGame` 布局，通过 `ScriptGenerator`、`FindChildComponent`、`XYButton` 和 Widget 接入。

首页在 `Content` 内异步加载页签对应的预制体；目前四个页签都显示 `UIHome_Home`，预留映射，已加载内容复用。选中页签的文字、图标使用当前主题 Accent，其他使用非选中颜色。绑定金币、继续游戏的难度与用时、新游戏；首页期间停止对局计时。其他未实现入口暂不接业务，不新增关卡缓存，只复用当前已有的对局和通关缓存。

新游戏打开 `UIHomeNewGame`。六档难度在 `OnCreate` 使用现有 `NewGameSelectDiffItem` 模板复制，每档在自身 `OnCreate` 根据解锁所需数量复制 `ProgressItem`，目前固定 2 个点，完成不同题后点亮。沿用前一档完成两道不同题解锁下一档的规则，重玩不重复累计。

难度项中的 `bg/Lock` 仅在未解锁时显示；`bg/SelectRoot` 仅在已解锁且当前选中时显示。已解锁但未选中的项隐藏这两个节点。锁使用 MutedText、选中圆底使用 Accent、选中对号使用 Surface，均复用现有主题订阅。

选择已解锁档位后，点击开始进入该档第一道未完成题；该档全部完成则从第一题重玩。仅开始时替换当前对局；唯一关闭按钮为 `Close_Btn`，关闭选择弹窗不更改旧对局。

弹窗使用 `UIWindow.OnInAnimation` / `OnOutAnimation`：`Bottom` 从屏幕下方滑入，遮罩渐显；离开时向下滑出，遮罩隐藏；动画结束调用框架完成回调。异步加载和关闭都需要处理取消、重复点击与过期回调。窗口、页签、难度项和进度点均接入现有调色盘；金币图标保留原金色。

补齐非 GM 文案缺失的英文，复用既有 Language.xlsx、导表和 FeiHuaSongTi-2 SDF 字体流程。GM 专用文案不补译，不创建新字体。

已实现入口为 `SudokuPresenter.ShowHomeAsync()`、`ContinueGameAsync()` 和 `StartNewGameAsync(difficulty)`。返回首页保留填写、笔记和用时；继续时解除此前的暂停。当前题已经完成时，继续按钮不可用，通过新游戏选择下一道题。金币加号、签到、每日任务、其他游戏和每日挑战卡片保留布局，尚未接入对应业务；调色盘按钮继续发送现有入口事件，换色订阅已经接入。

`UIHomeNewGame.cs` 顶部 `InDuration` / `OutDuration` 可调整弹窗速度。遮罩使用预制体根节点的 Image，保留原颜色和最终透明度；关闭只绑定 `Panel/Close_Btn`，通过 `HideUI` 调用框架退场动画。运行时生成的难度项、进度点和主题组件都放在现有预制体节点下，不重建布局或替换图标。

多语言表新增的两条难度说明改成参数格式：`home_new_game_des` 接收难度名称 `{0}`，`home_new_game_diff_des` 接收前一难度名称 `{0}` 和题数 `{1}`。本次补齐 36 个英文单元格，语言资源已经导出，现有字体已重新生成。使用这两个键时不要再按原先固定的“中级 / 初级”处理。

实际交互验证报告位于 `Tests/Sudoku/Integration/home-validation.txt`、`startup-validation.txt`，中英文截图同目录。覆盖异步子界面加载与复用、四页签主题色、金币、首页停表、继续/返回、六档难度、2 个通关点、锁定档位、弹窗进出/缓存重开、取消保留旧局、新游戏选题和原有对局功能。测试结束恢复测试前存档。

## 界面与生命周期

`UISudoku.ScriptGenerator()` 按 `UICorePlay` 的方式使用 `FindChildComponent`、`CreateWidget<XYButton>` 和独立 Widget 绑定现有节点，事件使用 `AddUIEvent`。旧 `UISudokuView` 引用组件已移除。

窗口和题包通过 UniTask 异步加载。棋盘的隐藏 `SudoCell` 已随窗口就绪，直接复制出 81 格，不再逐格加载资源。`OnCreate` 从唯一的 `Num1` 复制到 `Num9`，保留父节点的 LayoutGroup、间距和尺寸。缓存重开只刷新，不重复生成；销毁重开会重新绑定。

`SudokuBoardWidget` 根据棋盘实际尺寸定位格子及 20 条线，保留预制体中的线宽。当前棋盘仍为 990×990。节点路径变更后，需要同步调整 `UISudoku` / Widget 中的路径；适配与位置由预制体控制。

局内已接入选择格子、数字、笔记、清除、撤销、提示、用时、填写进度。暂停按钮首次点击暂停、再次点击继续，暂停时隐藏格子并停止计时及输入。界面只显示难度，不显示第几关。

## 换色与待接弹窗

`UIThemeColor` 可挂在 Image 或 TMP 的同一物体上，Inspector 的 Role 指定语义颜色，保留原透明度。现有需要换色的图形已配置；金币图标保留金色。组件在 OnEnable / OnDisable 订阅及注销主题事件，不轮询颜色。

`SudokuThemeManager.SetTheme(themeId)` 统一更新并保存，编号 0～5：经典亮色、薄荷、天空、蜜桃、浅紫、杏色。`GetAccent(id)` 可用于后续弹窗的色块，`CurrentThemeId` 可用于选中标识。

调色盘和设置尚未搭建，不生成临时弹窗。按钮先保存进度，再发送无参数事件；返回入口已经由 `SudokuAppBridge` 接到异步首页流程：

| 按钮 | 事件 / 调用 |
| --- | --- |
| 调色盘 | `EventDefine.SudokuPaletteRequested` |
| 设置 | `EventDefine.SudokuSettingsRequested` |
| 返回 | `EventDefine.SudokuBackRequested` → `SudokuPresenter.ShowHomeAsync()` |
| 色块选择 | `SudokuThemeManager.SetTheme(id)` |
| 音量设置 | `SudokuPresenter.SetAudioVolumes(music, sound)`，范围 0～1 |
| 重开 / 下一题 / 切难度 | `SudokuPresenter.Current.Restart()` / `NextLevel()` / `OpenLevel(difficulty, index)` |

调色盘和设置入口当前只发事件，不显示尚未搭建的弹窗。后续弹窗如需停表，可使用当前控制器的暂停状态；关闭时恢复它原来的状态。

## 提示与广告

初始提示 2 次、金币 0。优先消耗提示数量，其次消耗 100 金币；两者不足时展示激励广告。广告完整观看成功后才应用一次提示。优先填写选中的未完成格，否则选择一个未完成格；不会支付给无效目标。撤销不返还支付。

`SudokuHintWidget` 绑定已有 `Sign/m_num`、`m_coin`、`m_Ad/Loading`、`m_Ad/Ready`，加载失败时停止转圈并禁用广告使用，按参考流程有限重试。加载动画已有 Animation / Animator 时沿用，否则只旋转加载图标。

`AdSystem` / `RewardedAdFlow` 沿用参考项目的每广告位状态流、SDK 接口、加载超时、展示重试和回调版本保护。关闭页面取消等待，切题、重开、销毁重开后旧回调不能作用于新题。展示期间锁定交互、停止计时。

正式广告 ID 仅需替换 `Sudoku/Ad/AdIds.cs` 的 `Tip`。当前是 `adunit-sudoku-hint-placeholder`。EditorSDK 模拟完整观看；微信真机用占位 ID 不会得到真实可播放广告。

## 缓存

沿用参考工程的 `GameCacheManager → GameCacheData → IReadCacheFileSystem` 和 JsonUtility 格式。Unity / 桌面使用 `Application.persistentDataPath/game_cache.json`；微信 WebGL 使用 `WXBase.StorageGetStringSync` / `StorageSetStringSync`，键为 `gameCache`。

保存内容包括题目索引、填写内容、笔记、选中格、笔记开关、用时、各档不同题通关记录、提示数量、金币、主题、音量。答案不保存，恢复时从题包读取题面并求解一次。调试临时解锁不保存。题包 CRC 不匹配时不套用旧题面的填写状态或旧题号通关记录；非法存档退回初始状态。

填写 / 撤销 / 笔记开关、支付提示、通关、切题 / 重开、主题 / 设置变化、关闭窗口、切后台、退出时保存。每 30 秒保存一次用时。普通 Unity 文件 IO 异步且串行，合并中间待写快照；先写临时文件再替换，避免旧写覆盖新状态。退出回调不能等待下一帧，因此等待当前文件写入完成并同步提交最后快照。微信按参考项目使用同步 Storage，确保 onHide 返回前保存。

笔记和通关进度可以跨启动恢复；撤销栈只在本次题目会话内保留。

## 试玩与验证

使用 `Tools → 数独 → 开始试玩`。非 TE_RELEASE 版本按 F1 打开 GM，进度页有主题编号、提示数量和金币数量输入项，便于在弹窗制作前验证换色和道具状态。

核心测试：`dotnet run --no-restore --project Tools/SudokuTool -- test UnityProject/Assets/AssetRaw/Configs/Sudoku`。

发布编译检查：`Tests/Sudoku/Compile-Release.ps1`；添加 `-WeChat` 检查微信条件分支。实际 Unity 交互验证入口为 `SudokuEditor.SudokuStartupValidation.Run()`，报告和截图输出到 `Tests/Sudoku/Integration`，测试结束后恢复测试前的存档。

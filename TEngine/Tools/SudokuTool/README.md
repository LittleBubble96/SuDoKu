# 数独小游戏

当前项目：`E:/xx/Sudoku/TEngine/UnityProject`。源码、关卡包、字体和生成工具全部在本项目目录内，不依赖项目外的 Demo 文件夹。

目录位置：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Sudoku/`：数独核心和启动逻辑。
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/UI/Sudoku/`：正式 UISudoku 及局内 Widget。
- `UnityProject/Assets/Editor/Sudoku/`：关卡生成、试玩菜单和 Unity 验证工具。
- `UnityProject/Assets/AssetRaw/Configs/Sudoku/`：六档二进制关卡包。
- `UnityProject/Assets/AssetRaw/Fonts/FeiHuaSongTi-2 SDF.asset`、`UnityProject/Assets/AssetRaw/UI/Sudoku/`：字体和 UI 资源。
- `Tools/SudokuTool/`：离线生成及算法验证工具。
- `Tests/Sudoku/`：验证结果、截图和参考项目误改的撤回记录。

默认启动流程为 `GameApp → SudokuPresenter → 加载题包 → UISudoku`，继续沿用 TEngine 的 YooAsset 资源加载、UIWindow 生命周期、启动完成事件和 SDK 启动上报。字体由正式预制体引用，随预制体依赖加载。

Unity 打开当前项目后，等待编译完成，选择 **Tools → 数独 → 开始试玩**。菜单切换到本地编辑器资源模拟模式，并从现有 main 场景正常启动。停止运行后恢复原来的启动场景配置和资源模式。也可将资源模式设为 EditorMode 后使用原有 Launcher 按钮启动。

正式界面为 `UnityProject/Assets/AssetRaw/UI/Sudoku/UISudoku.prefab`。代码按现有节点路径使用 FindChildComponent 和 XYButton 绑定逻辑。OnCreate 从隐藏 SudoCell 生成 81 格，从唯一 Num1 复制到 Num9；沿用你配置的布局。网格线按实际棋盘尺寸动态定位。旧 UISudokuView 引用组件已移除。

不再提供 UI 自动搭建工具；界面由正式预制体编辑。接入方式见 [UI-Integration.md](UI-Integration.md)。

- 六档各 20 道独立生成题目，总计 120 道，六个文件共 5,016 字节。
- 只保存题面，进关时求解并确认唯一答案。
- 快速默认开放，其余难度在前一档完成两道不同题后解锁；重复通关同一道不增加计数。
- 存档沿用参考项目的 JSON、GameCacheManager 和平台文件系统结构；恢复填写、笔记、用时、通关记录、道具、主题与音量。Unity 使用本地文件，微信使用 Storage。
- 局内输入数字、擦除、笔记、撤销、提示、暂停；支持数字键、方向键、Backspace/Delete、N。GM 提供重开、上一题、下一题和切换难度。

## GM

非 `TE_RELEASE` 版本显示浮动 GM 按钮，也可按 F1 打开或关闭。GM 面板开启时暂停数独数字键输入，输入框中的数字不会写入棋盘。

按参考项目 UIGM 的生成方式实现：`CreateTopBtnAsync → CreateGmGroupAsync → CreateBtnAsync / CreateBtnAndInputAsync`。顶部页签、分组和条目通过 `CreateWidgetByPathAsync` 从以下项目内预制体实例化：

- `Assets/AssetRaw/UI/GM/UIGM.prefab`：面板、关闭按钮、页签区域、滚动列表和状态区域。
- `UIShowGM.prefab`：浮动入口。
- `TopBtn_GM.prefab`：页签。
- `GmGroup.prefab`：功能分组。
- `BtnAndInput_GM.prefab`：按钮及可选输入框。

具体命令集中在 `Assets/GameScripts/HotFix/GameLogic/UI/GM/UIGM.cs` 的 `GenerateGmItemsAsync()` 中。新增功能时在对应分组下注册按钮或带输入框的回调即可。

当前命令：跳转当前难度关卡（从 1 开始）、切换难度（0～5）、上一关、下一关、重开、填写一个正确数字、直接通关、临时解锁全部难度、重置进度、主题颜色、提示数量、金币数量、打印状态。

切换难度仍遵守解锁规则；可先用“临时解锁全部难度”调试。临时解锁不会伪造通关计数；重置进度会清除调试解锁，立即恢复快速档及初始状态。所有调试变化通过同一个控制器和 UI 事件同步到主界面。

定义 `TE_RELEASE` 后不编译 GM 入口、窗口、命令与 Session 调试接口。

## 关卡包格式 v1

所有多字节整数使用小端序。每个文件只有一个难度，关卡 ID 为文件中的零基序号。

| 偏移 | 长度 | 内容 |
| --- | --- | --- |
| 0 | 4 | ASCII `SDKP` |
| 4 | 1 | 格式版本，1 |
| 5 | 1 | 难度 0～5 |
| 6 | 1 | 每题长度，41 |
| 7 | 1 | 评级版本，1 |
| 8 | 4 | 题目数量，uint32 |
| 12 | 4 | 题面数据的 CRC32 |
| 16 | 41×数量 | 题面数据 |

每题按行排列 81 个 0～9 数字。高半字节存前一个格子、低半字节存后一个格子；最后字节低四位固定补 0。每题 41 字节。文件不包含答案、技巧记录或每题重复的难度、编号字段。

## 技巧评级 v1

快速：只用单格唯一候选，且已知数至少 60；Demo 生成器在 66 个已知数时开始收录。
初级：单格唯一候选和行列宫唯一位置。
中级：交叉排除、显性数对。
高级：隐性数对、显性/隐性三数组。
专家：X-Wing、XY-Wing。
大师：Swordfish、单数字强链双色排除（同色冲突、同时看见两种颜色）。

每次有进展后从最简单技巧重新开始。中级以上只有在低一档技巧无法解完、本档能解完时才收录。技巧库无法解完的题目舍弃，不自动归为大师。生成过程不在游戏启动时执行。

## 生成与验证

Unity：Tools → 数独 → 关卡工具，可设置每档题数、种子和尝试次数。取消生成或次数不足时保留原关卡包。

命令行（.NET 8，无第三方依赖）：

```powershell
dotnet run --project Tools/SudokuTool/SudokuTool.csproj -- test UnityProject/Assets/AssetRaw/Configs/Sudoku
dotnet run --project Tools/SudokuTool/SudokuTool.csproj -- generate UnityProject/Assets/AssetRaw/Configs/Sudoku Tools/SudokuTool/demo-ratings.json 20 20261022 100000
```

命令行工具开启额外技巧验证：每个填数、每次候选排除，都与唯一答案核对；验证代码不进入 Unity 客户端。每道题另用独立回溯实现交叉检查唯一解。

测试涵盖：非法/多解题、81 格编解码、CRC/截断/版本检查、题面不可修改、笔记、冲突、重开、不重复计数、逐档解锁、进度快照恢复、损坏/过期快照处理、撤销和提示，以及全部题目的分档正确性。

`demo-ratings.json` 是开发报告，不随客户端关卡包加载。桌面求解耗时仅用于开发验证，不代替微信小游戏设备性能测试。

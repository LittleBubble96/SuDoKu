using Cysharp.Threading.Tasks;
using GameLogic;
using GameSDK;
using TEngine;
#if ENABLE_OBFUZ
using Obfuz;
#endif

/// <summary>数独小游戏热更域入口：沿用框架初始化、资源加载、UI 和启动完成事件。</summary>
#if ENABLE_OBFUZ
[ObfuzIgnore(ObfuzScope.TypeName | ObfuzScope.MethodName)]
#endif
public partial class GameApp
{
    public static void Entrance(object[] objects)
    {
        Utility.Unity.AddDestroyListener(Release);
        StartGameLogic().Forget();
    }

    private static async UniTask StartGameLogic()
    {
        await GameLogic.Sudoku.SudokuPresenter.LaunchAsync();
        GameEvent.Send(EventDefine.GameStartSuccessEvent);
        SDK.ReportGameStart();
    }

    private static void Release()
    {
        GameLogic.Sudoku.SudokuPresenter.Release();
        SingletonSystem.Release();
        Log.Info("Release Sudoku GameApp");
    }
}


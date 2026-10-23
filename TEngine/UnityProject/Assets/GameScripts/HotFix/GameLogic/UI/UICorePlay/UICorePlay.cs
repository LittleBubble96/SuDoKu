
namespace GameLogic
{
    /// <summary>Framework-managed props HUD. Gameplay objects are loaded separately as MazeSceneRoot.</summary>
    [Window(UILayer.UI, location: "UICorePlay")]
    public sealed class UICorePlay : UIWindow
    {
        protected override void OnCreate()
        {
            base.OnCreate();
         
        }
        protected override void OnClose(){base.OnClose();}
    }
}

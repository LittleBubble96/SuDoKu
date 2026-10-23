#if !TE_RELEASE
using UnityEngine.UI;

namespace GameLogic
{
    [Window(UILayer.System, "UIShowGM")]
    public sealed class UIShowGM : UIWindow
    {
        protected override void OnCreate()
        {
            FindChildComponent<Button>("Panel/GM").onClick.AddListener(() => GMSingle.Instance.TogglePanel());
        }
    }
}
#endif

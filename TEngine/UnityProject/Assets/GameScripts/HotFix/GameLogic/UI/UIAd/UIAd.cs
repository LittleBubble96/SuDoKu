using TEngine;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.System, "UIAd")]
    public sealed class UIAd : UIWindow
    {
        private Transform _spinner;

        protected override void OnCreate()
        {
            base.OnCreate();
            _spinner = FindChildComponent<Transform>("Panel/root");
            // 手动旋转使用独立时间，避免暂停停止转圈或与原动画叠加。
            var animation = _spinner.GetComponent<Animation>();
            if (animation != null) animation.enabled = false;
        }
        protected override void OnRefresh()
        {
            base.OnRefresh();
            if (!AdSystem.HasOverlay) Close();
        }
        protected override void OnUpdate()
        {
            if (!AdSystem.HasOverlay) { Close(); return; }
            _spinner.Rotate(0, 0, -240f * Time.unscaledDeltaTime);
        }
    }
}

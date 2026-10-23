#if !TE_RELEASE
using Cysharp.Threading.Tasks;
using GameLogic.Sudoku;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    public sealed class GMSingle : MonoSingleton<GMSingle>
    {
        public SudokuGameController Game { get; private set; }
        public static bool IsPanelOpen { get; internal set; }
        private bool _opening;
        protected override bool DontDestroy => true;

        public async UniTask InitializeAsync(SudokuGameController game)
        {
            Game = game;
            if (await GameModule.UI.ShowUIAsyncAwait<UIShowGM>() == null) throw new System.InvalidOperationException("GM 入口预制体加载失败。");
        }
        public void TogglePanel() => ToggleAsync().Forget();
        private async UniTaskVoid ToggleAsync()
        {
            if (_opening || Game == null) return;
            if (IsPanelOpen) { GameModule.UI.CloseUI<UIGM>(); return; }
            _opening = true;
            try { await GameModule.UI.ShowUIAsyncAwait<UIGM>(); }
            finally { _opening = false; }
        }
        private void Update() { if (Input.GetKeyUp(KeyCode.F1)) TogglePanel(); }
        protected override void OnDestroy() { Game = null; IsPanelOpen = false; base.OnDestroy(); }
    }
}
#endif

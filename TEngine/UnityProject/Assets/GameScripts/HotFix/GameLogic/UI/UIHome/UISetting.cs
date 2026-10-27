using GameLogic.Sudoku;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI, "UISetting", fullScreen: true)]
    public sealed class UISetting : UIWindow
    {
        private UIHomeSettingWidget _page;
        private SudokuGameController _game;
        private bool _resumeGame;

        protected override void ScriptGenerator()
        {
            base.ScriptGenerator();
            UIThemeColor.Bind(gameObject.GetComponent<UnityEngine.UI.Image>(), SudokuThemeRole.Background);
            _page = CreateWidget<UIHomeSettingWidget>("Panel/Content/UIHome_Setting");
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            _game = SudokuPresenter.Current;
            _resumeGame = _game.IsWindowActive;
            _game.SetWindowActive(false);
            _page.RefreshDisplay();
        }

        protected override void OnClose()
        {
            _game.SetWindowActive(_resumeGame);
            GameEvent.Send(EventDefine.SudokuStateChanged);
            _game = null;
            base.OnClose();
        }
    }
}


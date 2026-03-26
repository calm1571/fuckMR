namespace Project.Core
{
        /// <summary>
    /// 主菜单状态。
    /// </summary>
    public sealed class MainMenuState : IAppState
    {
        private readonly MainMenuView _view;

        public MainMenuState(MainMenuView view)
        {
            _view = view;
        }

        public AppStateId Id => AppStateId.MainMenu;

        public void Enter()
        {
            _view.SetVisible(true);
        }

        public void Exit()
        {
            _view.SetVisible(false);
        }

        public void Tick()
        {
            _view.Tick();
        }
    }
}


namespace Project.Core
{
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

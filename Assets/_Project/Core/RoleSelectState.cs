namespace Project.Core
{
    public sealed class RoleSelectState : IAppState
    {
        private readonly RoleSelectView _view;

        public RoleSelectState(RoleSelectView view)
        {
            _view = view;
        }

        public AppStateId Id => AppStateId.RoleSelect;

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

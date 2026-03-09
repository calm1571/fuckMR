using System;

namespace Project.Core
{
    public sealed class LobbyClientState : IAppState
    {
        private readonly LobbyView _view;
        private readonly Action _onTick;

        public LobbyClientState(LobbyView view, Action onTick)
        {
            _view = view;
            _onTick = onTick;
        }

        public AppStateId Id => AppStateId.LobbyClient;

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
            _onTick?.Invoke();
        }
    }
}

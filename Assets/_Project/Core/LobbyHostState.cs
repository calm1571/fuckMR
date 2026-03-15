using System;

namespace Project.Core
{
    public sealed class LobbyHostState : IAppState
    {
        private readonly LobbyView _view;
        private readonly Action _onTick;

        public LobbyHostState(LobbyView view, Action onTick)
        {
            _view = view;
            _onTick = onTick;
        }

        public AppStateId Id => AppStateId.LobbyHost;

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

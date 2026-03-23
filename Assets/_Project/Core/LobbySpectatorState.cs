using System;

namespace Project.Core
{
        /// <summary>
    /// Spectator Lobby 状态。
    /// </summary>
    public sealed class LobbySpectatorState : IAppState
    {
        private readonly LobbyView _view;
        private readonly Action _onTick;

        public LobbySpectatorState(LobbyView view, Action onTick)
        {
            _view = view;
            _onTick = onTick;
        }

        public AppStateId Id => AppStateId.LobbySpectator;

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


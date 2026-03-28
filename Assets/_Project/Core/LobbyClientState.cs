// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Represents the client lobby state.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;

namespace Project.Core
{
        /// <summary>
    /// Client Lobby 状态。
    /// </summary>
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




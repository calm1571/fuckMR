// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Represents the host lobby state.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;

namespace Project.Core
{
        /// <summary>
    /// Host Lobby 状态。
    /// </summary>
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




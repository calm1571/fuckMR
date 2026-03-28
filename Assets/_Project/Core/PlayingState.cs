// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Represents the active gameplay state.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;

namespace Project.Core
{
        /// <summary>
    /// Playing state.
    /// </summary>
    public sealed class PlayingState : IAppState
    {
        private readonly Action _onEnter;
        private readonly Action _onExit;

        public PlayingState(Action onEnter, Action onExit)
        {
            _onEnter = onEnter;
            _onExit = onExit;
        }

        public AppStateId Id => AppStateId.Playing;

        public void Enter()
        {
            _onEnter?.Invoke();
        }

        public void Exit()
        {
            _onExit?.Invoke();
        }

        public void Tick()
        {
        }
    }
}





// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Coordinates state transitions for the application flow.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;
using System.Collections.Generic;

namespace Project.Core
{
        /// <summary>
    /// Minimal application state machine that owns transitions and Tick dispatch.
    /// </summary>
    public sealed class AppStateMachine
    {
        private readonly Dictionary<AppStateId, IAppState> _states = new Dictionary<AppStateId, IAppState>();
        private IAppState _current;

        public AppStateId CurrentId => _current != null ? _current.Id : AppStateId.Boot;

        public void Register(IAppState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            _states[state.Id] = state;
        }

        public void ChangeState(AppStateId next)
        {
            if (!_states.TryGetValue(next, out var target))
            {
                throw new InvalidOperationException("State not registered: " + next);
            }

            _current?.Exit();
            _current = target;
            _current.Enter();
        }

        public void Tick()
        {
            _current?.Tick();
        }
    }
}





// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Represents the role selection state.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

namespace Project.Core
{
        /// <summary>
    /// Role selection state.
    /// </summary>
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





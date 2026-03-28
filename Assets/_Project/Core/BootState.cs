// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Implements the application boot state.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.Core
{
        /// <summary>
    /// 应用启动状态。
    /// </summary>
    public sealed class BootState : IAppState
    {
        private readonly System.Action _onBootDone;

        public BootState(System.Action onBootDone)
        {
            _onBootDone = onBootDone;
        }

        public AppStateId Id => AppStateId.Boot;

        public void Enter()
        {
            _onBootDone?.Invoke();
        }

        public void Exit() { }

        public void Tick() { }
    }
}




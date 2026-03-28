// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines the interface contract for application states.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

namespace Project.Core
{
        /// <summary>
    /// 应用状态接口。
    /// </summary>
    public interface IAppState
    {
        AppStateId Id { get; }
        void Enter();
        void Exit();
        void Tick();
    }
}




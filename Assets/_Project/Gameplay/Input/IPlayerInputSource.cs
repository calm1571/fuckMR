// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Defines the input abstraction used by gameplay systems.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using System;

namespace Project.Gameplay.Input
{
        /// <summary>
    /// Player input abstraction interface.
    /// </summary>
    public interface IPlayerInputSource
    {
        event Action TriggerDown;
        event Action TriggerUp;
        event Action AButtonDown;
        event Action AButtonUp;

        bool IsDeviceReady { get; }

        void Tick();
    }
}





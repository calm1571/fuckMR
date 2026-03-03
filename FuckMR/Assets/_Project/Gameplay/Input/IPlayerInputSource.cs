using System;

namespace Project.Gameplay.Input
{
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

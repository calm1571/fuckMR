using System;

namespace Project.Gameplay.Input
{
        /// <summary>
    /// 玩家输入抽象接口。
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


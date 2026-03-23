using System;

namespace Project.Core
{
        /// <summary>
    /// 结果页状态。
    /// </summary>
    public sealed class ResultState : IAppState
    {
        private readonly Action _onEnter;
        private readonly Action _onExit;
        private readonly Action _onTick;

        public ResultState(Action onEnter, Action onExit, Action onTick)
        {
            _onEnter = onEnter;
            _onExit = onExit;
            _onTick = onTick;
        }

        public AppStateId Id => AppStateId.Result;

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
            _onTick?.Invoke();
        }
    }
}


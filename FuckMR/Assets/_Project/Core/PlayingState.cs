using System;

namespace Project.Core
{
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

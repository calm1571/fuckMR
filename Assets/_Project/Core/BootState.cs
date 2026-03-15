using UnityEngine;

namespace Project.Core
{
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

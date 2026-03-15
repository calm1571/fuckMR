namespace Project.Core
{
    public interface IAppState
    {
        AppStateId Id { get; }
        void Enter();
        void Exit();
        void Tick();
    }
}

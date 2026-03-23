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


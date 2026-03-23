namespace Project.Core
{
        /// <summary>
    /// 应用主流程状态枚举。
    /// </summary>
    public enum AppStateId
    {
        Boot = 0,
        MainMenu = 1,
        RoleSelect = 2,
        LobbyHost = 3,
        LobbyClient = 4,
        LobbySpectator = 5,
        Calibration = 6,
        Playing = 7,
        Result = 8,
        BackToMenu = 9
    }
}


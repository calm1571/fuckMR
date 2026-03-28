// Team-developed source file for the Enhancing Augmented Reality-Based Competitive Sports Experience, and Teaching How
// Authoring team: Team-GRP01
// Purpose: Provides debug visibility for gameplay input values.
// Third-party adaptation: No (see SOURCE_ATTRIBUTION.md)

using UnityEngine;

namespace Project.Gameplay.Input
{
        /// <summary>
    /// 输入调试探针，用于快速检查控制器输入状态。
    /// </summary>
    public sealed class M1InputDebugProbe
    {
        private readonly IPlayerInputSource _inputSource;

        public M1InputDebugProbe(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
            _inputSource.TriggerDown += OnTriggerDown;
            _inputSource.TriggerUp += OnTriggerUp;
            _inputSource.AButtonDown += OnAButtonDown;
            _inputSource.AButtonUp += OnAButtonUp;
        }

        public void Tick()
        {
            // Reserved for future HUD integration.
        }

        private static void OnTriggerDown()
        {
            Debug.Log("M1 Input: Trigger Down");
        }

        private static void OnTriggerUp()
        {
            Debug.Log("M1 Input: Trigger Up");
        }

        private static void OnAButtonDown()
        {
            Debug.Log("M1 Input: A Down");
        }

        private static void OnAButtonUp()
        {
            Debug.Log("M1 Input: A Up");
        }
    }
}




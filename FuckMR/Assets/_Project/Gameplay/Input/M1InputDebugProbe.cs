using UnityEngine;

namespace Project.Gameplay.Input
{
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

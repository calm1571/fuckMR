using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Gameplay.Input
{
    public sealed class PicoControllerInputSource : IPlayerInputSource
    {
        private readonly InputDeviceCharacteristics _characteristics;
        private UnityEngine.XR.InputDevice _device;

        private bool _triggerPressed;
        private bool _aPressed;
#if ENABLE_INPUT_SYSTEM
        private readonly ActionBasedController _actionController;
#endif

        public event Action TriggerDown;
        public event Action TriggerUp;
        public event Action AButtonDown;
        public event Action AButtonUp;

        public bool IsDeviceReady => _device.isValid;

        public PicoControllerInputSource(bool useRightController = true)
            : this(null, useRightController)
        {
        }

        public PicoControllerInputSource(ActionBasedController actionController, bool useRightController = true)
        {
            _characteristics = (useRightController ? InputDeviceCharacteristics.Right : InputDeviceCharacteristics.Left)
                | InputDeviceCharacteristics.Controller
                | InputDeviceCharacteristics.HeldInHand;
#if ENABLE_INPUT_SYSTEM
            _actionController = actionController;
#endif
        }

        public void Tick()
        {
            EnsureDevice();

            var trigger = ReadTriggerFromDevice();
            var aButton = ReadPrimaryButtonFromDevice();

            UpdateEdge(trigger, ref _triggerPressed, TriggerDown, TriggerUp);
            UpdateEdge(aButton, ref _aPressed, AButtonDown, AButtonUp);
        }

        private void EnsureDevice()
        {
            if (_device.isValid)
            {
                return;
            }

            var devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(_characteristics, devices);
            if (devices.Count > 0)
            {
                _device = devices[0];
                return;
            }

            _device = InputDevices.GetDeviceAtXRNode((_characteristics & InputDeviceCharacteristics.Right) != 0
                ? XRNode.RightHand
                : XRNode.LeftHand);
        }

        private static void UpdateEdge(bool current, ref bool previous, Action onDown, Action onUp)
        {
            if (current && !previous)
            {
                onDown?.Invoke();
            }
            else if (!current && previous)
            {
                onUp?.Invoke();
            }

            previous = current;
        }

        private bool ReadTriggerFromDevice()
        {
            var devicePressed = false;
            if (_device.isValid)
            {
                var triggerButton = _device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var triggerButtonValue) && triggerButtonValue;
                var triggerAxis = _device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out var triggerAxisValue) ? triggerAxisValue : 0f;
                devicePressed = triggerButton || triggerAxis > 0.25f;
            }

#if ENABLE_INPUT_SYSTEM
            if (TryReadActionButton(_actionController != null ? _actionController.activateAction.action : null, out var actionPressed))
            {
                return actionPressed || devicePressed;
            }
#endif

            return devicePressed;
        }

        private bool ReadPrimaryButtonFromDevice()
        {
            var devicePressed = _device.isValid &&
                _device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var primaryValue) &&
                primaryValue;

#if ENABLE_INPUT_SYSTEM
            if (TryReadActionButton(_actionController != null ? _actionController.uiPressAction.action : null, out var uiPress))
            {
                // UI Press is often trigger on XRI presets, so do not override primary button state.
                devicePressed = devicePressed || uiPress;
            }

            if (TryReadInputSystemButton("<XRController>{RightHand}/primaryButton", out var primaryButtonPressed))
            {
                return primaryButtonPressed || devicePressed;
            }
#endif

            return devicePressed;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryReadActionButton(InputAction action, out bool pressed)
        {
            pressed = false;
            if (action == null)
            {
                return false;
            }

            try
            {
                pressed = action.IsPressed();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadInputSystemButton(string path, out bool pressed)
        {
            pressed = false;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var control = InputSystem.FindControl(path);
            if (control == null)
            {
                return false;
            }

            pressed = control.IsPressed();
            return true;
        }
#endif
    }
}

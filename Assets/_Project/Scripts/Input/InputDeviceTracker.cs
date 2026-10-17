using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PassportFrenzy
{
    public enum ControlScheme { KeyboardMouse, Gamepad }

    /// <summary>Tracks the last device the player used so prompts and the residency app can adapt.</summary>
    public static class InputDeviceTracker
    {
        public static ControlScheme Current { get; private set; } = ControlScheme.KeyboardMouse;
        public static event Action<ControlScheme> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Current = ControlScheme.KeyboardMouse;
            Changed = null;
            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onActionChange += OnActionChange;
        }

        static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed && change != InputActionChange.ActionStarted) return;
            if (obj is not InputAction action || action.activeControl == null) return;
            var device = action.activeControl.device;
            if (device is Gamepad) Set(ControlScheme.Gamepad);
            else if (device is Keyboard || device is Mouse) Set(ControlScheme.KeyboardMouse);
        }

        public static void Set(ControlScheme scheme)
        {
            if (scheme == Current) return;
            Current = scheme;
            Changed?.Invoke(scheme);
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Agent/automation helper for driving Play mode via MCP eval. It injects input on virtual devices and holds it
    /// for a few player frames, so actions and the UI module see a real press (MCP simulate_key presses and
    /// releases inside one update, which UI Submit/WasPressedThisFrame never see).
    /// Examples (MCP eval):
    ///   PassportFrenzy.EditorTools.PlaytestDriver.TapKey(UnityEngine.InputSystem.Key.Space);
    ///   PassportFrenzy.EditorTools.PlaytestDriver.TapPad(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
    ///   PassportFrenzy.EditorTools.PlaytestDriver.FlickStick(new Vector2(1, 0));
    /// </summary>
    public static class PlaytestDriver
    {
        const string KeyboardName = "PlaytestKeyboard";
        const string GamepadName = "PlaytestGamepad";

        struct Release { public int frame; public System.Action action; }
        static readonly List<Release> Pending = new List<Release>();
        static bool _hooked;

        public static Keyboard VirtualKeyboard =>
            InputSystem.GetDevice(KeyboardName) as Keyboard ?? InputSystem.AddDevice<Keyboard>(KeyboardName);

        public static Gamepad VirtualGamepad =>
            InputSystem.GetDevice(GamepadName) as Gamepad ?? InputSystem.AddDevice<Gamepad>(GamepadName);

        public static string TapKey(Key key, int holdFrames = 3)
        {
            var kb = VirtualKeyboard;
            InputSystem.QueueStateEvent(kb, new KeyboardState(key));
            Later(holdFrames, () => InputSystem.QueueStateEvent(kb, new KeyboardState()));
            return $"tap {key} @frame {Time.frameCount}";
        }

        public static string TapPad(GamepadButton button, int holdFrames = 3)
        {
            var pad = VirtualGamepad;
            InputSystem.QueueStateEvent(pad, new GamepadState(button));
            Later(holdFrames, () => InputSystem.QueueStateEvent(pad, new GamepadState()));
            return $"pad {button} @frame {Time.frameCount}";
        }

        public static string FlickStick(Vector2 dir, int holdFrames = 4)
        {
            var pad = VirtualGamepad;
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = dir });
            Later(holdFrames, () => InputSystem.QueueStateEvent(pad, new GamepadState()));
            return $"flick {dir} @frame {Time.frameCount}";
        }

        /// <summary>Remove the virtual devices (call when done so they don't linger in the editor session).</summary>
        public static void RemoveDevices()
        {
            if (InputSystem.GetDevice(KeyboardName) is { } k) InputSystem.RemoveDevice(k);
            if (InputSystem.GetDevice(GamepadName) is { } g) InputSystem.RemoveDevice(g);
        }

        static void Later(int frames, System.Action a)
        {
            Pending.Add(new Release { frame = Time.frameCount + Mathf.Max(1, frames), action = a });
            if (_hooked) return;
            _hooked = true;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                if (!Application.isPlaying || Time.frameCount >= Pending[i].frame)
                {
                    Pending[i].action();
                    Pending.RemoveAt(i);
                }
            }
        }
    }
}

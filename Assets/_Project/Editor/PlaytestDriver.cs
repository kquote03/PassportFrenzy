using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Agent/automation helper for driving Play mode via MCP eval. It injects input on virtual devices from inside the
    /// game's own (Dynamic) input update and holds it for a few player frames, so actions and the UI module see real
    /// presses. (MCP simulate_key / eval-queued events get processed in the editor update, which gameplay never sees.)
    /// Examples (MCP eval):
    ///   PassportFrenzy.EditorTools.PlaytestDriver.TapKey(UnityEngine.InputSystem.Key.Space);
    ///   PassportFrenzy.EditorTools.PlaytestDriver.TapPad(UnityEngine.InputSystem.LowLevel.GamepadButton.South);
    ///   PassportFrenzy.EditorTools.PlaytestDriver.FlickStick(new UnityEngine.Vector2(1, 0));
    /// </summary>
    public static class PlaytestDriver
    {
        const string KeyboardName = "PlaytestKeyboard";
        const string GamepadName = "PlaytestGamepad";

        class Step { public int delayFrames; public System.Action action; }
        static readonly Queue<Step> Steps = new Queue<Step>();
        static int _waitUntilFrame = -1;
        static bool _hooked;

        public static Keyboard VirtualKeyboard =>
            InputSystem.GetDevice(KeyboardName) as Keyboard ?? InputSystem.AddDevice<Keyboard>(KeyboardName);

        public static Gamepad VirtualGamepad =>
            InputSystem.GetDevice(GamepadName) as Gamepad ?? InputSystem.AddDevice<Gamepad>(GamepadName);

        public static int PendingSteps => Steps.Count;

        /// <summary>Optional: also route real devices to the game while the editor is unfocused.</summary>
        public static string Prepare()
        {
            var s = InputSystem.settings;
            s.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            s.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Hook();
            // Deterministic game time: each frame advances a fixed step, so slow (unfocused) editor frame rates
            // don't make real-time windows expire before injected input lands. Reset with Time.captureDeltaTime = 0.
            Time.captureDeltaTime = 1f / 30f;
            return $"input prepared ({s.backgroundBehavior}, {s.editorInputBehaviorInPlayMode}), captureDeltaTime=1/30";
        }

        public static string TapKey(Key key, int holdFrames = 3)
        {
            var kb = VirtualKeyboard;
            Enqueue(0, () => InputSystem.QueueStateEvent(kb, new KeyboardState(key)));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(kb, new KeyboardState()));
            return $"queued tap {key}";
        }

        /// <summary>Tap a key N times (e.g. mashing).</summary>
        public static string MashKey(Key key, int times, int holdFrames = 2)
        {
            for (int i = 0; i < times; i++) TapKey(key, holdFrames);
            return $"queued {times}x {key}";
        }

        public static string TapPad(GamepadButton button, int holdFrames = 3)
        {
            var pad = VirtualGamepad;
            Enqueue(0, () => InputSystem.QueueStateEvent(pad, new GamepadState(button)));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(pad, new GamepadState()));
            return $"queued pad {button}";
        }

        public static string FlickStick(Vector2 dir, int holdFrames = 4)
        {
            var pad = VirtualGamepad;
            Enqueue(0, () => InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = dir }));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(pad, new GamepadState()));
            return $"queued flick {dir}";
        }

        /// <summary>Remove the virtual devices (call when done so they don't linger).</summary>
        public static void RemoveDevices()
        {
            Steps.Clear();
            if (InputSystem.GetDevice(KeyboardName) is { } k) InputSystem.RemoveDevice(k);
            if (InputSystem.GetDevice(GamepadName) is { } g) InputSystem.RemoveDevice(g);
        }

        static void Enqueue(int delayFrames, System.Action a)
        {
            Hook();
            Steps.Enqueue(new Step { delayFrames = delayFrames, action = a });
        }

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            InputSystem.onBeforeUpdate += OnBeforeUpdate;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s != PlayModeStateChange.ExitingPlayMode) return;
                Steps.Clear();
                Time.captureDeltaTime = 0f;
            };
        }

        // Runs inside the input update; we only act on the game's Dynamic updates so events land in gameplay frames.
        static void OnBeforeUpdate()
        {
            if (!Application.isPlaying || Steps.Count == 0) return;
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (_waitUntilFrame < 0) _waitUntilFrame = Time.frameCount + Steps.Peek().delayFrames;
            if (Time.frameCount < _waitUntilFrame) return;
            Steps.Dequeue().action();
            _waitUntilFrame = -1;
        }
    }
}

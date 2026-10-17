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

        // Held input (e.g. "keep walking right" in free-movement mode) is merged into every injected state.
        static readonly HashSet<Key> HeldKeys = new HashSet<Key>();
        static Vector2 _heldStick;

        static KeyboardState Kb(params Key[] extra)
        {
            var keys = new List<Key>(HeldKeys);
            keys.AddRange(extra);
            return new KeyboardState(keys.ToArray());
        }

        static GamepadState Pad(GamepadButton? button = null, Vector2? stick = null)
        {
            var s = button.HasValue ? new GamepadState(button.Value) : new GamepadState();
            s.leftStick = stick ?? _heldStick;
            return s;
        }

        /// <summary>Hold or release a key until changed (merged with taps).</summary>
        public static string SetHeldKey(Key key, bool held)
        {
            var kb = VirtualKeyboard;
            Enqueue(0, () =>
            {
                if (held) HeldKeys.Add(key); else HeldKeys.Remove(key);
                InputSystem.QueueStateEvent(kb, Kb());
            });
            return $"held {key}={held}";
        }

        /// <summary>Hold the left stick at a direction until changed (Vector2.zero releases).</summary>
        public static string SetHeldStick(Vector2 dir)
        {
            var pad = VirtualGamepad;
            Enqueue(0, () =>
            {
                _heldStick = dir;
                InputSystem.QueueStateEvent(pad, Pad());
            });
            return $"held stick {dir}";
        }

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
            Enqueue(0, () => InputSystem.QueueStateEvent(kb, Kb(key)));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(kb, Kb()));
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
            Enqueue(0, () => InputSystem.QueueStateEvent(pad, Pad(button)));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(pad, Pad()));
            return $"queued pad {button}";
        }

        public static string FlickStick(Vector2 dir, int holdFrames = 4)
        {
            var pad = VirtualGamepad;
            Enqueue(0, () => InputSystem.QueueStateEvent(pad, Pad(stick: dir)));
            Enqueue(holdFrames, () => InputSystem.QueueStateEvent(pad, Pad()));
            return $"queued flick {dir}";
        }

        public static Mouse VirtualMouse =>
            InputSystem.GetDevice(MouseName) as Mouse ?? InputSystem.AddDevice<Mouse>(MouseName);

        const string MouseName = "PlaytestMouse";

        /// <summary>Left-drag from one screen point to another over several frames (uGUI drag and drop).</summary>
        public static string DragMouse(Vector2 from, Vector2 to, int moveFrames = 8)
        {
            var mouse = VirtualMouse;
            Enqueue(0, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = from }));
            Enqueue(1, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = from }.WithButton(MouseButton.Left)));
            for (int i = 1; i <= moveFrames; i++)
            {
                var p = Vector2.Lerp(from, to, i / (float)moveFrames);
                Enqueue(1, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left)));
            }
            Enqueue(1, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = to }));
            return $"queued drag {from} -> {to}";
        }

        /// <summary>Left-click at a screen point.</summary>
        public static string ClickMouse(Vector2 at)
        {
            var mouse = VirtualMouse;
            Enqueue(0, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = at }));
            Enqueue(1, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = at }.WithButton(MouseButton.Left)));
            Enqueue(2, () => InputSystem.QueueStateEvent(mouse, new MouseState { position = at }));
            return $"queued click {at}";
        }

        /// <summary>Remove the virtual devices (call when done so they don't linger).</summary>
        public static void RemoveDevices()
        {
            Steps.Clear();
            HeldKeys.Clear();
            _heldStick = Vector2.zero;
            if (InputSystem.GetDevice(KeyboardName) is { } k) InputSystem.RemoveDevice(k);
            if (InputSystem.GetDevice(GamepadName) is { } g) InputSystem.RemoveDevice(g);
            if (InputSystem.GetDevice(MouseName) is { } m) InputSystem.RemoveDevice(m);
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
                HeldKeys.Clear();
                _heldStick = Vector2.zero;
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

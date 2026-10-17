using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PassportFrenzy.EditorTools
{
    /// <summary>
    /// Editor-only bot that plays the game through PlaytestDriver input (keyboard or gamepad), for MCP-driven
    /// end-to-end checks. It reacts to GameFlow state; the console gets "[Flow]", "[Residency]" and "[Autopilot]" lines.
    /// Example (MCP eval, in Play mode):
    ///   PassportFrenzy.EditorTools.PlaytestAutopilot.Run(gamepad: true, bookingIndex: 1, mash: true, photos: 3, shopIndex: 0);
    ///   PassportFrenzy.EditorTools.PlaytestAutopilot.Stop();
    /// bookingIndex -1 = let the booking timer expire. shopIndex -1 = press Skip, -2 = let the shop time out.
    /// </summary>
    public static class PlaytestAutopilot
    {
        public static bool Gamepad;
        public static int BookingIndex;
        public static bool Mash = true;
        public static int Photos = 3;
        public static int ShopIndex = -1;
        public static bool AutoJump = true;
        public static bool AutoPhoto = true;
        public static bool UseItemInChase = true;
        public static bool PressPlayOnMenu = true;
        /// <summary>Attach residency photos by mouse drag-and-drop instead of stick/arrow.</summary>
        public static bool MouseDrag;

        static bool _on, _hooked;
        static string _handledKey;
        static float _nextJumpTime;

        public static string Run(bool gamepad = false, int bookingIndex = 0, bool mash = true, int photos = 3, int shopIndex = -1,
            bool autoJump = true, bool autoPhoto = true, bool useItemInChase = true, bool pressPlayOnMenu = true)
        {
            Gamepad = gamepad;
            BookingIndex = bookingIndex;
            Mash = mash;
            Photos = photos;
            ShopIndex = shopIndex;
            AutoJump = autoJump;
            AutoPhoto = autoPhoto;
            UseItemInChase = useItemInChase;
            PressPlayOnMenu = pressPlayOnMenu;
            PlaytestDriver.Prepare();
            _handledKey = null;
            _on = true;
            if (!_hooked)
            {
                _hooked = true;
                EditorApplication.update += Tick;
                EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.ExitingPlayMode) _on = false; };
            }
            return $"[Autopilot] on: pad={gamepad} booking={bookingIndex} mash={mash} photos={photos} shop={shopIndex}";
        }

        public static void Stop() => _on = false;

        static void Log(string msg) => Debug.Log("[Autopilot] " + msg);

        // ---- input helpers (device-aware)
        static void Confirm() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.South); else PlaytestDriver.TapKey(Key.Enter); }
        static void Down() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.DpadDown); else PlaytestDriver.TapKey(Key.DownArrow); }
        static void Jump() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.South); else PlaytestDriver.TapKey(Key.Space); }
        static void Photo() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.West); else PlaytestDriver.TapKey(Key.F); }
        static void UseItem() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.North); else PlaytestDriver.TapKey(Key.E); }
        static void MashOnce() { if (Gamepad) PlaytestDriver.TapPad(GamepadButton.South, 2); else PlaytestDriver.TapKey(Key.Space, 2); }
        static void AttachOnce() { if (Gamepad) PlaytestDriver.FlickStick(Vector2.right, 3); else PlaytestDriver.TapKey(Key.RightArrow, 3); }

        static bool Once(string key)
        {
            if (_handledKey == key) return false;
            _handledKey = key;
            return true;
        }

        static void Tick()
        {
            if (!_on || !Application.isPlaying || GameRoot.I == null) return;
            if (PlaytestDriver.PendingSteps > 0) return; // let queued input play out first

            var root = GameRoot.I;
            var flow = root.flow;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            switch (flow.State)
            {
                case GameState.Home:
                    if (PressPlayOnMenu && selected != null && Once("home"))
                    {
                        Log("press Play");
                        Confirm();
                    }
                    break;

                case GameState.Booking:
                    if (BookingIndex >= 0 && selected != null && Once("booking" + flow.Run.RunSeconds.ToString("0")))
                    {
                        Log($"book card {BookingIndex}");
                        for (int i = 0; i < BookingIndex; i++) Down();
                        Confirm();
                    }
                    break;

                case GameState.Residency:
                {
                    var app = root.ui.phone.residency;
                    if (app.CurrentPhase == ResidencyApp.Phase.Mash && Mash && Once("mash"))
                    {
                        Log("mash x30");
                        for (int i = 0; i < 30; i++) MashOnce();
                    }
                    else if (app.CurrentPhase == ResidencyApp.Phase.Photos && MouseDrag && Once("photos"))
                    {
                        // Drag each tray card onto the paper (screen-space overlay canvas: world pos == screen pos).
                        var paper = (RectTransform)app.transform.Find("PhotoGroup/Paper");
                        var cards = app.GetComponentsInChildren<PhotoDragItem>();
                        int n = Mathf.Min(Photos, cards.Length);
                        Log($"drag {n} photo(s) onto the paper with the mouse");
                        // The tray re-flows after each attach, so the next card always sits in the top slot.
                        Vector2 top = cards.Length > 0 ? (Vector2)cards[0].transform.position : Vector2.zero;
                        for (int i = 0; i < n; i++)
                            PlaytestDriver.DragMouse(top, paper.TransformPoint(paper.rect.center)); // paper pivot is top-right
                    }
                    else if (app.CurrentPhase == ResidencyApp.Phase.Photos && Once("photos"))
                    {
                        Log($"attach {Photos} photo(s) via {(Gamepad ? "stick flick" : "right arrow")}");
                        for (int i = 0; i < Photos; i++) AttachOnce();
                    }
                    break;
                }

                case GameState.Shop:
                    if (ShopIndex == -2 || selected == null || !Once("shop" + flow.Run.RunSeconds.ToString("0"))) break;
                    int steps = ShopIndex >= 0 ? ShopIndex : 3; // 3 items, then Skip
                    Log(ShopIndex >= 0 ? $"buy item {ShopIndex}" : "skip shop");
                    for (int i = 0; i < steps; i++) Down();
                    Confirm();
                    break;

                case GameState.Departing:
                case GameState.CountryRun:
                case GameState.ToAirport:
                case GameState.Chase:
                    _handledKey = null;
                    var player = root.player;
                    if (AutoJump && player.Blocked && Time.realtimeSinceStartup > _nextJumpTime)
                    {
                        _nextJumpTime = Time.realtimeSinceStartup + 0.3f;
                        Jump();
                    }
                    var zone = player.GetComponent<PlayerInteractions>().CurrentZone;
                    if (AutoPhoto && zone != null && !zone.Photographed)
                    {
                        Log("photo " + zone.FestivalName);
                        Photo();
                    }
                    if (UseItemInChase && flow.State == GameState.Chase && root.buffs.Held != null)
                    {
                        Log("use " + root.buffs.Held.displayName);
                        UseItem();
                    }
                    break;
            }
        }
    }
}

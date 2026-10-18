using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// First-run control hints for the HUD hint line. One hint at a time, each until the player has done it once this
    /// session: pick on the phone, jump over an obstacle or crowd, hold right to keep running. Turn off with
    /// GameTuning.showHints. <see cref="Tick"/> returns the text to show (device tokens unformatted), or null.
    /// </summary>
    public class ControlHints
    {
        const string PhoneHint = "{PhoneNav} Move up / down     {Submit} Select";
        const string JumpHint = "{Jump} Jump over obstacles and crowds";
        const string RunHint = "Hold {Right} to keep running";

        static bool _phoneDone, _jumpDone, _runDone;

        // Project has "Enter Play Mode: no domain reload", so the session flags are cleared by hand.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => _phoneDone = _jumpDone = _runDone = false;

        readonly List<Collider2D> _hits = new List<Collider2D>();
        readonly ContactFilter2D _triggers = new ContactFilter2D { useTriggers = true };
        float _stillSeconds, _runSeconds;
        bool _jumpShowing;

        /// <summary>A jump the player pressed while the jump hint was up (or any crowd bounce) completes it.</summary>
        public void OnJumped(bool manual)
        {
            if (manual && _jumpShowing) _jumpDone = true;
        }

        public void OnCrowdBounced() => _jumpDone = true;

        public string Tick(float dt)
        {
            var root = GameRoot.I;
            if (root == null || !root.tuning.showHints) return null;
            var run = root.flow.Run;
            var state = root.flow.State;
            var input = root.input;
            var player = root.player;
            _jumpShowing = false;
            if (run == null) return null;

            // 1. Phone: while choosing a flight or a shop item.
            if (input.PhoneMode && state is GameState.Booking or GameState.Shop)
            {
                if (input.PhoneSubmit.WasPressedThisFrame()) _phoneDone = true;
                return _phoneDone ? null : PhoneHint;
            }

            bool inControl = !input.MenuMode && !input.PhoneMode && !player.AutoMoving &&
                             state is GameState.Departing or GameState.Chase or GameState.CountryRun or GameState.ToAirport;
            if (!inControl)
            {
                _stillSeconds = 0f;
                return null;
            }

            if (input.Move.ReadValue<Vector2>().x > 0.5f)
            {
                _runSeconds += dt;
                if (_runSeconds >= 1f) _runDone = true;
            }

            // 2. Jump: something to jump over just ahead.
            if (!_jumpDone && player.Grounded && ObstacleAhead(player.transform.position))
            {
                _jumpShowing = true;
                return JumpHint;
            }

            // 3. Run: standing still with the controls in hand.
            if (_runDone) return null;
            float speed01 = player.CurrentSpeed / Mathf.Max(0.01f, root.tuning.baseRunSpeed);
            _stillSeconds = speed01 < 0.15f ? _stillSeconds + dt : 0f;
            return _stillSeconds >= 0.6f ? RunHint : null;
        }

        /// <summary>An obstacle not yet hit, or a crowd, 1-8 m in front of the player's feet.</summary>
        bool ObstacleAhead(Vector2 feet)
        {
            int n = Physics2D.OverlapBox(new Vector2(feet.x + 4.5f, feet.y + 1f), new Vector2(7f, 2f), 0f, _triggers, _hits);
            for (int i = 0; i < n; i++)
            {
                var obstacle = _hits[i].GetComponent<ObstacleBase>();
                if ((obstacle != null && !obstacle.Used) || _hits[i].GetComponent<CrowdZone>() != null) return true;
            }
            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Side-scrolling runner. Move input (A/D, arrows, stick, D-pad) drives a speed built from multipliers
    /// (stumble, crowd, boost, block), plus jump. GameTuning.autoRun = legacy auto-runner. Values come from GameTuning.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RunnerController : MonoBehaviour
    {
        [Tooltip("Optional child shown while the shield is up.")]
        [SerializeField] GameObject shieldVisual;
        [Tooltip("Child that gets squashed/flashed for feedback (the 'Visual' child).")]
        [SerializeField] Transform visual;
        [Tooltip("Placeholder run bob/lean on the Visual. Turn off once the artist adds a real run animation.")]
        [SerializeField] bool proceduralAnimation = true;
        [SerializeField] Color boostTint = new Color(0.4f, 1f, 1f);

        Rigidbody2D _rb;
        ContactFilter2D _groundFilter;
        readonly List<CrowdZone> _crowds = new List<CrowdZone>();
        readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

        float _stumbleTimer, _stumbleDuration = 1f, _stumbleMultiplier = 1f;
        float _boostTimer, _boostMultiplier = 1f;
        float _invulnTimer, _coyoteTimer, _jumpBufferTimer;
        bool _autoHop;
        float _flashTimer, _runPhase;
        float _moveX, _autoMoveX, _facing = 1f, _furthestX, _leftLimit;
        Vector3 _visualBasePos, _visualBaseScale = Vector3.one;
        SpriteRenderer _visualRenderer;
        Color _visualBaseColor = Color.white;

        public bool Running { get; private set; }
        public bool Blocked { get; private set; }
        public bool Grounded { get; private set; }
        public bool HasShield { get; private set; }
        public float BoostSecondsRemaining => _boostTimer;
        public float CurrentSpeed { get; private set; }
        public float VerticalSpeed => _rb != null ? _rb.linearVelocity.y : 0f;
        public bool InCrowd => _crowds.Count > 0;

        GameTuning T => GameRoot.I.tuning;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _groundFilter = new ContactFilter2D { useTriggers = false, useNormalAngle = true, minNormalAngle = 60f, maxNormalAngle = 120f };
            if (visual != null)
            {
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
                _visualBasePos = visual.localPosition;
                _visualBaseScale = visual.localScale;
            }
            if (_visualRenderer != null) _visualBaseColor = _visualRenderer.color;
            if (shieldVisual != null) shieldVisual.SetActive(false);
        }

        void Start() => _rb.gravityScale = T.gravityScale;

        public void SetRunning(bool running)
        {
            Running = running;
            if (!running) Blocked = false;
        }

        /// <summary>Teleport and clear transient state (used on landing / returning home).</summary>
        public void ResetTo(Vector2 position)
        {
            _rb.position = position;
            transform.position = position;
            _rb.linearVelocity = Vector2.zero;
            _furthestX = position.x;
            _leftLimit = position.x - 8f; // chunks start ~10 m behind the spawn point
            _facing = 1f;
            _crowds.Clear();
            Blocked = false;
            _stumbleTimer = 0f;
            _boostTimer = 0f;
            _invulnTimer = 0f;
            _jumpBufferTimer = 0f;
            _autoMoveX = 0f;
            _autoHop = false;
        }

        /// <summary>Scripted move input (-1..1) that drives the runner while player input is locked (the intro, the phone).
        /// <paramref name="hopWhenBlocked"/> jumps automatically when a block obstacle stops the runner (the phone has the
        /// jump button). 0 hands movement back to the player.</summary>
        public void SetAutoMove(float x, bool hopWhenBlocked = false)
        {
            _autoMoveX = Mathf.Clamp(x, -1f, 1f);
            _autoHop = hopWhenBlocked && _autoMoveX != 0f;
        }

        public void ClearRunBuffs()
        {
            SetShield(false);
            _boostTimer = 0f;
        }

        void Update()
        {
            var input = GameRoot.I.input;
            if (input.Jump.WasPressedThisFrame()) _jumpBufferTimer = T.jumpBufferTime;
            if (_autoHop && Blocked && Grounded) _jumpBufferTimer = T.jumpBufferTime;
            _moveX = _autoMoveX != 0f ? _autoMoveX : Running && !input.MenuMode ? input.Move.ReadValue<Vector2>().x : 0f;
            if (Mathf.Abs(_moveX) < 0.2f) _moveX = 0f;

            float dt = Time.deltaTime;
            _jumpBufferTimer -= dt;
            _coyoteTimer -= dt;
            _invulnTimer -= dt;
            if (_stumbleTimer > 0f) _stumbleTimer -= dt;
            if (_boostTimer > 0f) _boostTimer -= dt;

            if (_jumpBufferTimer > 0f && (Grounded || _coyoteTimer > 0f) && Running && !input.MenuMode)
                DoJump();

            UpdateFeedback(dt);
        }

        void FixedUpdate()
        {
            Grounded = _rb.IsTouching(_groundFilter);
            if (Grounded) _coyoteTimer = T.coyoteTime;

            if (T.autoRun)
            {
                CurrentSpeed = Blocked ? 0f : ComputeSpeed();
                _rb.linearVelocity = new Vector2(CurrentSpeed, _rb.linearVelocity.y);
                return;
            }

            // Free movement (experiment): Move input drives speed; obstacles/crowds/boost still scale it.
            float target = _moveX * ComputeSpeed();
            if (Blocked && target > 0f) target = 0f;           // walls only stop forward motion; back off, then jump
            float vx = Mathf.MoveTowards(_rb.linearVelocity.x, target, T.moveAcceleration * Time.fixedDeltaTime);

            float x = _rb.position.x;
            _furthestX = Mathf.Max(_furthestX, x);
            _leftLimit = Mathf.Max(_leftLimit, _furthestX - T.maxBacktrackDistance);
            if (x <= _leftLimit && vx < 0f) vx = 0f;           // can't walk back into despawned chunks

            if (Mathf.Abs(vx) > 0.1f) _facing = Mathf.Sign(vx);
            CurrentSpeed = Mathf.Abs(vx);
            _rb.linearVelocity = new Vector2(vx, _rb.linearVelocity.y);
        }

        float ComputeSpeed()
        {
            if (!Running) return 0f;
            float speed = T.baseRunSpeed;
            if (_stumbleTimer > 0f)
            {
                float t = 1f - _stumbleTimer / _stumbleDuration; // 0 just hit -> 1 recovered
                speed *= Mathf.Lerp(_stumbleMultiplier, 1f, t * t);
            }
            if (_crowds.Count > 0)
            {
                float m = 1f;
                foreach (var c in _crowds) if (c != null) m = Mathf.Min(m, c.Multiplier);
                speed *= m;
            }
            if (_boostTimer > 0f) speed *= _boostMultiplier;
            return speed;
        }

        void DoJump()
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            Blocked = false;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, T.jumpVelocity);
            GameRoot.I.audioManager.Play(SoundIds.Jump);
            if (visual != null) visual.localScale = Vector3.Scale(FacingScale, new Vector3(0.8f, 1.2f, 1f));
        }

        /// <summary>Obstacle hit. Returns true if the hit was handled (applied or absorbed by shield).</summary>
        public bool TryHit(HitKind kind, float stumbleMultiplier = -1f, float stumbleSeconds = -1f)
        {
            if (_invulnTimer > 0f) return false;
            _invulnTimer = T.hitInvulnerabilitySeconds;

            if (HasShield)
            {
                SetShield(false);
                GameRoot.I.audioManager.Play(SoundIds.ShieldBreak);
                GameEvents.RaiseToast("Shield absorbed the hit!");
                GameEvents.RaisePlayerHit(kind, true);
                return true;
            }

            if (kind == HitKind.Slow)
            {
                _stumbleMultiplier = stumbleMultiplier >= 0f ? stumbleMultiplier : T.stumbleSpeedMultiplier;
                _stumbleDuration = stumbleSeconds > 0f ? stumbleSeconds : T.stumbleRecoverSeconds;
                _stumbleTimer = _stumbleDuration;
                GameRoot.I.audioManager.Play(SoundIds.Hit);
            }
            else
            {
                Blocked = true;
                GameRoot.I.audioManager.Play(SoundIds.Block);
            }
            _flashTimer = 0.25f;
            GameEvents.RaisePlayerHit(kind, false);
            if (GameRoot.I.Cam != null) GameRoot.I.Cam.Shake(kind == HitKind.Block ? 0.25f : 0.15f);
            return true;
        }

        public void ClearBlocked() => Blocked = false;

        public void EnterCrowd(CrowdZone zone) { if (!_crowds.Contains(zone)) _crowds.Add(zone); }
        public void ExitCrowd(CrowdZone zone) => _crowds.Remove(zone);

        public void SetShield(bool on)
        {
            HasShield = on;
            if (shieldVisual != null) shieldVisual.SetActive(on);
            GameEvents.RaiseItemChanged();
        }

        public void StartBoost(float seconds, float multiplier)
        {
            _boostTimer = seconds;
            _boostMultiplier = multiplier;
        }

        Vector3 FacingScale => new Vector3(Mathf.Abs(_visualBaseScale.x) * _facing, _visualBaseScale.y, _visualBaseScale.z);

        void UpdateFeedback(float dt)
        {
            if (visual != null)
            {
                if (Mathf.Sign(visual.localScale.x) != Mathf.Sign(FacingScale.x)) visual.localScale = Vector3.Scale(visual.localScale, new Vector3(-1f, 1f, 1f));
                visual.localScale = Vector3.Lerp(visual.localScale, FacingScale, 10f * dt);
                if (proceduralAnimation)
                {
                    // Run bob + forward lean scaled by speed; leans back a little in the air.
                    float speed01 = Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.01f, T.baseRunSpeed));
                    _runPhase += dt * (6f + 8f * speed01);
                    float bob = Grounded ? Mathf.Abs(Mathf.Sin(_runPhase)) * 0.08f * speed01 : 0f;
                    float lean = (Grounded ? -8f * speed01 : 6f) * _facing;
                    visual.localPosition = _visualBasePos + new Vector3(0f, bob, 0f);
                    visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, lean), 12f * dt);
                }
            }
            if (_visualRenderer != null)
            {
                _flashTimer -= dt;
                Color c = _visualBaseColor;
                if (_boostTimer > 0f) c = Color.Lerp(_visualBaseColor, boostTint, 0.5f + 0.5f * Mathf.Sin(Time.time * 20f));
                _visualRenderer.color = _flashTimer > 0f && Mathf.Repeat(_flashTimer, 0.1f) > 0.05f ? Color.red : c;
            }
        }

        // Clear stale crowd overlaps if a crowd is destroyed while we're inside it.
        void LateUpdate() => _crowds.RemoveAll(c => c == null || !c.isActiveAndEnabled);
    }
}

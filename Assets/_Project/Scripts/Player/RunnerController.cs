using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Auto-runner. Moves right at a speed built from multipliers (stumble, crowd, boost, block).
    /// Jump is the only movement input. Values come from GameTuning.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class RunnerController : MonoBehaviour
    {
        [Tooltip("Optional child shown while the shield is up.")]
        [SerializeField] GameObject shieldVisual;
        [Tooltip("Child that gets squashed/flashed for feedback (the 'Visual' child).")]
        [SerializeField] Transform visual;

        Rigidbody2D _rb;
        ContactFilter2D _groundFilter;
        readonly List<CrowdZone> _crowds = new List<CrowdZone>();
        readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

        float _stumbleTimer, _stumbleDuration = 1f, _stumbleMultiplier = 1f;
        float _boostTimer, _boostMultiplier = 1f;
        float _invulnTimer, _coyoteTimer, _jumpBufferTimer;
        float _flashTimer;
        SpriteRenderer _visualRenderer;
        Color _visualBaseColor = Color.white;

        public bool Running { get; private set; }
        public bool Blocked { get; private set; }
        public bool Grounded { get; private set; }
        public bool HasShield { get; private set; }
        public float BoostSecondsRemaining => _boostTimer;
        public float CurrentSpeed { get; private set; }
        public bool InCrowd => _crowds.Count > 0;

        GameTuning T => GameRoot.I.tuning;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _groundFilter = new ContactFilter2D { useTriggers = false, useNormalAngle = true, minNormalAngle = 60f, maxNormalAngle = 120f };
            if (visual != null) _visualRenderer = visual.GetComponent<SpriteRenderer>();
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
            _crowds.Clear();
            Blocked = false;
            _stumbleTimer = 0f;
            _boostTimer = 0f;
            _invulnTimer = 0f;
            _jumpBufferTimer = 0f;
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

            CurrentSpeed = ComputeSpeed();
            _rb.linearVelocity = new Vector2(CurrentSpeed, _rb.linearVelocity.y);
        }

        float ComputeSpeed()
        {
            if (!Running || Blocked) return 0f;
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
            if (visual != null) visual.localScale = new Vector3(0.8f, 1.2f, 1f);
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

        void UpdateFeedback(float dt)
        {
            if (visual != null)
                visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, 10f * dt);
            if (_visualRenderer != null)
            {
                _flashTimer -= dt;
                _visualRenderer.color = _flashTimer > 0f && Mathf.Repeat(_flashTimer, 0.1f) > 0.05f ? Color.red : _visualBaseColor;
            }
        }

        // Clear stale crowd overlaps if a crowd is destroyed while we're inside it.
        void LateUpdate() => _crowds.RemoveAll(c => c == null || !c.isActiveAndEnabled);
    }
}

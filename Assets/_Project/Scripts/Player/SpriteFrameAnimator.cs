using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Flip-book animation for the player's Visual from whole texture frames (Assets/Art/RunningAnimation,
    /// IdleAnimation, JumpingAnimation). Each frame becomes a full-rect sprite with its clip's pivot, so frames never
    /// jitter no matter how the artist's importer sliced them (and their import settings are never touched).
    /// Playback follows RunnerController: the run rate scales with speed, the idle clip loops when standing, and the
    /// jump clip is driven by vertical velocity (take-off and tuck rising, reach falling, crouch on a slow landing).
    /// Without idle/jump frames it falls back to holding single run frames.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [Header("Run")]
        [Tooltip("Run cycle, in order. Whole textures; the importer's sprite slicing is ignored.")]
        [SerializeField] Texture2D[] frames;
        [Tooltip("Frames per second at base run speed (the art is drawn on twos: 24 fps = 12 poses/s).")]
        [SerializeField] float framesPerSecond = 24f;
        [Tooltip("Normalized pivot inside each run frame; put it at the feet so the character stands on the ground.")]
        [SerializeField] Vector2 pivot = new Vector2(0.5f, 0.04f);
        [Tooltip("Texture pixels per world unit (bigger = smaller character). Shared by every clip.")]
        [SerializeField] float pixelsPerUnit = 190f;
        [Tooltip("Run frame held while standing still (only when there are no idle frames).")]
        [SerializeField] int idleFrame;
        [Tooltip("Run frame held while in the air (only when there are no jump frames).")]
        [SerializeField] int airborneFrame = 8;
        [Tooltip("Below this fraction of base speed the character counts as standing.")]
        [SerializeField] float idleSpeedFraction = 0.08f;

        [Header("Idle")]
        [Tooltip("Idle loop played while standing still. Leave empty to hold the run's idle frame.")]
        [SerializeField] Texture2D[] idleFrames;
        [SerializeField] float idleFramesPerSecond = 12f;
        [Tooltip("Normalized pivot inside each idle frame (feet).")]
        [SerializeField] Vector2 idlePivot = new Vector2(0.5f, 0.012f);

        [Header("Jump")]
        [Tooltip("Jump sequence. Leave empty to hold the run's airborne frame.")]
        [SerializeField] Texture2D[] jumpFrames;
        [Tooltip("Normalized pivot inside each jump frame (feet).")]
        [SerializeField] Vector2 jumpPivot = new Vector2(0.5f, 0.005f);
        [Tooltip("First jump frame shown on take-off. Frames from here to the fall start play while rising.")]
        [SerializeField] int jumpRiseStart = 2;
        [Tooltip("First jump frame shown while falling.")]
        [SerializeField] int jumpFallStart = 8;
        [Tooltip("First landing frame (crouch). Frames from here to the end play after touching down.")]
        [SerializeField] int jumpLandStart = 11;
        [Tooltip("How long the landing crouch shows.")]
        [SerializeField] float landSeconds = 0.15f;
        [Tooltip("The landing crouch only plays below this fraction of base speed (running keeps running).")]
        [SerializeField] float landMaxSpeedFraction = 0.5f;
        [Tooltip("Ignore landings after shorter hops than this (bumps, steps).")]
        [SerializeField] float minAirSeconds = 0.15f;

        SpriteRenderer _renderer;
        RunnerController _runner;
        Sprite[] _run, _idle, _jump;
        float _phase, _idlePhase, _airTime, _landTimer;
        bool _wasGrounded = true;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _runner = GetComponentInParent<RunnerController>();
            _run = Build(frames, pivot);
            _idle = Build(idleFrames, idlePivot);
            _jump = Build(jumpFrames, jumpPivot);
            if (_idle.Length > 0) Show(_idle, 0);
            else if (_run.Length > 0) Show(_run, Mathf.Clamp(idleFrame, 0, _run.Length - 1));
        }

        Sprite[] Build(Texture2D[] textures, Vector2 clipPivot)
        {
            int count = textures != null ? textures.Length : 0;
            var sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var tex = textures[i];
                if (tex == null) continue;
                // FullRect: no outline generation, so the textures don't need to be readable.
                sprites[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), clipPivot, pixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
                sprites[i].name = tex.name;
            }
            return sprites;
        }

        void OnDestroy()
        {
            DestroyAll(_run);
            DestroyAll(_idle);
            DestroyAll(_jump);
        }

        static void DestroyAll(Sprite[] sprites)
        {
            if (sprites == null) return;
            foreach (var s in sprites) if (s != null) Destroy(s);
        }

        void LateUpdate()
        {
            if (_run == null || _run.Length == 0) return;
            if (_runner == null) { AdvanceRun(1f); return; }

            float dt = Time.deltaTime;
            var tuning = GameRoot.I != null ? GameRoot.I.tuning : null;
            float baseSpeed = tuning != null ? tuning.baseRunSpeed : 8f;
            float jumpVelocity = tuning != null ? tuning.jumpVelocity : 12f;
            float speed01 = _runner.CurrentSpeed / Mathf.Max(0.01f, baseSpeed);
            bool grounded = _runner.Grounded;

            if (!grounded)
            {
                _airTime += dt;
                _landTimer = 0f;
                ShowAirborne(_runner.VerticalSpeed / Mathf.Max(0.01f, jumpVelocity));
                _wasGrounded = false;
                return;
            }

            if (!_wasGrounded && _airTime >= minAirSeconds) _landTimer = landSeconds;
            _wasGrounded = true;
            _airTime = 0f;

            if (_landTimer > 0f)
            {
                _landTimer -= dt;
                if (speed01 < landMaxSpeedFraction && ShowLanding()) return;
                _landTimer = 0f;
            }

            if (speed01 < idleSpeedFraction)
            {
                _phase = 0f;
                if (_idle.Length > 0)
                {
                    _idlePhase = Mathf.Repeat(_idlePhase + dt * idleFramesPerSecond, _idle.Length);
                    Show(_idle, (int)_idlePhase);
                }
                else Show(_run, Mathf.Clamp(idleFrame, 0, _run.Length - 1));
            }
            else
            {
                _idlePhase = 0f;
                AdvanceRun(Mathf.Clamp(speed01, 0.4f, 2f));
            }
        }

        /// <param name="vy01">Vertical speed / jump velocity: 1 at take-off, 0 at the apex, negative falling.</param>
        void ShowAirborne(float vy01)
        {
            if (_jump.Length == 0) { Show(_run, Mathf.Clamp(airborneFrame, 0, _run.Length - 1)); return; }

            int last = _jump.Length - 1;
            int rise = Mathf.Clamp(jumpRiseStart, 0, last);
            int fall = Mathf.Clamp(jumpFallStart, rise, last);
            int land = Mathf.Clamp(jumpLandStart, fall, _jump.Length);
            if (vy01 > 0f) Show(_jump, Pick(rise, fall, 1f - vy01));
            else Show(_jump, Pick(fall, land, -vy01));
        }

        bool ShowLanding()
        {
            int start = Mathf.Clamp(jumpLandStart, 0, _jump.Length);
            if (start >= _jump.Length) return false;
            Show(_jump, Pick(start, _jump.Length, 1f - _landTimer / Mathf.Max(0.01f, landSeconds)));
            return true;
        }

        /// <summary>Frame in [start, end) at progress t (0..1); just start when the range is empty.</summary>
        static int Pick(int start, int end, float t)
        {
            int count = end - start;
            if (count <= 0) return start;
            return start + Mathf.Min(count - 1, (int)(Mathf.Clamp01(t) * count));
        }

        void AdvanceRun(float rate)
        {
            _phase = Mathf.Repeat(_phase + Time.deltaTime * framesPerSecond * rate, _run.Length);
            Show(_run, (int)_phase);
        }

        void Show(Sprite[] clip, int index)
        {
            if (clip[index] != null) _renderer.sprite = clip[index];
        }
    }
}

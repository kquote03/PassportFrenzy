using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Follows the runner on X only, with a lead offset so the player sits in the left third.</summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] float leadX = 6f;
        [Tooltip("Lead used in free-movement mode (GameTuning.autoRun off), so walking back still frames well.")]
        [SerializeField] float freeMoveLeadX = 3f;
        [SerializeField] float fixedY = 3f;
        [SerializeField] float followSharpness = 12f;

        Transform _target;
        float _shakeTimer, _shakeStrength;
        bool _holding;
        float _holdX;

        public Camera Camera { get; private set; }

        /// <summary>Centre of the view in world X (the held position while holding, so shake doesn't move it).</summary>
        public float CenterX => _holding ? _holdX : transform.position.x;

        /// <summary>Half the visible width in world units.</summary>
        public float HalfWidth => Camera.orthographicSize * Camera.aspect;

        void Awake() => Camera = GetComponent<Camera>();

        void LateUpdate()
        {
            if (_target == null && GameRoot.I != null && GameRoot.I.player != null) _target = GameRoot.I.player.transform;
            if (_target == null) return;

            Vector3 goal = new Vector3(_target.position.x + Lead, fixedY, transform.position.z);
            Vector3 p = transform.position;
            if (_holding) p.x = _holdX;
            else p.x = Mathf.Lerp(p.x, goal.x, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            p.y = fixedY;

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                p += (Vector3)(Random.insideUnitCircle * _shakeStrength);
            }
            transform.position = p;
        }

        public void SnapToTarget()
        {
            if (_target == null && GameRoot.I != null) _target = GameRoot.I.player.transform;
            if (_target != null) transform.position = new Vector3(_target.position.x + Lead, fixedY, transform.position.z);
        }

        /// <summary>Stop following and stay where the camera is now (the intro's still shot).</summary>
        public void Hold()
        {
            _holding = true;
            _holdX = transform.position.x;
        }

        /// <summary>Follow the player again, easing into the usual lead.</summary>
        public void Release() => _holding = false;

        float Lead => GameRoot.I != null && !GameRoot.I.tuning.autoRun ? freeMoveLeadX : leadX;

        public void Shake(float strength, float seconds = 0.2f)
        {
            _shakeStrength = strength;
            _shakeTimer = seconds;
        }

        public void SetBackground(Color c) => Camera.backgroundColor = c;
    }
}

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

        public Camera Camera { get; private set; }

        void Awake() => Camera = GetComponent<Camera>();

        void LateUpdate()
        {
            if (_target == null && GameRoot.I != null && GameRoot.I.player != null) _target = GameRoot.I.player.transform;
            if (_target == null) return;

            Vector3 goal = new Vector3(_target.position.x + Lead, fixedY, transform.position.z);
            Vector3 p = transform.position;
            p.x = Mathf.Lerp(p.x, goal.x, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
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

        float Lead => GameRoot.I != null && !GameRoot.I.tuning.autoRun ? freeMoveLeadX : leadX;

        public void Shake(float strength, float seconds = 0.2f)
        {
            _shakeStrength = strength;
            _shakeTimer = seconds;
        }

        public void SetBackground(Color c) => Camera.backgroundColor = c;
    }
}

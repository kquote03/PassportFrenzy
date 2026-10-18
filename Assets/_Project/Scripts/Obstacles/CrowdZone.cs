using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Continuous slow while overlapping; the shield does not block crowds. Jumping into a crowd from outside bounces the
    /// runner over it (<see cref="RunnerController.EnterCrowd"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CrowdZone : MonoBehaviour
    {
        [Tooltip("Speed multiplier while inside. -1 = GameTuning.crowdSpeedMultiplier.")]
        [SerializeField] float speedMultiplier = -1f;

        Collider2D _collider;

        public float Multiplier => speedMultiplier > 0f ? speedMultiplier : GameRoot.I.tuning.crowdSpeedMultiplier;

        /// <summary>World-space extent of the crowd's trigger.</summary>
        public Bounds Bounds => (_collider != null ? _collider : _collider = GetComponent<Collider2D>()).bounds;

        void OnTriggerEnter2D(Collider2D other)
        {
            var r = Runner(other);
            if (r != null) r.EnterCrowd(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var r = Runner(other);
            if (r != null) r.ExitCrowd(this);
        }

        static RunnerController Runner(Collider2D c) =>
            c.attachedRigidbody != null ? c.attachedRigidbody.GetComponent<RunnerController>() : null;
    }
}

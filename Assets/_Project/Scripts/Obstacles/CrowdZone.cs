using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Continuous slow while overlapping. Unavoidable by design; the shield does not block crowds.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class CrowdZone : MonoBehaviour
    {
        [Tooltip("Speed multiplier while inside. -1 = GameTuning.crowdSpeedMultiplier.")]
        [SerializeField] float speedMultiplier = -1f;

        public float Multiplier => speedMultiplier > 0f ? speedMultiplier : GameRoot.I.tuning.crowdSpeedMultiplier;

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

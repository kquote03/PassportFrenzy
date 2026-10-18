using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Trigger-based obstacle. Logic + trigger collider on the root, art on the "Visual" child.
    /// Jumping over the trigger avoids it. Each obstacle hits at most once.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class ObstacleBase : MonoBehaviour
    {
        [Tooltip("Visual alpha after the obstacle has been hit or absorbed.")]
        [Range(0f, 1f)] [SerializeField] float usedAlpha = 0.35f;

        bool _used;

        /// <summary>Already hit (or absorbed by the shield).</summary>
        public bool Used => _used;

        protected abstract bool ApplyHit(RunnerController runner);

        /// <summary>Make the obstacle hittable again (free-movement mode: you backed off instead of jumping).</summary>
        protected void Rearm()
        {
            _used = false;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        void OnTriggerStay2D(Collider2D other) => TryHit(other); // retry after hit-invulnerability ends

        void TryHit(Collider2D other)
        {
            if (_used || other.attachedRigidbody == null) return;
            var runner = other.attachedRigidbody.GetComponent<RunnerController>();
            if (runner == null || !runner.Running) return;
            if (!ApplyHit(runner)) return;
            _used = true;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                var c = sr.color;
                c.a = usedAlpha;
                sr.color = c;
            }
        }
    }
}

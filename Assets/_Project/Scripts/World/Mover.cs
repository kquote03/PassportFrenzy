using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Moves this object once the player is within range (e.g. Spain's running bull charging left).</summary>
    public class Mover : MonoBehaviour
    {
        public Vector2 velocity = new Vector2(-4f, 0f);
        [Tooltip("Starts moving when the player is this close (world units on X).")]
        public float activationDistance = 22f;
        [Tooltip("Optional vertical bob for crowds/animals.")]
        public float bobAmplitude = 0.1f;
        public float bobFrequency = 6f;

        bool _active;
        float _baseY, _t;

        void Start() => _baseY = transform.localPosition.y;

        void Update()
        {
            var player = GameRoot.I != null ? GameRoot.I.player : null;
            if (!_active && player != null && transform.position.x - player.transform.position.x < activationDistance) _active = true;
            if (!_active) return;

            _t += Time.deltaTime;
            Vector3 p = transform.localPosition + (Vector3)(velocity * Time.deltaTime);
            p.y = _baseY + velocity.y * _t + Mathf.Abs(Mathf.Sin(_t * bobFrequency)) * bobAmplitude;
            transform.localPosition = p;
        }
    }
}

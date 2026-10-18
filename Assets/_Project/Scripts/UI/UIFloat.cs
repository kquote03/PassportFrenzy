using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Idle life for menu art: floats a RectTransform slowly down and back up from where it was placed (it never rises
    /// above its placed position, so art touching the top edge stays on screen). Unscaled time.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIFloat : MonoBehaviour
    {
        [Tooltip("How far it drifts down, in canvas pixels.")]
        [SerializeField] float distance = 18f;
        [Tooltip("Seconds for one full down-and-up cycle.")]
        [SerializeField] float period = 3.5f;

        RectTransform _rect;
        Vector2 _placed;
        float _time;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _placed = _rect.anchoredPosition;
        }

        void OnDisable() => _rect.anchoredPosition = _placed;

        void Update()
        {
            _time += Time.unscaledDeltaTime;
            float wave = 0.5f - 0.5f * Mathf.Cos(_time * 2f * Mathf.PI / Mathf.Max(0.1f, period)); // 0 -> 1 -> 0, eased
            _rect.anchoredPosition = _placed + new Vector2(0f, -wave * distance);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// The departing plane (PF_Plane): logic on the root, primitive art under the Visual child, nose pointing right.
    /// Nested in the gate chunk under its WindowMask (it plays the boarding takeoff, clipped by the glass), and
    /// spawned on its own for a missed flight (fly-past, no mask). GameFlow drives it through <see cref="Play"/>.
    /// </summary>
    public class PlaneDeparture : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [Tooltip("Vertical jitter while rolling down the apron.")]
        [SerializeField] float taxiShudder = 0.03f;

        Vector3 _basePos, _baseScale;
        Quaternion _baseRot;
        SpriteRenderer[] _renderers;

        public bool IsPlaying { get; private set; }
        /// <summary>0..1 through the current flight (for tests/automation, e.g. pausing on a mid-climb frame).</summary>
        public float Progress { get; private set; }

        void Awake()
        {
            if (visual == null) visual = transform.Find("Visual") ?? transform;
            _basePos = visual.localPosition;
            _baseScale = visual.localScale;
            _baseRot = visual.localRotation;
            _renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            // Parked behind the gate's glass (a SpriteMask next to us under WindowMask): only the part inside the window shows.
            bool masked = GetComponentInParent<SpriteMask>() != null
                          || (transform.parent != null && transform.parent.GetComponentInChildren<SpriteMask>(true) != null);
            foreach (var r in _renderers)
                r.maskInteraction = masked ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
        }

        /// <summary>Tint the Accent parts for a country (chunks do this through Chunk.OnSpawned; spawned planes call it).</summary>
        public void ApplyPalette(CountryDefinition c)
        {
            foreach (var t in GetComponentsInChildren<PaletteTint>(true)) t.Apply(c);
        }

        /// <summary>Fire-and-forget version of <see cref="Play"/> (runs on this object, so it dies with it).</summary>
        public void Begin(PlaneFlightCurve.Kind kind, float seconds, float flyPastDistance = 34f) =>
            StartCoroutine(Play(kind, seconds, flyPastDistance));

        public IEnumerator Play(PlaneFlightCurve.Kind kind, float seconds, float flyPastDistance = 34f)
        {
            IsPlaying = true;
            // A fly-past crosses right-to-left: mirror the art, so "nose up" rotates the other way.
            float facing = kind == PlaneFlightCurve.Kind.FlyPast ? -1f : 1f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, seconds);
                Progress = Mathf.Clamp01(t);
                var p = PlaneFlightCurve.Evaluate(kind, t, flyPastDistance);
                float shudder = kind == PlaneFlightCurve.Kind.Takeoff && t < PlaneFlightCurve.TaxiFraction
                    ? Mathf.Sin(Time.time * 90f) * taxiShudder : 0f;
                visual.localPosition = _basePos + new Vector3(p.dx, p.dy + shudder, 0f);
                visual.localRotation = _baseRot * Quaternion.Euler(0f, 0f, facing * p.angleDeg);
                visual.localScale = new Vector3(_baseScale.x * p.scale * facing, _baseScale.y * p.scale, _baseScale.z);
                yield return null;
            }
            IsPlaying = false;
        }

        public void ResetVisual()
        {
            StopAllCoroutines();
            IsPlaying = false;
            Progress = 0f;
            visual.localPosition = _basePos;
            visual.localRotation = _baseRot;
            visual.localScale = _baseScale;
        }
    }
}

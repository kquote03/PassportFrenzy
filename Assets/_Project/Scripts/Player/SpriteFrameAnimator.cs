using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Flip-book animation for the player's Visual from whole texture frames (e.g. Assets/Art/RunningAnimation).
    /// Each frame becomes a full-rect sprite with the same pivot, so frames never jitter no matter how the artist's
    /// importer sliced them (and their import settings are never touched). Playback follows RunnerController:
    /// frame rate scales with run speed, an idle frame is held when standing, an airborne frame while jumping.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [Tooltip("Run cycle, in order. Whole textures; the importer's sprite slicing is ignored.")]
        [SerializeField] Texture2D[] frames;
        [Tooltip("Frames per second at base run speed (the art is drawn on twos: 24 fps = 12 poses/s).")]
        [SerializeField] float framesPerSecond = 24f;
        [Tooltip("Normalized pivot inside each frame; put it at the feet so the character stands on the ground.")]
        [SerializeField] Vector2 pivot = new Vector2(0.5f, 0.04f);
        [Tooltip("Texture pixels per world unit (bigger = smaller character).")]
        [SerializeField] float pixelsPerUnit = 190f;
        [Tooltip("Frame held while standing still.")]
        [SerializeField] int idleFrame;
        [Tooltip("Frame held while in the air.")]
        [SerializeField] int airborneFrame = 8;
        [Tooltip("Below this fraction of base speed the idle frame is shown.")]
        [SerializeField] float idleSpeedFraction = 0.08f;

        SpriteRenderer _renderer;
        RunnerController _runner;
        Sprite[] _sprites;
        float _phase;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _runner = GetComponentInParent<RunnerController>();
            BuildSprites();
        }

        void BuildSprites()
        {
            int count = frames != null ? frames.Length : 0;
            _sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var tex = frames[i];
                if (tex == null) continue;
                // FullRect: no outline generation, so the textures don't need to be readable.
                _sprites[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, pixelsPerUnit, 0,
                    SpriteMeshType.FullRect);
                _sprites[i].name = tex.name;
            }
            if (count > 0) Show(Mathf.Clamp(idleFrame, 0, count - 1));
        }

        void OnDestroy()
        {
            if (_sprites == null) return;
            foreach (var s in _sprites) if (s != null) Destroy(s);
        }

        void LateUpdate()
        {
            if (_sprites == null || _sprites.Length == 0) return;
            if (_runner == null) { Advance(1f); return; }

            float baseSpeed = GameRoot.I != null ? GameRoot.I.tuning.baseRunSpeed : 8f;
            float speed01 = _runner.CurrentSpeed / Mathf.Max(0.01f, baseSpeed);
            if (!_runner.Grounded) Show(Mathf.Clamp(airborneFrame, 0, _sprites.Length - 1));
            else if (speed01 < idleSpeedFraction) { _phase = 0f; Show(Mathf.Clamp(idleFrame, 0, _sprites.Length - 1)); }
            else Advance(Mathf.Clamp(speed01, 0.4f, 2f));
        }

        void Advance(float rate)
        {
            _phase = Mathf.Repeat(_phase + Time.deltaTime * framesPerSecond * rate, _sprites.Length);
            Show((int)_phase);
        }

        void Show(int index)
        {
            if (_sprites[index] != null) _renderer.sprite = _sprites[index];
        }
    }
}

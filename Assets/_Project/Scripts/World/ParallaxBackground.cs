using System;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Country backdrop behind the chunks: a sky haze and ground haze glued to the camera, plus tiled silhouette
    /// layers (clouds, far landmarks, mid skyline) that scroll slower than the world and connect across chunk seams.
    /// Sprites are generated white silhouettes (Placeholder/Backgrounds); colours come from the country palette.
    /// Sorting orders -40..-30 sit behind every chunk backdrop (-20..-10), so the painted airport wall hides them indoors.
    /// </summary>
    [DefaultExecutionOrder(50)] // after PlayerCamera.LateUpdate
    public class ParallaxBackground : MonoBehaviour
    {
        public enum TintSource { White, Far, Mid }

        [Serializable]
        public class Layer
        {
            public string name = "Layer";
            public Sprite sprite;
            [Tooltip("Fraction of world movement followed: 0 = glued to the camera, 1 = fixed in the world.")]
            [Range(0f, 1f)] public float motion = 0.15f;
            public int sortingOrder = -35;
            [Tooltip("World Y of the sprite's bottom edge.")]
            public float baseY;
            [Tooltip("Constant drift in units/second (clouds).")]
            public float driftSpeed;
            [Range(0f, 1f)] public float alpha = 1f;
            public TintSource tint = TintSource.Far;
            [NonSerialized] public SpriteRenderer[] tiles;
            [NonSerialized] public float drift;
        }

        [Tooltip("Parent for the runtime tile renderers (artist-swappable sprites live in the layers below).")]
        [SerializeField] Transform layerParent;
        [SerializeField] Layer[] layers = Array.Empty<Layer>();
        [Tooltip("World width of one tile of every layer sprite.")]
        [SerializeField] float tileWidth = 32f;
        [SerializeField] int tilesPerLayer = 3;

        [Header("Camera-glued haze")]
        [SerializeField] Sprite skyHazeSprite;
        [SerializeField] Sprite groundHazeSprite;
        [SerializeField] int hazeOrder = -40;
        [Tooltip("World Y range the sky haze covers (bottom = horizon, strongest).")]
        [SerializeField] Vector2 skyHazeY = new Vector2(-1f, 9f);
        [SerializeField] Vector2 groundHazeY = new Vector2(-3.5f, -2f);

        SpriteRenderer _skyHaze, _groundHaze;
        CountryDefinition _country;
        bool _built;

        GameTuning T => GameRoot.I != null ? GameRoot.I.tuning : null;
        Transform Parent => layerParent != null ? layerParent : transform;

        void Awake() => Build();

        void Build()
        {
            if (_built) return;
            _built = true;
            foreach (var l in layers)
            {
                l.tiles = new SpriteRenderer[Mathf.Max(1, tilesPerLayer)];
                for (int i = 0; i < l.tiles.Length; i++) l.tiles[i] = MakeRenderer(l.name + i, l.sortingOrder);
            }
            _skyHaze = MakeRenderer("SkyHaze", hazeOrder);
            _skyHaze.sprite = skyHazeSprite;
            _groundHaze = MakeRenderer("GroundHaze", hazeOrder);
            _groundHaze.sprite = groundHazeSprite;
            SetVisible(false);
        }

        SpriteRenderer MakeRenderer(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Swap the country's silhouettes and recolour everything from its palette. Null hides the backdrop.</summary>
        public void SetCountry(CountryDefinition c)
        {
            Build();
            _country = c;
            if (c == null) { SetVisible(false); return; }

            float farBlend = T != null ? T.parallaxFarBlend : 0.35f;
            float midBlend = T != null ? T.parallaxMidBlend : 0.6f;
            Color far = ParallaxMath.LayerTint(c.skyColor, c.primaryColor, farBlend);
            Color mid = ParallaxMath.LayerTint(c.skyColor, c.primaryColor, midBlend);
            foreach (var l in layers)
            {
                Sprite s = l.tint switch
                {
                    TintSource.Far => c.backgroundFar != null ? c.backgroundFar : l.sprite,
                    TintSource.Mid => c.backgroundMid != null ? c.backgroundMid : l.sprite,
                    _ => l.sprite,
                };
                Color col = l.tint switch { TintSource.Far => far, TintSource.Mid => mid, _ => Color.white };
                col.a = l.alpha;
                foreach (var r in l.tiles) { r.sprite = s; r.color = col; r.enabled = s != null; }
            }
            _skyHaze.color = Color.Lerp(c.skyColor, Color.white, 0.5f);
            _groundHaze.color = c.groundColor * new Color(0.8f, 0.8f, 0.8f, 1f);
            SetVisible(true);
            LateUpdate();
        }

        public void SetVisible(bool on)
        {
            Build();
            foreach (var l in layers) foreach (var r in l.tiles) r.gameObject.SetActive(on && r.sprite != null);
            _skyHaze.gameObject.SetActive(on && _skyHaze.sprite != null);
            _groundHaze.gameObject.SetActive(on && _groundHaze.sprite != null);
        }

        void LateUpdate()
        {
            if (_country == null || GameRoot.I == null || GameRoot.I.Cam == null) return;
            var cam = GameRoot.I.Cam;
            float camX = cam.transform.position.x;
            float hw = cam.HalfWidth + 1f; // a little slack for shake

            foreach (var l in layers)
            {
                if (l.tiles[0].sprite == null) continue;
                l.drift += l.driftSpeed * Time.deltaTime;
                var (kMin, count) = ParallaxMath.TileRange(camX - l.drift, l.motion, hw, tileWidth);
                for (int i = 0; i < l.tiles.Length; i++)
                {
                    var r = l.tiles[i];
                    bool used = i < count;
                    r.gameObject.SetActive(used);
                    if (!used) continue;
                    float left = ParallaxMath.TileLeftX(kMin + i, camX - l.drift, l.motion, tileWidth) + l.drift;
                    float w = r.sprite.bounds.size.x, h = r.sprite.bounds.size.y;
                    r.transform.position = new Vector3(left + w * 0.5f, l.baseY + h * 0.5f, 0f);
                    r.transform.localScale = new Vector3(tileWidth / w, 1f, 1f);
                }
            }

            FitHaze(_skyHaze, camX, hw, skyHazeY);
            FitHaze(_groundHaze, camX, hw, groundHazeY);
        }

        static void FitHaze(SpriteRenderer sr, float camX, float hw, Vector2 yRange)
        {
            if (sr.sprite == null) return;
            var size = sr.sprite.bounds.size;
            sr.transform.position = new Vector3(camX, (yRange.x + yRange.y) * 0.5f, 0f);
            sr.transform.localScale = new Vector3(2f * hw / size.x, (yRange.y - yRange.x) / size.y, 1f);
        }
    }
}

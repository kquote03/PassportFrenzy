using System;
using UnityEngine;

namespace PassportFrenzy
{
    [Serializable]
    public class WeightedChunk
    {
        public Chunk prefab;
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// One country = one asset. Adding a country needs only a new asset (plus optional chunk prefabs)
    /// registered in the CountryDatabase. No code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Passport Frenzy/Country", fileName = "Country_New")]
    public class CountryDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "xx";
        public string displayName = "New Country";
        [Tooltip("Home country (UAE): main menu airport, never offered as a residency.")]
        public bool isHome;

        [Header("Placeholder palette (applied through PaletteTint)")]
        public Color skyColor = new Color(0.55f, 0.75f, 0.95f);
        public Color groundColor = new Color(0.35f, 0.3f, 0.25f);
        public Color primaryColor = Color.white;
        public Color accentColor = Color.red;

        [Header("Rules")]
        [Range(1, 3)] public int difficultyTier = 1;
        [Tooltip("Years (accumulated across visits within a run) needed to apply for residency.")]
        public int requiredResidencyYears = 3;
        [Tooltip("Visa duration in years rolled at booking, inclusive.")]
        public Vector2Int visaYearsRange = new Vector2Int(2, 5);

        [Header("Content")]
        public string[] festivalNames = { "Local Festival" };
        [Tooltip("Country-run chunks. Empty = CountryDatabase.genericChunkPool.")]
        public WeightedChunk[] chunkPool = Array.Empty<WeightedChunk>();
        [Tooltip("Airport chase chunks. Empty = CountryDatabase.defaultAirportChasePool.")]
        public WeightedChunk[] airportChunkPool = Array.Empty<WeightedChunk>();
        [Tooltip("Optional unique festival chunk (e.g. La Tomatina). Used for the first planned festival of a visit; "
                 + "its FestivalZone name override is the festival's name.")]
        public Chunk signatureFestivalChunk;
        [Tooltip("Festival zone used by the debug 'spawn festival' button. Empty = database default.")]
        public FestivalZone festivalZonePrefab;

        [Header("Background (white silhouettes, tinted from the palette by ParallaxBackground)")]
        [Tooltip("Far landmarks skyline, tileable (Placeholder/Backgrounds/bg_<id>_far). Empty = the layer's default sprite.")]
        public Sprite backgroundFar;
        [Tooltip("Nearer rooftops/trees, tileable (Placeholder/Backgrounds/bg_<id>_mid).")]
        public Sprite backgroundMid;

        [Header("Audio")]
        [Tooltip("Optional. If empty, AudioManager looks up 'music_<id>' in the SoundLibrary.")]
        public AudioClip music;

        public string RandomFestivalName()
        {
            if (festivalNames == null || festivalNames.Length == 0) return displayName + " Festival";
            return festivalNames[UnityEngine.Random.Range(0, festivalNames.Length)];
        }

        public int RollVisaYears(System.Random rng)
        {
            int min = Mathf.Max(1, Mathf.Min(visaYearsRange.x, visaYearsRange.y));
            int max = Mathf.Max(min, Mathf.Max(visaYearsRange.x, visaYearsRange.y));
            return rng.Next(min, max + 1);
        }
    }
}

using UnityEngine;

namespace PassportFrenzy
{
    public enum DensityCategory { Obstacle, Crowd }

    /// <summary>
    /// Put on an obstacle/crowd inside a chunk. When the chunk spawns, the object stays active with
    /// probability baseChance * tier density (GameTuning.tiers). Chunks author layouts; tiers thin them out.
    /// </summary>
    public class DensityGated : MonoBehaviour
    {
        public DensityCategory category = DensityCategory.Obstacle;
        [Range(0f, 1f)] public float baseChance = 1f;

        public void Gate(int tier)
        {
            var t = GameRoot.I.tuning.GetTier(tier);
            float density = category == DensityCategory.Crowd ? t.crowdDensity : t.obstacleDensity;
            gameObject.SetActive(Random.value < baseChance * density);
        }
    }
}

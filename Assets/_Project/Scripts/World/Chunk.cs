using UnityEngine;

namespace PassportFrenzy
{
    public enum ChunkKind { Country, Transition, AirportSafe, AirportChase, Gate, Home }

    /// <summary>What the spawner tells a chunk when it is placed.</summary>
    public struct SpawnContext
    {
        public CountryDefinition palette;   // colors for PaletteTint
        public CountryDefinition country;   // festival names / photo tagging
        public int tier;                    // density scaling (1-3)
    }

    /// <summary>
    /// A level segment. Pivot = left edge at ground height (ground top is y = 0).
    /// Children opt into behaviour with PaletteTint, DensityGated, PickupSocket, FestivalZone, FlowTrigger.
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        [SerializeField, Min(1f)] float length = 24f;
        public ChunkKind kind = ChunkKind.Country;

        public float Length => length;

        public void OnSpawned(SpawnContext ctx)
        {
            foreach (var g in GetComponentsInChildren<DensityGated>(true)) g.Gate(ctx.tier);
            foreach (var p in GetComponentsInChildren<PaletteTint>(true)) p.Apply(ctx.palette);
            foreach (var f in GetComponentsInChildren<FestivalZone>(true)) f.SetCountry(ctx.country);
            foreach (var s in GetComponentsInChildren<PickupSocket>(true)) s.TrySpawn();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
            Vector3 a = transform.position, b = a + Vector3.right * length;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(a, a + Vector3.up * 6f);
            Gizmos.DrawLine(b, b + Vector3.up * 6f);
        }
    }
}

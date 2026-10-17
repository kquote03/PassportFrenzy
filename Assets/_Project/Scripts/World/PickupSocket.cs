using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Maybe spawns a random buff pickup here (GameTuning.pickupChance * chanceMultiplier).</summary>
    public class PickupSocket : MonoBehaviour
    {
        [Min(0f)] public float chanceMultiplier = 1f;

        public void TrySpawn()
        {
            var root = GameRoot.I;
            if (root.pickupBuffs.Count == 0 || root.countries.pickupPrefab == null) return;
            if (Random.value >= root.tuning.pickupChance * chanceMultiplier) return;
            var buff = root.pickupBuffs[Random.Range(0, root.pickupBuffs.Count)];
            var pickup = Instantiate(root.countries.pickupPrefab, transform.position, Quaternion.identity, transform);
            pickup.SetBuff(buff);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}

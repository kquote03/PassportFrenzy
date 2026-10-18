using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Picks one of several sprites for this SpriteRenderer when spawned (e.g. suitcase colours).
    /// Put it on a Visual; the sprite assigned in the prefab is what the editor shows.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RandomSpriteVariant : MonoBehaviour
    {
        [Tooltip("Sprites to choose from; empty entries are skipped.")]
        [SerializeField] Sprite[] variants;
        [Tooltip("Also mirror the sprite horizontally at random.")]
        [SerializeField] bool randomFlipX;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (variants != null && variants.Length > 0)
            {
                var pick = variants[Random.Range(0, variants.Length)];
                if (pick != null) sr.sprite = pick;
            }
            if (randomFlipX) sr.flipX = Random.value < 0.5f;
        }
    }
}

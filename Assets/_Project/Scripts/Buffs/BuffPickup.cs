using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Rare in-run pickup wrapping a BuffDefinition. Tints its Visual with the buff color.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BuffPickup : MonoBehaviour
    {
        [SerializeField] BuffDefinition buff;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] TMP_Text label;

        float _t;
        Vector3 _base;

        void Start()
        {
            _base = transform.localPosition;
            if (buff != null) SetBuff(buff);
        }

        public void SetBuff(BuffDefinition b)
        {
            buff = b;
            if (visual != null) visual.color = b.color;
            if (label != null) label.text = string.IsNullOrEmpty(b.displayName) ? "?" : b.displayName.Substring(0, 1);
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.localPosition = _base + Vector3.up * Mathf.Sin(_t * 4f) * 0.15f;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (buff == null || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<RunnerController>() == null) return;
            if (!GameRoot.I.buffs.Acquire(buff)) return;
            GameRoot.I.audioManager.Play(SoundIds.Pickup);
            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>Visual only: copies another graphic's color (e.g. the stamp frame follows the APPROVED/REJECTED ink).</summary>
    [RequireComponent(typeof(Graphic))]
    public class MatchGraphicColor : MonoBehaviour
    {
        [SerializeField] Graphic source;
        [SerializeField] float alpha = 1f;

        Graphic _self;

        void Awake() => _self = GetComponent<Graphic>();

        void LateUpdate()
        {
            if (source == null) return;
            var c = source.color;
            c.a *= alpha;
            _self.color = c;
        }
    }
}

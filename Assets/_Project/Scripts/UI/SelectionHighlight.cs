using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// Visual only: shows a glowing rounded ring and scales the element up a little while it is selected (controller)
    /// or hovered (mouse), so the current choice is always obvious. Lives next to a Selectable.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class SelectionHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Graphic ring;
        [SerializeField] float selectedScale = 1.04f;
        [SerializeField] float pulseSpeed = 5f;

        Selectable _selectable;
        bool _hover;
        float _t;

        void Awake() => _selectable = GetComponent<Selectable>();

        void OnEnable() => Apply(0f, true);

        void OnDisable()
        {
            _hover = false;
            Apply(0f, true);
        }

        public void OnPointerEnter(PointerEventData e) => _hover = true;
        public void OnPointerExit(PointerEventData e) => _hover = false;

        void Update()
        {
            var es = EventSystem.current;
            bool selected = es != null && es.currentSelectedGameObject == gameObject;
            bool on = (selected || _hover) && _selectable.IsInteractable();
            _t = Mathf.MoveTowards(_t, on ? 1f : 0f, Time.unscaledDeltaTime * 10f);
            Apply(_t, false);
        }

        void Apply(float t, bool snap)
        {
            if (snap) _t = t;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, selectedScale, t);
            if (ring == null) return;
            ring.enabled = t > 0.01f;
            var c = ring.color;
            c.a = t * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
            ring.color = c;
        }
    }
}

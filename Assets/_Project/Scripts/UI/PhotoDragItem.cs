using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PassportFrenzy
{
    /// <summary>A festival photo in the residency tray. Drag it onto the paper with the mouse.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PhotoDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] TMP_Text caption;

        ResidencyApp _owner;
        CanvasGroup _group;

        public bool Attached { get; private set; }
        RectTransform Rect => (RectTransform)transform;

        public void Init(ResidencyApp owner, string festivalName)
        {
            _owner = owner;
            _group = GetComponent<CanvasGroup>();
            caption.text = festivalName;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (Attached) return;
            transform.SetParent(_owner.DragLayer, true);
            transform.SetAsLastSibling();
            _group.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData e)
        {
            if (Attached) return;
            Rect.position = e.position;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (Attached) return;
            _group.blocksRaycasts = true;
            if (_owner.IsOverPaper(e.position)) _owner.Attach(this);
            else _owner.ReturnToTray(this);
        }

        public void AttachTo(RectTransform slot)
        {
            Attached = true;
            _group.blocksRaycasts = false;
            transform.SetParent(slot, false);
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.anchoredPosition = Vector2.zero;
            transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-8f, 8f));
        }

        public void ReturnToTray(RectTransform tray)
        {
            transform.SetParent(tray, false);
            _group.blocksRaycasts = true;
        }
    }
}

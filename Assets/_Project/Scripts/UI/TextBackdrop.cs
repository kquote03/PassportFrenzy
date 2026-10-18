using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// Visual only: a rounded shape that hugs a label's text and follows its visibility.
    /// As the sibling just before the label it is a pill behind it (toasts, prompts); as a child of the label it rotates
    /// with it and draws on top (e.g. a hollow passport-stamp frame).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TextBackdrop : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] Vector2 padding = new Vector2(36f, 14f);

        Image _image;
        RectTransform _rect;
        string _last;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
        }

        void LateUpdate()
        {
            bool on = text != null && text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text);
            _image.enabled = on;
            if (!on) { _last = null; return; }
            if (text.text != _last)
            {
                _last = text.text;
                text.ForceMeshUpdate();
            }
            var b = text.textBounds;
            _rect.sizeDelta = new Vector2(b.size.x + padding.x * 2f, b.size.y + padding.y * 2f);
            if (transform.parent == text.transform) _rect.localPosition = b.center;
            else _rect.position = text.rectTransform.TransformPoint(b.center);
        }
    }
}

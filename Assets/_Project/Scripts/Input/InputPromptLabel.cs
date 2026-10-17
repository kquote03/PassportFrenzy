using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Static label that shows device-correct prompts, e.g. "{Jump} Jump". Edit the template in the Inspector.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class InputPromptLabel : MonoBehaviour
    {
        [TextArea] public string template = "{Jump} Jump";

        TMP_Text _text;

        void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            InputDeviceTracker.Changed += OnChanged;
            Refresh();
        }

        void OnDisable() => InputDeviceTracker.Changed -= OnChanged;

        void OnChanged(ControlScheme _) => Refresh();

        public void SetTemplate(string t)
        {
            template = t;
            Refresh();
        }

        void Refresh()
        {
            if (_text != null) _text.text = InputPrompts.Format(template);
        }
    }
}

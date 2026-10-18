using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>Main menu shown at the home airport (UAE).</summary>
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button playButton;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text controlsText;
        [Tooltip("Seconds the menu takes to fade out after Play.")]
        [SerializeField] float fadeSeconds = 0.45f;

        CanvasGroup _group;
        float _fadeLeft; // > 0 while fading out

        string ControlsTemplate => (GameRoot.I != null && !GameRoot.I.tuning.autoRun ? "{Move} Move    " : "")
                                   + "{Jump} Jump    {Photo} Photo    {Use} Use item    {PhoneControls} Phone    {Debug} Debug";

        void Awake()
        {
            playButton.onClick.AddListener(() => GameRoot.I.flow.StartRun());
            InputDeviceTracker.Changed += OnDeviceChanged;
        }

        void OnDestroy() => InputDeviceTracker.Changed -= OnDeviceChanged;

        void OnDeviceChanged(ControlScheme _) => controlsText.text = InputPrompts.Format(ControlsTemplate);

        CanvasGroup Group
        {
            get
            {
                if (_group == null) _group = panel.GetComponent<CanvasGroup>();
                if (_group == null) _group = panel.AddComponent<CanvasGroup>();
                return _group;
            }
        }

        public void Show()
        {
            _fadeLeft = 0f;
            Group.alpha = 1f;
            Group.interactable = Group.blocksRaycasts = true;
            panel.SetActive(true);
            var db = GameRoot.I.countries;
            var d = SaveSystem.Data;
            // One line under PLAY: the menu follows the artist's logo + PLAY mock-up.
            statsText.text = $"<b>Passports {db.CountOwned(d)}/{db.ActiveDestinations.Count}</b>     Miles {d.miles}";
            controlsText.text = InputPrompts.Format(ControlsTemplate);
            UIRoot.Select(playButton.gameObject);
        }

        /// <summary>Fades the menu out (the intro starts underneath it); clicks are ignored from the first frame.</summary>
        public void Hide()
        {
            if (!panel.activeSelf || _fadeLeft > 0f) return;
            Group.interactable = Group.blocksRaycasts = false;
            UIRoot.Select(null);
            _fadeLeft = Mathf.Max(0.01f, fadeSeconds);
        }

        void Update()
        {
            if (_fadeLeft > 0f)
            {
                _fadeLeft -= Time.unscaledDeltaTime;
                Group.alpha = Mathf.Clamp01(_fadeLeft / Mathf.Max(0.01f, fadeSeconds));
                if (_fadeLeft <= 0f) panel.SetActive(false);
                return;
            }
            // Keyboard/controller users must never lose focus (a stray mouse click on the background deselects).
            if (panel.activeSelf && UIRoot.NothingSelected) UIRoot.Select(playButton.gameObject);
        }
    }
}

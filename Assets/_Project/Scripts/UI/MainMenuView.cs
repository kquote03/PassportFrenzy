using System.Text;
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

        string ControlsTemplate => (GameRoot.I != null && !GameRoot.I.tuning.autoRun ? "{Move} Move    " : "")
                                   + "{Jump} Jump    {Photo} Photo    {Use} Use item    {Debug} Debug";

        void Awake()
        {
            playButton.onClick.AddListener(() => GameRoot.I.flow.StartRun());
            InputDeviceTracker.Changed += OnDeviceChanged;
        }

        void OnDestroy() => InputDeviceTracker.Changed -= OnDeviceChanged;

        void OnDeviceChanged(ControlScheme _) => controlsText.text = InputPrompts.Format(ControlsTemplate);

        public void Show()
        {
            panel.SetActive(true);
            var db = GameRoot.I.countries;
            var d = SaveSystem.Data;
            var sb = new StringBuilder();
            sb.AppendLine($"Passports: {d.passports.Count}/{db.destinations.Count}     Miles: {d.miles}");
            foreach (var c in db.destinations)
                if (c != null) sb.Append(d.Owns(c.id) ? $"<color=#2a8a2a>[{c.displayName}]</color> " : $"<color=#888888>{c.displayName}</color> ");
            statsText.text = sb.ToString();
            controlsText.text = InputPrompts.Format(ControlsTemplate);
            UIRoot.Select(playButton.gameObject);
        }

        public void Hide() => panel.SetActive(false);

        void Update()
        {
            // Keyboard/controller users must never lose focus (a stray mouse click on the background deselects).
            if (panel.activeSelf && UIRoot.NothingSelected) UIRoot.Select(playButton.gameObject);
        }
    }
}

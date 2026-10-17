using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    public class BookingCardView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text visaText;
        [SerializeField] TMP_Text infoText;
        [SerializeField] Image swatchPrimary;
        [SerializeField] Image swatchAccent;
        [SerializeField] Image background;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color pickedColor = new Color(0.6f, 1f, 0.6f);
        [SerializeField] Color rejectedColor = new Color(0.6f, 0.6f, 0.6f);

        public void Bind(BookingOption o, float secondsPerYear, bool owned, Action onClick)
        {
            var c = o.country;
            nameText.text = c.displayName.ToUpperInvariant();
            visaText.text = "Visa: " + TimeFormat.Years(o.visaYears, secondsPerYear);
            infoText.text = $"Difficulty {new string('*', c.difficultyTier)}   Residency: {c.requiredResidencyYears} yrs";
            swatchPrimary.color = c.primaryColor;
            swatchAccent.color = c.accentColor;
            background.color = normalColor;
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        public void SetResult(bool picked)
        {
            background.color = picked ? pickedColor : rejectedColor;
            button.interactable = false;
        }
    }
}

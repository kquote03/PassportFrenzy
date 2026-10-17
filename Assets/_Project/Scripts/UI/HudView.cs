using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>In-run HUD. Polls GameFlow/RunState each frame (cheap, and impossible to get out of sync).</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] GameObject statsGroup;
        [SerializeField] TMP_Text countryText;
        [SerializeField] TMP_Text visaText;
        [SerializeField] Image visaFill;
        [SerializeField] TMP_Text residencyText;
        [SerializeField] TMP_Text photosText;
        [SerializeField] TMP_Text milesText;
        [SerializeField] TMP_Text itemText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text toastText;
        [SerializeField] Image photoFlash;
        [SerializeField] float toastSeconds = 2.2f;

        readonly Queue<string> _toasts = new Queue<string>();
        float _toastTimer;
        FestivalZone _zone;

        void OnEnable()
        {
            GameEvents.Toast += OnToast;
            GameEvents.FestivalInRange += OnFestival;
            GameEvents.PhotoTaken += OnPhoto;
            toastText.gameObject.SetActive(false);
            promptText.gameObject.SetActive(false);
            SetFlash(0f);
        }

        void OnDisable()
        {
            GameEvents.Toast -= OnToast;
            GameEvents.FestivalInRange -= OnFestival;
            GameEvents.PhotoTaken -= OnPhoto;
        }

        void OnToast(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (_toasts.Count > 3) _toasts.Dequeue();
            _toasts.Enqueue(msg);
        }

        void OnFestival(FestivalZone z) => _zone = z;
        void OnPhoto(PhotoRecord _) => SetFlash(0.9f);

        void SetFlash(float a)
        {
            var c = photoFlash.color;
            c.a = a;
            photoFlash.color = c;
        }

        void Update()
        {
            UpdateToast();
            if (photoFlash.color.a > 0f) SetFlash(Mathf.MoveTowards(photoFlash.color.a, 0f, Time.deltaTime * 3f));

            var root = GameRoot.I;
            var run = root.flow.Run;
            var state = root.flow.State;
            bool show = run != null && state != GameState.Won;
            statsGroup.SetActive(show);

            bool prompt = _zone != null && !_zone.Photographed && !root.input.MenuMode;
            promptText.gameObject.SetActive(prompt);
            if (prompt) promptText.text = InputPrompts.Format($"{{Photo}} Photograph {_zone.FestivalName}!");

            if (!show) return;
            var T = root.tuning;
            var c = run.Current;
            float spy = T.SecondsPerYear;

            countryText.text = c.isHome ? $"HOME: {c.displayName}" : $"{c.displayName.ToUpperInvariant()}  {new string('*', c.difficultyTier)}";

            bool inCountry = state is GameState.CountryRun or GameState.ToAirport;
            if (inCountry && run.VisaOverstayed)
            {
                visaText.text = "<color=#FF5252>VISA OVERSTAYED - miles lost</color>";
                visaFill.fillAmount = 0f;
            }
            else if (inCountry)
            {
                // Visa deadline: red when you can't make the airport at full speed any more.
                float toAirport = root.spawner.AirportX - root.player.transform.position.x;
                string airport = float.IsNaN(toAirport) ? "" : $"  -  Airport in {Mathf.Max(0, Mathf.CeilToInt(toAirport))} m";
                bool late = !float.IsNaN(toAirport) && toAirport / T.baseRunSpeed > run.VisaSecondsRemaining;
                string clock = TimeFormat.Clock(run.VisaSecondsRemaining);
                visaText.text = late ? $"<color=#FF5252>Visa left: {clock}{airport}</color>" : $"Visa left: {clock}{airport}";
                visaFill.fillAmount = run.VisaSecondsTotal > 0f ? run.VisaSecondsRemaining / run.VisaSecondsTotal : 0f;
            }
            else
            {
                visaText.text = c.isHome ? "Home airport" : $"{c.displayName} airport";
                visaFill.fillAmount = 0f;
            }

            if (c.isHome) residencyText.text = "Home sweet home";
            else if (SaveSystem.Data.Owns(c.id)) residencyText.text = "Residency: OWNED";
            else
            {
                string years = TimeFormat.YearsDecimal(run.SecondsIn(c.id), spy);
                bool ok = run.IsResidencyEligible(c, spy);
                string paused = inCountry ? "" : "  (paused at airport)";
                residencyText.text = $"Residency: {years}/{c.requiredResidencyYears} yrs{(ok ? "  READY" : paused)}";
            }

            photosText.text = c.isHome ? "" : $"Photos here: {run.PhotoCount(c.id)}/{T.photosRequired}";
            milesText.text = $"Miles: {SaveSystem.Data.miles}   Passports: {SaveSystem.Data.passports.Count}/{root.countries.destinations.Count}";

            var held = root.buffs.Held;
            itemText.text = held != null ? InputPrompts.Format($"Item: {held.displayName}  {{Use}}") : "Item: (empty)";

            string status = "";
            if (root.player.HasShield) status += "SHIELD  ";
            if (root.player.BoostSecondsRemaining > 0f) status += $"BOOST {root.player.BoostSecondsRemaining:0}s  ";
            if (root.player.InCrowd) status += "CROWD - slowed  ";
            if (root.player.Blocked) status += InputPrompts.Format("BLOCKED! {Jump}  ");
            if (T.DemoMode) status += "  [DEMO]";
            statusText.text = status;
        }

        void UpdateToast()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f) toastText.gameObject.SetActive(false);
            }
            if (_toastTimer <= 0f && _toasts.Count > 0)
            {
                toastText.text = _toasts.Dequeue();
                toastText.gameObject.SetActive(true);
                _toastTimer = _toasts.Count > 0 ? toastSeconds * 0.6f : toastSeconds;
            }
        }
    }
}

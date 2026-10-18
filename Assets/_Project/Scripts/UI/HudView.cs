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
        [Header("Announcement banner (built from the toast at runtime)")]
        [SerializeField] float announcementFontScale = 1.6f;
        [Tooltip("Banner position below the top of the screen (the toast sits at about -206).")]
        [SerializeField] float announcementY = -290f;
        [Header("Control hint line (built from the toast at runtime)")]
        [Tooltip("Hint position above the bottom of the screen.")]
        [SerializeField] float hintY = 90f;

        readonly Queue<string> _toasts = new Queue<string>();
        float _toastTimer;
        FestivalZone _zone;
        RectTransform _announce;
        CanvasGroup _announceGroup;
        TMP_Text _announceText;
        float _announceTimer, _announceSeconds;
        readonly ControlHints _hints = new ControlHints();
        CanvasGroup _hintGroup;
        TMP_Text _hintText;
        string _hintRaw;

        void Awake()
        {
            _announceText = CloneToast("Announcement", 1f, announcementY, 220f, announcementFontScale, out _announce, out _announceGroup);
            _announceText.lineSpacing = -10f;
            _hintText = CloneToast("ControlHint", 0f, hintY, 90f, 1f, out var hintRect, out _hintGroup);
            _hintGroup.alpha = 0f;
            hintRect.gameObject.SetActive(true);
        }

        /// <summary>
        /// Clones the toast label and its pill into a new HUD line (anchored at the top or bottom centre), so it matches the
        /// HUD style without a prefab change. Returns the label; the container starts inactive.
        /// </summary>
        TMP_Text CloneToast(string name, float anchorY, float y, float height, float fontScale, out RectTransform container,
            out CanvasGroup group)
        {
            var parent = toastText.transform.parent;
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            container = (RectTransform)go.transform;
            container.SetParent(parent, false);
            container.SetSiblingIndex(toastText.transform.GetSiblingIndex() + 1);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, anchorY);
            container.anchoredPosition = new Vector2(0f, y);
            container.sizeDelta = new Vector2(toastText.rectTransform.sizeDelta.x, height);
            group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            TextBackdrop pill = null;
            foreach (Transform sibling in parent)
            {
                var b = sibling.GetComponent<TextBackdrop>();
                if (b != null && b.Target == toastText) { pill = b; break; }
            }
            if (pill != null) Instantiate(pill.gameObject, container, false).name = name + "Backdrop";

            var label = Instantiate(toastText.gameObject, container, false).GetComponent<TMP_Text>();
            label.name = name + "Text";
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = container.sizeDelta;
            label.fontSize = toastText.fontSize * fontScale;
            label.text = "";
            label.gameObject.SetActive(true);
            if (pill != null) container.GetComponentInChildren<TextBackdrop>().Target = label;
            go.SetActive(false);
            return label;
        }

        /// <summary>Shows the current control hint: a new hint waits for the old one to fade out, then fades in.</summary>
        void UpdateHint()
        {
            string want = _hints.Tick(Time.deltaTime);
            float target = 0f;
            if (want == _hintRaw) target = want != null ? 1f : 0f;
            else if (_hintGroup.alpha <= 0f) _hintRaw = want; // swap while invisible
            _hintText.text = _hintRaw != null ? InputPrompts.Format(_hintRaw) : ""; // the device can change at any time
            _hintGroup.alpha = Mathf.MoveTowards(_hintGroup.alpha, target, Time.unscaledDeltaTime * 5f);
        }

        void OnAnnouncement(string msg, float seconds)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _announceText.text = msg;
            _announceSeconds = _announceTimer = Mathf.Max(0.5f, seconds);
            _announce.gameObject.SetActive(true);
            UpdateAnnouncement();
        }

        /// <summary>Pops in (scale 1.25 -> 1), holds, fades out over the last 0.4 s.</summary>
        void UpdateAnnouncement()
        {
            if (_announce == null || !_announce.gameObject.activeSelf) return;
            _announceTimer -= Time.deltaTime;
            if (_announceTimer <= 0f) { _announce.gameObject.SetActive(false); return; }
            float shown = _announceSeconds - _announceTimer;
            float pop = Mathf.Clamp01(shown / 0.25f);
            _announce.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, 1f - (1f - pop) * (1f - pop));
            _announceGroup.alpha = Mathf.Min(Mathf.Clamp01(shown / 0.12f), Mathf.Clamp01(_announceTimer / 0.4f));
        }

        void OnEnable()
        {
            GameEvents.Toast += OnToast;
            GameEvents.Announcement += OnAnnouncement;
            GameEvents.FestivalInRange += OnFestival;
            GameEvents.PhotoTaken += OnPhoto;
            GameEvents.Jumped += _hints.OnJumped;
            GameEvents.CrowdBounced += _hints.OnCrowdBounced;
            toastText.gameObject.SetActive(false);
            promptText.gameObject.SetActive(false);
            SetFlash(0f);
        }

        void OnDisable()
        {
            GameEvents.Toast -= OnToast;
            GameEvents.Announcement -= OnAnnouncement;
            GameEvents.FestivalInRange -= OnFestival;
            GameEvents.PhotoTaken -= OnPhoto;
            GameEvents.Jumped -= _hints.OnJumped;
            GameEvents.CrowdBounced -= _hints.OnCrowdBounced;
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
            UpdateAnnouncement();
            UpdateHint();
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
            milesText.text = $"Miles: {SaveSystem.Data.miles}   Passports: {root.countries.CountOwned(SaveSystem.Data)}/{root.countries.ActiveDestinations.Count}";

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

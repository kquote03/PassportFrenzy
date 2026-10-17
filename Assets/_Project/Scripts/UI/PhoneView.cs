using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// The phone frame that slides in for "menu hell" apps. Hosts BookingApp, ResidencyApp and ShopApp,
    /// plus a shared countdown bar the apps drive through <see cref="SetTimer"/>.
    /// </summary>
    public class PhoneView : MonoBehaviour
    {
        public BookingApp booking;
        public ResidencyApp residency;
        public ShopApp shop;

        [SerializeField] RectTransform frame;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text timerText;
        [SerializeField] Image timerFill;
        [SerializeField] TMP_Text waitingText;
        [SerializeField] Vector2 shownPosition = new Vector2(-40f, 0f);
        [SerializeField] Vector2 hiddenPosition = new Vector2(760f, 0f);
        [SerializeField] Color timerColor = new Color(0.2f, 0.75f, 1f);
        [SerializeField] Color timerUrgentColor = new Color(1f, 0.3f, 0.25f);

        bool _open;

        public bool IsOpen => _open;

        void Awake()
        {
            frame.anchoredPosition = hiddenPosition;
            frame.gameObject.SetActive(false);
        }

        void HideApps()
        {
            booking.gameObject.SetActive(false);
            residency.gameObject.SetActive(false);
            shop.gameObject.SetActive(false);
            waitingText.gameObject.SetActive(false);
        }

        public void OpenApp(GameObject app, string title)
        {
            if (!_open) GameRoot.I.audioManager.Play(SoundIds.PhoneOpen);
            _open = true;
            frame.gameObject.SetActive(true);
            HideApps();
            app.SetActive(true);
            titleText.text = title;
            SetTimerVisible(true);
            UIRoot.Select(null);
        }

        public void ShowWaiting(string message)
        {
            _open = true;
            frame.gameObject.SetActive(true);
            HideApps();
            waitingText.gameObject.SetActive(true);
            waitingText.text = message;
            SetTimerVisible(false);
            UIRoot.Select(null);
        }

        public void SetTimer(float normalized, float secondsLeft)
        {
            timerFill.fillAmount = Mathf.Clamp01(normalized);
            timerFill.color = normalized < 0.3f ? timerUrgentColor : timerColor;
            timerText.text = $"{Mathf.Max(0f, secondsLeft):0.0}s";
        }

        void SetTimerVisible(bool on)
        {
            timerFill.transform.parent.gameObject.SetActive(on);
            timerText.gameObject.SetActive(on);
        }

        public void Close()
        {
            _open = false;
            UIRoot.Select(null);
        }

        void Update()
        {
            if (!frame.gameObject.activeSelf) return;
            Vector2 goal = _open ? shownPosition : hiddenPosition;
            frame.anchoredPosition = Vector2.Lerp(frame.anchoredPosition, goal, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            if (!_open && (frame.anchoredPosition - hiddenPosition).sqrMagnitude < 4f)
            {
                HideApps();
                frame.gameObject.SetActive(false);
            }
        }
    }
}

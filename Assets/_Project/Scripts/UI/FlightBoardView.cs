using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// Departure board: the "pursuer". Gate countdown plus a plane icon taxiing toward takeoff as time drains,
    /// and the distance to the gate during the chase.
    /// </summary>
    public class FlightBoardView : MonoBehaviour
    {
        [SerializeField] GameObject board;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] TMP_Text distanceText;
        [SerializeField] RectTransform track;
        [SerializeField] RectTransform planeIcon;
        [SerializeField] RectTransform runnerIcon;
        [SerializeField] Image background;
        [SerializeField] Color normalColor = new Color(0.08f, 0.1f, 0.18f, 0.92f);
        [SerializeField] Color urgentColor = new Color(0.55f, 0.08f, 0.08f, 0.92f);
        [SerializeField] float urgentSeconds = 10f;

        void Update()
        {
            var root = GameRoot.I;
            var run = root.flow.Run;
            bool show = run != null && run.FlightTimerActive && run.Destination != null;
            board.SetActive(show);
            if (!show) return;

            float remaining = run.FlightSecondsRemaining;
            titleText.text = $"FLIGHT TO {run.Destination.displayName.ToUpperInvariant()}";
            countdownText.text = $"Gate closes {TimeFormat.Clock(remaining)}";
            bool urgent = remaining < urgentSeconds;
            background.color = urgent ? Color.Lerp(normalColor, urgentColor, Mathf.PingPong(Time.time * 3f, 1f)) : normalColor;

            float width = track.rect.width;
            float planeT = 1f - remaining / Mathf.Max(0.01f, run.FlightSecondsTotal);
            planeIcon.anchoredPosition = new Vector2(Mathf.Lerp(0f, width, planeT), planeIcon.anchoredPosition.y);

            var spawner = root.spawner;
            // The terminal chase starts right after booking; residency and shop run on the phone during it.
            if (spawner.Mode == SpawnMode.Chase && !float.IsNaN(spawner.ChaseStartX))
            {
                float px = root.player.transform.position.x;
                float gate = spawner.PlannedGateX;
                float total = Mathf.Max(1f, gate - spawner.ChaseStartX);
                float dist = Mathf.Max(0f, gate - px);
                distanceText.text = $"Gate in {Mathf.CeilToInt(dist)} m";
                runnerIcon.gameObject.SetActive(true);
                runnerIcon.anchoredPosition = new Vector2(Mathf.Lerp(0f, width, Mathf.Clamp01(1f - dist / total)), runnerIcon.anchoredPosition.y);
            }
            else
            {
                distanceText.text = "Book your flight!";
                runnerIcon.gameObject.SetActive(false);
            }
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>Win screen: every destination passport collected (lifetime).</summary>
    public class WinView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Button homeButton;
        [SerializeField] Button resetButton;

        void Awake()
        {
            homeButton.onClick.AddListener(() => GameRoot.I.flow.EnterHome());
            resetButton.onClick.AddListener(() => { SaveSystem.Clear(); GameRoot.I.flow.EnterHome(); });
        }

        public void Show(RunState run)
        {
            panel.SetActive(true);
            var d = SaveSystem.Data;
            int total = Mathf.RoundToInt(d.secondsPlayed);
            statsText.text =
                $"Passports: {d.passports.Count}/{GameRoot.I.countries.destinations.Count}\n" +
                $"Miles: {d.miles}\n" +
                $"Countries visited: {d.countriesVisited}\n" +
                $"Photos taken: {d.photosTaken}\n" +
                $"Flights: {d.flightsTaken} taken, {d.flightsMissed} missed\n" +
                $"Time played: {total / 3600}h {total / 60 % 60:00}m {total % 60:00}s";
            UIRoot.Select(homeButton.gameObject);
        }

        public void Hide() => panel.SetActive(false);
    }
}

using System.Collections;
using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>"Flight missed" screen. The plane leaves; the player keeps passports and miles.</summary>
    public class MissedView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] RectTransform plane;

        public IEnumerator Run(string destination, RunState run, float seconds)
        {
            panel.SetActive(true);
            titleText.text = "FLIGHT MISSED!";
            bodyText.text = $"The plane to {destination} left without you.\n\n" +
                            $"This run: {run.CountriesVisited} countries, {run.Photos.Count} photos, +{run.MilesEarned} miles\n" +
                            (run.MilesLostToOverstay > 0 ? $"Visa overstays cost you {run.MilesLostToOverstay} miles\n" : "") +
                            $"Kept: {SaveSystem.Data.passports.Count} passports, {SaveSystem.Data.miles} miles\n\n" +
                            InputPrompts.Format("{Confirm} Back to the home airport");
            float t = 0f;
            var input = GameRoot.I.input;
            while (t < seconds)
            {
                t += Time.deltaTime;
                plane.anchoredPosition = new Vector2(t * 420f - 300f, t * 140f);
                if (t > 1f && (input.Submit.WasPressedThisFrame() || input.Mash.WasPressedThisFrame())) break;
                yield return null;
            }
            panel.SetActive(false);
        }

        public void Hide() => panel.SetActive(false);
    }
}

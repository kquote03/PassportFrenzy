using System.Collections;
using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Full-screen "flying to..." card between countries.</summary>
    public class FlightTransitionView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text milesText;
        [SerializeField] RectTransform plane;
        [SerializeField] float planeTravel = 1400f;

        public IEnumerator Play(CountryDefinition from, CountryDefinition to, int miles, float seconds)
        {
            panel.SetActive(true);
            titleText.text = $"{from.displayName}  >>>  {to.displayName}";
            milesText.text = $"+{miles} miles  (total {SaveSystem.Data.miles})";
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float k = t / seconds;
                plane.anchoredPosition = new Vector2(Mathf.Lerp(-planeTravel * 0.5f, planeTravel * 0.5f, k), Mathf.Sin(k * Mathf.PI) * 60f);
                yield return null;
            }
        }

        public void Hide() => panel.SetActive(false);
    }
}

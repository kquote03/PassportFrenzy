using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Airline booking app: pick one of up to 3 destinations before the timer runs out, or get a random one.</summary>
    public class BookingApp : MonoBehaviour
    {
        [SerializeField] PhoneView phone;
        [SerializeField] BookingCardView[] cards;
        [SerializeField] TMP_Text hintText;

        public IEnumerator Run(List<BookingOption> options, float window, Action<BookingOption> onPicked)
        {
            phone.OpenApp(gameObject, "SkyBook - Where next?");
            float spy = GameRoot.I.tuning.SecondsPerYear;
            int? chosen = null;

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < options.Count;
                cards[i].gameObject.SetActive(used);
                if (!used) continue;
                int index = i;
                cards[i].Bind(options[i], spy, SaveSystem.Data.Owns(options[i].country.id), () => chosen ??= index);
            }
            hintText.text = InputPrompts.Format("Pick fast! {Submit} to book");
            yield return null; // let layout settle before selecting
            UIRoot.Select(cards[0].gameObject);

            float t = window;
            while (chosen == null && t > 0f)
            {
                t -= Time.deltaTime;
                phone.SetTimer(t / window, t);
                yield return null;
            }

            int pick = chosen ?? UnityEngine.Random.Range(0, options.Count);
            if (chosen == null) GameEvents.RaiseToast($"Too slow! The app booked {options[pick].country.displayName} for you");
            for (int i = 0; i < options.Count; i++) cards[i].SetResult(i == pick);
            hintText.text = $"Booked: {options[pick].country.displayName}!";
            UIRoot.Select(null);
            yield return new WaitForSeconds(0.7f);
            onPicked(options[pick]);
        }
    }
}

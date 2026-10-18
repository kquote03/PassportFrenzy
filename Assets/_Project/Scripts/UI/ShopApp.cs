using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>Freezone duty-free: buy at most one buff with miles before the boarding call.</summary>
    public class ShopApp : MonoBehaviour
    {
        [SerializeField] PhoneView phone;
        [SerializeField] ShopItemView[] items;
        [SerializeField] Button skipButton;
        [SerializeField] TMP_Text milesText;
        [SerializeField] TMP_Text infoText;
        [Tooltip("Seconds before the first item is selected for the controller (mouse works immediately).")]
        [SerializeField] float selectDelay = 0.1f;

        bool _done;

        void Awake() => skipButton.onClick.AddListener(() => _done = true);

        public IEnumerator Run(float window)
        {
            var root = GameRoot.I;
            phone.OpenApp(gameObject, "Freezone Duty Free");
            _done = false;

            for (int i = 0; i < items.Length; i++)
            {
                bool used = i < root.shopBuffs.Count && root.shopBuffs[i] != null;
                items[i].gameObject.SetActive(used);
                if (!used) continue;
                var buff = root.shopBuffs[i];
                items[i].Bind(buff, () => { if (TryBuy(buff)) _done = true; });
            }
            var held = root.buffs.Held;
            infoText.text = held != null ? $"Holding: {held.displayName} (1 item max)" : "One item per trip. Choose wisely!";
            float t = window;
            bool selected = false;
            while (!_done && t > 0f)
            {
                // Short grace period so leftover RT mash presses can't buy the first item by accident.
                if ((!selected && window - t >= selectDelay) || (selected && UIRoot.NothingSelected))
                {
                    UIRoot.Select(items.Length > 0 && items[0].gameObject.activeSelf ? items[0].gameObject : skipButton.gameObject);
                    selected = true;
                }
                milesText.text = $"Miles: {SaveSystem.Data.miles}";
                foreach (var it in items) if (it.gameObject.activeSelf) it.Refresh(SaveSystem.Data.miles);
                t -= Time.deltaTime;
                phone.SetTimer(t / window, t);
                yield return null;
            }
            if (!_done) GameEvents.RaiseToast("Final boarding call! Shop closed");
            UIRoot.Select(null);
        }

        bool TryBuy(BuffDefinition b)
        {
            var root = GameRoot.I;
            if (SaveSystem.Data.miles < b.cost) { GameEvents.RaiseToast($"Not enough miles for {b.displayName}"); return false; }
            if (!root.buffs.CanAcquire(b, out string reason)) { GameEvents.RaiseToast(reason); return false; }
            SaveSystem.AddMiles(-b.cost);
            root.buffs.Acquire(b);
            root.audioManager.Play(SoundIds.Buy);
            return true;
        }
    }
}

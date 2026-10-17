using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PassportFrenzy
{
    /// <summary>
    /// Residency application (menu hell):
    /// 1) mash to fill in the form within a random window,
    /// 2) attach N festival photos from the tray to the paper (mouse: drag; controller: flick stick right).
    /// Fails on timeout or when the player runs out of photos.
    /// </summary>
    public class ResidencyApp : MonoBehaviour
    {
        [SerializeField] PhoneView phone;

        [Header("Mash phase")]
        [SerializeField] GameObject mashGroup;
        [SerializeField] Image mashFill;
        [SerializeField] TMP_Text mashText;
        [SerializeField] Button mashButton;

        [Header("Photo phase")]
        [SerializeField] GameObject photoGroup;
        [SerializeField] RectTransform tray;
        [SerializeField] RectTransform paper;
        [SerializeField] RectTransform[] paperSlots;
        [SerializeField] PhotoDragItem photoPrefab;
        [SerializeField] TMP_Text photoText;
        [SerializeField] int maxTrayPhotos = 5;

        [Header("Result")]
        [SerializeField] TMP_Text stampText;

        readonly List<PhotoDragItem> _items = new List<PhotoDragItem>();
        int _clicks, _attached;

        public RectTransform DragLayer => (RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform;

        void Awake() => mashButton.onClick.AddListener(() => _clicks++);

        public IEnumerator Run(CountryDefinition country, List<PhotoRecord> photos, Action<bool> done)
        {
            var T = GameRoot.I.tuning;
            var input = GameRoot.I.input;
            phone.OpenApp(gameObject, $"Residency: {country.displayName}");
            stampText.gameObject.SetActive(false);
            ClearItems();

            // ---- Phase 1: mash
            mashGroup.SetActive(true);
            photoGroup.SetActive(false);
            int target = UnityEngine.Random.Range(T.mashPressesRange.x, T.mashPressesRange.y + 1);
            float window = UnityEngine.Random.Range(T.mashWindowSeconds.x, T.mashWindowSeconds.y);
            int presses = 0;
            _clicks = 0;
            float t = window;
            while (presses < target && t > 0f)
            {
                if (input.Mash.WasPressedThisFrame()) { presses++; GameRoot.I.audioManager.Play(SoundIds.Mash); }
                presses += _clicks;
                _clicks = 0;
                mashFill.fillAmount = presses / (float)target;
                mashText.text = InputPrompts.Format($"Fill in the form!\nMash {{Mash}} or click\n<size=150%>{Mathf.Min(presses, target)}/{target}</size>");
                t -= Time.deltaTime;
                phone.SetTimer(t / window, t);
                yield return null;
            }
            if (presses < target)
            {
                yield return ShowResult(false, "TOO SLOW!\nForm incomplete");
                done(false);
                yield break;
            }

            // ---- Phase 2: photos
            mashGroup.SetActive(false);
            photoGroup.SetActive(true);
            int required = T.photosRequired;
            _attached = 0;
            for (int i = 0; i < photos.Count && i < maxTrayPhotos; i++)
            {
                var item = Instantiate(photoPrefab, tray);
                item.Init(this, photos[i].festivalName);
                _items.Add(item);
            }
            float pt = T.photoAttachSeconds;
            bool flickArmed = true;
            while (_attached < required && pt > 0f)
            {
                photoText.text = InputPrompts.Format($"Proof of integration: {_attached}/{required} photos\n{{Attach}}");
                if (_attached >= _items.Count)
                {
                    yield return new WaitForSeconds(0.4f);
                    yield return ShowResult(false, $"NOT ENOUGH PHOTOS\n{photos.Count}/{required} from {country.displayName} festivals");
                    done(false);
                    yield break;
                }

                // Controller / keyboard: flick the stick from the tray (left) toward the paper (right).
                float x = input.Navigate.ReadValue<Vector2>().x;
                if (flickArmed && x > 0.7f) { AttachNext(); flickArmed = false; }
                else if (x < 0.3f) flickArmed = true;

                pt -= Time.deltaTime;
                phone.SetTimer(pt / T.photoAttachSeconds, pt);
                yield return null;
            }

            bool ok = _attached >= required;
            yield return ShowResult(ok, ok ? "APPROVED" : "TOO SLOW!\nApplication timed out");
            done(ok);
        }

        IEnumerator ShowResult(bool ok, string message)
        {
            stampText.gameObject.SetActive(true);
            stampText.text = message;
            stampText.color = ok ? new Color(0.1f, 0.6f, 0.15f) : new Color(0.85f, 0.1f, 0.1f);
            stampText.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-14f, 14f));
            GameRoot.I.audioManager.Play(ok ? SoundIds.Stamp : SoundIds.Rejected);
            if (GameRoot.I.Cam != null) GameRoot.I.Cam.Shake(0.12f);
            yield return new WaitForSeconds(GameRoot.I.tuning.resultDisplaySeconds);
        }

        void AttachNext()
        {
            foreach (var item in _items)
                if (!item.Attached) { Attach(item); return; }
        }

        public bool IsOverPaper(Vector2 screenPosition) =>
            RectTransformUtility.RectangleContainsScreenPoint(paper, screenPosition, null);

        public void Attach(PhotoDragItem item)
        {
            if (item.Attached || _attached >= paperSlots.Length) { item.ReturnToTray(tray); return; }
            item.AttachTo(paperSlots[_attached]);
            _attached++;
            GameRoot.I.audioManager.Play(SoundIds.Mash);
        }

        public void ReturnToTray(PhotoDragItem item) => item.ReturnToTray(tray);

        void ClearItems()
        {
            foreach (var i in _items) if (i != null) Destroy(i.gameObject);
            _items.Clear();
        }

        void OnDisable() => ClearItems();
    }
}

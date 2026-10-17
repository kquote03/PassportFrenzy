using TMPro;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Festival trigger area. Inside it, a prompt appears; pressing Photo captures one photo
    /// tagged with the current country. One photo per festival instance.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FestivalZone : MonoBehaviour
    {
        [Tooltip("Leave empty to pick a random name from the country's festivalNames.")]
        [SerializeField] string festivalNameOverride;
        [Tooltip("Optional world-space label (child 'Label').")]
        [SerializeField] TMP_Text label;

        public string FestivalName { get; private set; } = "Festival";
        public bool Photographed { get; private set; }

        public void SetCountry(CountryDefinition c)
        {
            FestivalName = !string.IsNullOrEmpty(festivalNameOverride) ? festivalNameOverride
                : c != null ? c.RandomFestivalName() : "Festival";
            RefreshLabel();
        }

        /// <summary>Set by the festival planner so each planned festival in a visit has a distinct name.</summary>
        public void SetFestivalName(string festivalName)
        {
            if (string.IsNullOrEmpty(festivalName)) return;
            FestivalName = festivalName;
            RefreshLabel();
        }

        /// <summary>Name forced by the prefab (signature festivals like "La Tomatina"), or null.</summary>
        public string NameOverride => string.IsNullOrEmpty(festivalNameOverride) ? null : festivalNameOverride;

        public void TakePhoto(RunState run)
        {
            if (Photographed) return;
            Photographed = true;
            var record = new PhotoRecord(run.Current != null ? run.Current.id : "", FestivalName);
            run.Photos.Add(record);
            SaveSystem.Data.photosTaken++;
            SaveSystem.Save();
            GameRoot.I.audioManager.Play(SoundIds.Photo);
            GameEvents.RaisePhotoTaken(record);
            GameEvents.RaiseToast($"Photo taken: {FestivalName}!");
            GameEvents.RaiseFestivalInRange(null);
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (label != null) label.text = Photographed ? FestivalName + "\n<size=60%>PHOTO TAKEN</size>" : FestivalName;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = Interactions(other);
            if (p != null && !Photographed) p.EnterFestival(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var p = Interactions(other);
            if (p != null) p.ExitFestival(this);
        }

        static PlayerInteractions Interactions(Collider2D c) =>
            c.attachedRigidbody != null ? c.attachedRigidbody.GetComponent<PlayerInteractions>() : null;
    }
}

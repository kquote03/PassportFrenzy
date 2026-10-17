using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Photo + Use Item buttons. Tracks which festival zone the player is standing in.</summary>
    public class PlayerInteractions : MonoBehaviour
    {
        FestivalZone _zone;

        public FestivalZone CurrentZone => _zone;

        public void EnterFestival(FestivalZone zone)
        {
            _zone = zone;
            GameEvents.RaiseFestivalInRange(zone);
        }

        public void ExitFestival(FestivalZone zone)
        {
            if (_zone != zone) return;
            _zone = null;
            GameEvents.RaiseFestivalInRange(null);
        }

        public void ClearZone()
        {
            _zone = null;
            GameEvents.RaiseFestivalInRange(null);
        }

        void Update()
        {
            var root = GameRoot.I;
            if (root.input.MenuMode) return;

            if (root.input.Photo.WasPressedThisFrame())
            {
                if (_zone != null && !_zone.Photographed && root.flow.Run != null)
                {
                    _zone.TakePhoto(root.flow.Run);
                }
                else
                {
                    root.audioManager.Play(SoundIds.Photo);
                    GameEvents.RaiseToast(_zone != null ? "Already photographed this festival" : "Nothing worth a photo here");
                }
            }

            if (root.input.UseItem.WasPressedThisFrame())
                root.buffs.UseHeld();
        }
    }
}

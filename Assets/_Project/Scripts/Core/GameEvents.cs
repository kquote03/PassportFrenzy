using System;

namespace PassportFrenzy
{
    public enum GameState
    {
        Home,          // main menu at the home airport, player idle
        Departing,     // pressed Play: the run-in intro, or running to the home airport door
        Booking,       // phone: booking app
        Residency,     // phone: residency application
        Shop,          // phone: freezone shop
        Chase,         // airport chase to the gate
        Flying,        // flight transition screen
        CountryRun,    // running through a country (visa timer)
        ToAirport,     // visa expired, running into the airport
        Missed,        // flight missed screen
        Won,           // win screen
    }

    /// <summary>Global fire-and-forget events for UI / audio / feedback. Gameplay logic should call systems directly.</summary>
    public static class GameEvents
    {
        public static event Action<GameState> StateChanged;
        public static event Action<string> Toast;
        public static event Action<string, float> Announcement; // big banner text, seconds on screen
        public static event Action<PhotoRecord> PhotoTaken;
        public static event Action<HitKind, bool> PlayerHit; // kind, absorbedByShield
        public static event Action ItemChanged;
        public static event Action<string> PassportGranted;
        public static event Action MilesChanged;
        public static event Action<FestivalZone> FestivalInRange; // null = left zone
        public static event Action<bool> Jumped;      // true = the player pressed jump (not an automatic hop)
        public static event Action CrowdBounced;

        // Project has "Enter Play Mode: no domain reload", so statics must be cleared by hand.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            StateChanged = null; Toast = null; Announcement = null; PhotoTaken = null; PlayerHit = null;
            ItemChanged = null; PassportGranted = null; MilesChanged = null; FestivalInRange = null;
            Jumped = null; CrowdBounced = null;
        }

        public static void RaiseStateChanged(GameState s) => StateChanged?.Invoke(s);
        public static void RaiseToast(string msg) => Toast?.Invoke(msg);
        public static void RaiseAnnouncement(string msg, float seconds) => Announcement?.Invoke(msg, seconds);
        public static void RaisePhotoTaken(PhotoRecord p) => PhotoTaken?.Invoke(p);
        public static void RaisePlayerHit(HitKind k, bool absorbed) => PlayerHit?.Invoke(k, absorbed);
        public static void RaiseItemChanged() => ItemChanged?.Invoke();
        public static void RaisePassportGranted(string id) => PassportGranted?.Invoke(id);
        public static void RaiseMilesChanged() => MilesChanged?.Invoke();
        public static void RaiseFestivalInRange(FestivalZone z) => FestivalInRange?.Invoke(z);
        public static void RaiseJumped(bool manual) => Jumped?.Invoke(manual);
        public static void RaiseCrowdBounced() => CrowdBounced?.Invoke();
    }

    public enum HitKind { Slow, Block }
}

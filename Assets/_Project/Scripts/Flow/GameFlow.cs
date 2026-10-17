using System.Collections;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// The game loop:
    /// Home -> Departing -> [Booking -> Residency? -> Shop] -> Chase -> Flying -> CountryRun -> ToAirport -> [Booking ...]
    /// Missing the flight (flight timer hits 0) -> Missed -> Home. Owning every destination passport -> Won.
    /// The bracketed airport part is one coroutine (<see cref="AirportSequence"/>).
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [SerializeField] Vector2 playerStart = new Vector2(0f, 1f);

        readonly System.Random _rng = new System.Random();
        Coroutine _sequence;

        public GameState State { get; private set; }
        public RunState Run { get; private set; }

        GameRoot R => GameRoot.I;
        GameTuning T => R.tuning;

        void Start() => EnterHome();

        void SetState(GameState s)
        {
            Debug.Log($"[Flow] {State} -> {s}");
            State = s;
            GameEvents.RaiseStateChanged(s);
        }

        // ------------------------------------------------------------------ Home / main menu

        public void EnterHome()
        {
            StopSequence();
            Run = null;
            var home = R.countries.home;
            R.ui.HideOverlays();
            R.ui.phone.Close();
            R.spawner.ResetWorld(SpawnMode.Home, home, 1, R.countries.homeLobbyChunk);
            R.player.ResetTo(playerStart);
            R.player.SetRunning(false);
            R.player.ClearRunBuffs();
            R.Cam.SnapToTarget();
            R.Cam.SetBackground(home.skyColor);
            R.input.SetMenuMode(true);
            R.audioManager.PlayMusic(home);
            SaveSystem.Save();
            SetState(GameState.Home);
            R.ui.mainMenu.Show();
        }

        /// <summary>Main menu Play button: run through the home airport door.</summary>
        public void StartRun()
        {
            if (State != GameState.Home) return;
            R.ui.mainMenu.Hide();
            Run = new RunState(R.countries.home);
            R.input.SetMenuMode(false);
            R.spawner.TrimAhead(18f);
            R.spawner.SetMode(SpawnMode.AirportSafe);
            R.spawner.EnqueueOnce(R.countries.homeDoorChunk);
            R.player.SetRunning(true);
            SetState(GameState.Departing);
        }

        // ------------------------------------------------------------------ Per-frame timers

        void Update()
        {
            if (Run == null) return;
            float dt = Time.deltaTime;
            Run.RunSeconds += dt;
            SaveSystem.Data.secondsPlayed += dt;

            if (State is GameState.CountryRun or GameState.ToAirport or GameState.Booking or GameState.Residency
                or GameState.Shop or GameState.Chase)
                Run.AddTimeInCurrent(dt);

            if (State == GameState.CountryRun)
            {
                Run.VisaSecondsRemaining -= dt;
                if (Run.VisaSecondsRemaining <= 0f) BeginAirportApproach();
            }

            if (Run.FlightTimerActive)
            {
                Run.FlightSecondsRemaining -= dt;
                if (Run.FlightSecondsRemaining <= 0f) MissFlight();
            }
        }

        // ------------------------------------------------------------------ Triggers from the world

        public void OnFlowTrigger(FlowTriggerId id)
        {
            switch (id)
            {
                case FlowTriggerId.HomeDoor when State == GameState.Departing:
                case FlowTriggerId.AirportEntrance when State == GameState.ToAirport:
                    _sequence = StartCoroutine(AirportSequence());
                    break;
                case FlowTriggerId.Gate when State == GameState.Chase:
                    _sequence = StartCoroutine(FlySequence());
                    break;
            }
        }

        void BeginAirportApproach()
        {
            Run.VisaSecondsRemaining = 0f;
            SetState(GameState.ToAirport);
            R.spawner.TrimAhead(18f);
            R.spawner.SetMode(SpawnMode.AirportSafe);
            R.spawner.EnqueueOnce(R.countries.airportEntranceChunk);
            GameEvents.RaiseToast("Visa expiring! Head to the airport!");
        }

        // ------------------------------------------------------------------ Airport: booking -> residency -> shop -> chase

        IEnumerator AirportSequence()
        {
            var here = Run.Current;
            R.input.SetMenuMode(true);
            R.player.GetComponent<PlayerInteractions>().ClearZone();

            // 1. Booking (menu hell: pick within the window or get a random one)
            SetState(GameState.Booking);
            var options = BookingRules.PickOptions(R.countries.destinations, SaveSystem.Data.passports,
                here.isHome ? null : here, T.bookingChoices, _rng);
            if (options.Count == 0) { Win(); yield break; }

            BookingOption choice = default;
            float window = Random.Range(T.bookingWindowSeconds.x, T.bookingWindowSeconds.y);
            yield return R.ui.phone.booking.Run(options, window, o => choice = o);
            Run.Destination = choice.country;
            Run.DestinationVisaYears = choice.visaYears;
            R.audioManager.Play(SoundIds.Booking);

            bool owned = SaveSystem.Data.Owns(here.id);
            bool eligible = !here.isHome && !owned && Run.IsResidencyEligible(here, T.SecondsPerYear);
            StartFlightTimer(eligible);

            // 2. Residency application (foreign airports only, once per visit)
            if (!here.isHome && !owned)
            {
                if (eligible)
                {
                    R.ui.phone.ShowWaiting("Checking your residency status...");
                    yield return new WaitForSeconds(T.residencyDelayAfterBooking);
                    SetState(GameState.Residency);
                    bool success = false;
                    yield return R.ui.phone.residency.Run(here, Run.PhotosFor(here.id), ok => success = ok);
                    if (success)
                    {
                        SaveSystem.GrantPassport(here.id);
                        GameEvents.RaiseToast($"{here.displayName} residency granted! ({SaveSystem.Data.passports.Count}/{R.countries.destinations.Count})");
                        if (OwnsAllPassports()) { Win(); yield break; }
                    }
                }
                else
                {
                    GameEvents.RaiseToast($"{here.displayName} residency: {Run.YearsIn(here.id, T.SecondsPerYear):0.0}/{here.requiredResidencyYears} years. Not eligible yet");
                }
            }

            // 3. Freezone shop
            SetState(GameState.Shop);
            R.spawner.EnqueueOnce(R.countries.freezoneChunk);
            yield return R.ui.phone.shop.Run(T.shopWindowSeconds);
            R.ui.phone.Close();

            // 4. Chase to the gate
            R.input.SetMenuMode(false);
            int tier = Run.Destination.difficultyTier;
            R.spawner.BeginChase(here, tier, T.TerminalLength(tier));
            SetState(GameState.Chase);
            GameEvents.RaiseToast($"Run to the gate! Flight to {Run.Destination.displayName}");
            _sequence = null;
        }

        void StartFlightTimer(bool residencyAhead)
        {
            int tier = Run.Destination.difficultyTier;
            float menus = T.shopWindowSeconds;
            if (residencyAhead)
                menus += T.residencyDelayAfterBooking + T.mashWindowSeconds.y + T.photoAttachSeconds + T.resultDisplaySeconds * 2f;
            float fillerRun = T.spawnAheadDistance / T.baseRunSpeed; // safe chunks already spawned ahead of the chase
            float total = menus + fillerRun + T.ExpectedChaseSeconds(tier) + T.flightTimerPaddingSeconds;
            Run.FlightSecondsTotal = Run.FlightSecondsRemaining = total;
            Run.FlightTimerActive = true;
        }

        // ------------------------------------------------------------------ Flight / landing

        IEnumerator FlySequence()
        {
            SetState(GameState.Flying);
            Run.FlightTimerActive = false;
            R.input.SetMenuMode(true);
            R.player.SetRunning(false);

            int miles = BookingRules.MilesForFlight(T, Run.SecondsThisStay);
            Run.MilesEarned += miles;
            SaveSystem.Data.flightsTaken++;
            SaveSystem.AddMiles(miles);
            R.audioManager.Play(SoundIds.Takeoff);

            yield return R.ui.flightTransition.Play(Run.Current, Run.Destination, miles, T.flyingScreenSeconds);
            Land(Run.Destination, Run.DestinationVisaYears);
            _sequence = null;
        }

        void Land(CountryDefinition c, int visaYears)
        {
            Run.Current = c;
            Run.Destination = null;
            Run.SecondsThisStay = 0f;
            Run.CountriesVisited++;
            SaveSystem.Data.countriesVisited++;
            SaveSystem.Save();
            Run.VisaSecondsTotal = Run.VisaSecondsRemaining = T.CountryRunSeconds(visaYears);

            R.spawner.ResetWorld(SpawnMode.Country, c, c.difficultyTier, R.countries.arrivalChunk);
            R.player.ResetTo(playerStart);
            R.player.SetRunning(true);
            R.Cam.SnapToTarget();
            R.Cam.SetBackground(c.skyColor);
            R.audioManager.PlayMusic(c);
            R.ui.flightTransition.Hide();
            R.input.SetMenuMode(false);
            SetState(GameState.CountryRun);
            GameEvents.RaiseToast($"Welcome to {c.displayName}! Visa: {TimeFormat.Years(visaYears, T.SecondsPerYear)}");
        }

        // ------------------------------------------------------------------ Lose / win

        void MissFlight()
        {
            Debug.Log($"[Flow] Flight missed in state {State}, {R.spawner.PlannedGateX - R.player.transform.position.x:0} m short of the gate");
            StopSequence();
            Run.FlightTimerActive = false;
            Run.FlightSecondsRemaining = 0f;
            SetState(GameState.Missed);
            R.ui.phone.Close();
            R.input.SetMenuMode(true);
            R.player.SetRunning(false);
            R.audioManager.Play(SoundIds.FlightMissed);
            SaveSystem.Data.flightsMissed++;
            SaveSystem.Save();
            _sequence = StartCoroutine(MissedSequence());
        }

        IEnumerator MissedSequence()
        {
            string dest = Run.Destination != null ? Run.Destination.displayName : "your destination";
            yield return R.ui.missed.Run(dest, Run, T.missedScreenSeconds);
            _sequence = null;
            EnterHome();
        }

        public bool OwnsAllPassports()
        {
            foreach (var c in R.countries.destinations)
                if (c != null && !SaveSystem.Data.Owns(c.id)) return false;
            return true;
        }

        public void Win()
        {
            StopSequence();
            if (Run != null) Run.FlightTimerActive = false;
            SetState(GameState.Won);
            R.ui.phone.Close();
            R.input.SetMenuMode(true);
            R.player.SetRunning(false);
            R.audioManager.Play(SoundIds.Win);
            SaveSystem.Save();
            R.ui.win.Show(Run);
        }

        void StopSequence()
        {
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = null;
        }

        // ------------------------------------------------------------------ Debug hooks (DebugPanel)

        public void DebugSkipTime(float seconds)
        {
            if (Run == null) return;
            Run.AddTimeInCurrent(seconds);
            if (State == GameState.CountryRun) Run.VisaSecondsRemaining -= seconds;
        }

        public void DebugAddYear() => Run?.AddTimeInCurrent(T.SecondsPerYear);

        public void DebugForceAirport()
        {
            if (State == GameState.CountryRun) BeginAirportApproach();
        }

        public void DebugGrantPhotos(int count)
        {
            if (Run?.Current == null) return;
            for (int i = 0; i < count; i++) Run.Photos.Add(new PhotoRecord(Run.Current.id, "Debug Festival " + (i + 1)));
            GameEvents.RaiseToast($"+{count} photos ({Run.PhotoCount(Run.Current.id)} in {Run.Current.displayName})");
        }

        public void DebugWin()
        {
            foreach (var c in R.countries.destinations) if (c != null) SaveSystem.GrantPassport(c.id);
            Win();
        }
    }
}

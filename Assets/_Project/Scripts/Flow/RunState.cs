using System;
using System.Collections.Generic;

namespace PassportFrenzy
{
    [Serializable]
    public struct PhotoRecord
    {
        public string countryId;
        public string festivalName;

        public PhotoRecord(string countryId, string festivalName)
        {
            this.countryId = countryId;
            this.festivalName = festivalName;
        }
    }

    public struct BookingOption
    {
        public CountryDefinition country;
        public int visaYears;
    }

    /// <summary>Everything about the current run. Plain C# so it is unit-testable. Lost when the run ends.</summary>
    public class RunState
    {
        public CountryDefinition Current;      // country (or home airport) we are in
        public CountryDefinition Destination;  // booked flight
        public int DestinationVisaYears;

        public readonly List<PhotoRecord> Photos = new List<PhotoRecord>();
        public BuffDefinition HeldItem;

        // Timers
        public float VisaSecondsTotal, VisaSecondsRemaining;
        public bool FlightTimerActive;
        public float FlightSecondsTotal, FlightSecondsRemaining;

        // Stats
        public float SecondsThisStay;          // real seconds since landing in Current
        public float RunSeconds;
        public int CountriesVisited;
        public int MilesEarned;

        readonly Dictionary<string, float> _secondsInCountry = new Dictionary<string, float>();

        public RunState(CountryDefinition start) { Current = start; }

        /// <summary>Time spent in the current country. Home time never counts toward residency.</summary>
        public void AddTimeInCurrent(float dt)
        {
            SecondsThisStay += dt;
            if (Current == null || Current.isHome) return;
            _secondsInCountry.TryGetValue(Current.id, out float s);
            _secondsInCountry[Current.id] = s + dt;
        }

        public float SecondsIn(string countryId) =>
            countryId != null && _secondsInCountry.TryGetValue(countryId, out float s) ? s : 0f;

        public float YearsIn(string countryId, float secondsPerYear) => SecondsIn(countryId) / secondsPerYear;

        public bool IsResidencyEligible(CountryDefinition c, float secondsPerYear) =>
            c != null && !c.isHome && YearsIn(c.id, secondsPerYear) >= c.requiredResidencyYears;

        public int PhotoCount(string countryId)
        {
            int n = 0;
            foreach (var p in Photos) if (p.countryId == countryId) n++;
            return n;
        }

        public List<PhotoRecord> PhotosFor(string countryId) => Photos.FindAll(p => p.countryId == countryId);
    }
}

using System;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Every gameplay number lives here. Designers tune this asset (Data/GameTuning.asset), never code.
    /// Read values through the accessor methods (e.g. <see cref="SecondsPerYear"/>) so Jam Demo Mode is respected.
    /// </summary>
    [CreateAssetMenu(menuName = "Passport Frenzy/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Serializable]
        public class TierSettings
        {
            [Tooltip("Multiplier on each obstacle's DensityGated chance.")]
            public float obstacleDensity = 0.6f;
            [Tooltip("Multiplier on each crowd's DensityGated chance.")]
            public float crowdDensity = 0.5f;
            [Tooltip("World units from chase start to the gate.")]
            public float terminalLength = 200f;
        }

        [Header("Jam Demo Mode (clamps everything for a short live demo)")]
        public bool jamDemoMode = true;
        [Tooltip("Real seconds per in-game year while in demo mode.")]
        public float demoSecondsPerYear = 6f;
        public float demoCountryRunSeconds = 30f;
        [Tooltip("Target chase duration in demo mode; terminal length is clamped to fit.")]
        public float demoChaseSeconds = 30f;

        [Header("Time")]
        [Tooltip("Real seconds per in-game year (normal mode). Brief: 1 year = 1 minute.")]
        public float secondsPerYear = 60f;

        [Header("Movement")]
        [Tooltip("False = free movement with Move (A/D, arrows, stick, D-pad). True = legacy auto-runner fallback.")]
        public bool autoRun = false;
        [Tooltip("Free movement: how fast horizontal speed reaches its target (units/s^2).")]
        public float moveAcceleration = 60f;
        [Tooltip("Free movement: how far you can walk back from the furthest point reached.")]
        public float maxBacktrackDistance = 18f;

        [Header("Runner")]
        public float baseRunSpeed = 8f;
        public float jumpVelocity = 13f;
        public float gravityScale = 3.5f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;
        [Tooltip("Speed multiplier right after hitting a Slow obstacle.")]
        [Range(0f, 1f)] public float stumbleSpeedMultiplier = 0.25f;
        public float stumbleRecoverSeconds = 2f;
        [Tooltip("Default crowd slow multiplier (a CrowdZone can override).")]
        [Range(0.05f, 1f)] public float crowdSpeedMultiplier = 0.55f;
        public float hitInvulnerabilitySeconds = 0.6f;

        [Header("Buffs")]
        public float speedBoostMultiplier = 1.6f;

        [Header("Booking app")]
        [Range(1, 3)] public int bookingChoices = 3;
        public Vector2 bookingWindowSeconds = new Vector2(4f, 6f);

        [Header("Residency application")]
        public float residencyDelayAfterBooking = 2f;
        public Vector2Int mashPressesRange = new Vector2Int(14, 24);
        public Vector2 mashWindowSeconds = new Vector2(4f, 6f);
        public int photosRequired = 3;
        public float photoAttachSeconds = 8f;
        public float resultDisplaySeconds = 1.6f;

        [Header("Freezone shop")]
        public float shopWindowSeconds = 7f;

        [Header("Flight")]
        [Tooltip("Extra seconds always added to the flight timer.")]
        public float flightTimerPaddingSeconds = 6f;
        [Tooltip("Chase time = (terminal / speed) * crowd factor * slack.")]
        public float chaseSlackMultiplier = 1.25f;
        public float flyingScreenSeconds = 2.5f;
        public float missedScreenSeconds = 3.5f;
        public int milesBasePerFlight = 100;
        [Tooltip("Miles per real second spent in the country you are leaving.")]
        public float milesPerSecondStayed = 2f;

        [Header("Difficulty tiers (element 0 = tier 1)")]
        public TierSettings[] tiers =
        {
            new TierSettings { obstacleDensity = 0.45f, crowdDensity = 0.35f, terminalLength = 160f },
            new TierSettings { obstacleDensity = 0.7f, crowdDensity = 0.6f, terminalLength = 240f },
            new TierSettings { obstacleDensity = 0.95f, crowdDensity = 0.85f, terminalLength = 320f },
        };

        [Header("Visa deadline")]
        [Tooltip("Country length = visa seconds x base speed x this. Below 1 leaves spare time for stumbles; "
                 + "reach the airport before the visa ends or lose all miles.")]
        [Range(0.3f, 1f)] public float visaDistanceSlack = 0.85f;

        [Header("Festivals per country visit")]
        [Range(0f, 1f)] public float noFestivalChance = 0.05f;
        [Range(0f, 1f)] public float twoFestivalChance = 0.05f;
        [Tooltip("Two festivals are only planned when the country run lasts at least this long.")]
        public float minRunSecondsForTwoFestivals = 20f;

        [Header("Spawning")]
        public float spawnAheadDistance = 40f;
        public float despawnBehindDistance = 30f;
        [Tooltip("Base chance for each PickupSocket to spawn a buff pickup.")]
        [Range(0f, 1f)] public float pickupChance = 0.12f;

        // ---- Accessors (respect demo mode) ----

        // Runtime-only override set by the debug panel, so toggling never dirties the asset.
        [NonSerialized] bool? _demoOverride;

        public bool DemoMode => _demoOverride ?? jamDemoMode;
        public void SetDemoOverride(bool on) => _demoOverride = on;
        public void ClearRuntimeOverrides() => _demoOverride = null;

        public float SecondsPerYear => DemoMode ? demoSecondsPerYear : secondsPerYear;

        public float CountryRunSeconds(int visaYears)
        {
            float s = visaYears * SecondsPerYear;
            return DemoMode ? Mathf.Min(s, demoCountryRunSeconds) : s;
        }

        /// <summary>World length of a country run; the airport entrance sits at the end.</summary>
        public float CountryLength(int visaYears) => CountryRunSeconds(visaYears) * baseRunSpeed * visaDistanceSlack;

        public TierSettings GetTier(int tier) => tiers[Mathf.Clamp(tier - 1, 0, tiers.Length - 1)];

        public float TerminalLength(int tier)
        {
            float length = GetTier(tier).terminalLength;
            if (DemoMode)
                length = Mathf.Min(length, demoChaseSeconds * baseRunSpeed / (CrowdTimeFactor(tier) * chaseSlackMultiplier));
            return length;
        }

        /// <summary>How much longer a chase takes because of crowds (1 = no crowds).</summary>
        public float CrowdTimeFactor(int tier)
        {
            // Roughly 60% of chase ground is crowd at density 1.
            float d = Mathf.Clamp01(GetTier(tier).crowdDensity * 0.6f);
            return 1f + d * (1f / crowdSpeedMultiplier - 1f);
        }

        public float ExpectedChaseSeconds(int tier) =>
            TerminalLength(tier) / baseRunSpeed * CrowdTimeFactor(tier) * chaseSlackMultiplier;
    }
}

using System;
using System.Collections.Generic;

namespace PassportFrenzy
{
    public struct FestivalSlot
    {
        public float distance;      // from the start of the country run
        public Chunk chunk;
        public string festivalName;
    }

    /// <summary>
    /// Decides the festivals of one country visit: usually exactly one, rarely none or two
    /// (GameTuning.noFestivalChance / twoFestivalChance). Two only when the run is long enough and two distinct
    /// festival names exist. Pure logic, unit-tested.
    /// </summary>
    public static class FestivalPlanner
    {
        public static int RollCount(double roll01, float runSeconds, GameTuning t)
        {
            if (roll01 < t.noFestivalChance) return 0;
            if (roll01 >= 1.0 - t.twoFestivalChance && runSeconds >= t.minRunSecondsForTwoFestivals) return 2;
            return 1;
        }

        public static List<FestivalSlot> Plan(CountryDefinition country, Chunk genericChunk, float runLength,
            float runSeconds, GameTuning t, Random rng)
        {
            var slots = new List<FestivalSlot>();
            int count = RollCount(rng.NextDouble(), runSeconds, t);
            if (count == 0 || genericChunk == null && country.signatureFestivalChunk == null) return slots;

            var used = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                Chunk chunk = null;
                string name = null;
                // First festival of the visit shows off the country's signature festival when it has one.
                if (i == 0 && country.signatureFestivalChunk != null)
                {
                    chunk = country.signatureFestivalChunk;
                    var zone = chunk.GetComponentInChildren<FestivalZone>(true);
                    name = zone != null ? zone.NameOverride : null;
                }
                if (chunk == null || string.IsNullOrEmpty(name))
                {
                    chunk ??= genericChunk;
                    name ??= PickUnusedName(country, used, rng);
                }
                if (chunk == null || string.IsNullOrEmpty(name) || used.Contains(name)) break; // nothing distinct left
                used.Add(name);
                slots.Add(new FestivalSlot { chunk = chunk, festivalName = name });
            }

            // Spread them out: one around the middle, two in separate halves.
            for (int i = 0; i < slots.Count; i++)
            {
                (float lo, float hi) band = slots.Count == 1 ? (0.25f, 0.75f) : i == 0 ? (0.15f, 0.40f) : (0.60f, 0.85f);
                var s = slots[i];
                s.distance = runLength * (band.lo + (float)rng.NextDouble() * (band.hi - band.lo));
                slots[i] = s;
            }
            return slots;
        }

        static string PickUnusedName(CountryDefinition c, HashSet<string> used, Random rng)
        {
            var options = new List<string>();
            if (c.festivalNames != null)
                foreach (var n in c.festivalNames)
                    if (!string.IsNullOrEmpty(n) && !used.Contains(n)) options.Add(n);
            if (options.Count == 0) return used.Count == 0 ? c.displayName + " Festival" : null;
            return options[rng.Next(options.Count)];
        }
    }
}

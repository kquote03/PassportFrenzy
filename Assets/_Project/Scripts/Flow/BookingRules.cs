using System;
using System.Collections.Generic;

namespace PassportFrenzy
{
    public static class BookingRules
    {
        /// <summary>
        /// Up to <paramref name="count"/> random destinations the player does not own yet (lifetime collection),
        /// excluding home and, if possible, the country they are standing in.
        /// </summary>
        public static List<BookingOption> PickOptions(IList<CountryDefinition> destinations, ICollection<string> owned,
            CountryDefinition current, int count, Random rng)
        {
            var pool = new List<CountryDefinition>();
            foreach (var c in destinations)
                if (c != null && !c.isHome && !owned.Contains(c.id) && c != current) pool.Add(c);

            // Last unowned country is the one we are in: let them fly back to it.
            if (pool.Count == 0 && current != null && !current.isHome && !owned.Contains(current.id))
                pool.Add(current);

            // Fisher-Yates partial shuffle.
            for (int i = 0; i < pool.Count; i++)
            {
                int j = rng.Next(i, pool.Count);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<BookingOption>();
            for (int i = 0; i < pool.Count && i < count; i++)
                result.Add(new BookingOption { country = pool[i], visaYears = pool[i].RollVisaYears(rng) });
            return result;
        }

        public static int MilesForFlight(GameTuning t, float secondsStayed) =>
            t.milesBasePerFlight + (int)Math.Round(secondsStayed * t.milesPerSecondStayed);
    }
}

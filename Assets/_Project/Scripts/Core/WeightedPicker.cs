using System;
using System.Collections.Generic;

namespace PassportFrenzy
{
    public static class WeightedPicker
    {
        /// <summary>Picks an item by weight. Items with weight &lt;= 0 are never picked. Returns default if none.</summary>
        /// <param name="roll01">A random value in [0,1).</param>
        public static T Pick<T>(IList<T> items, Func<T, float> weight, double roll01)
        {
            if (items == null || items.Count == 0) return default;
            double total = 0;
            foreach (var it in items) total += Math.Max(0f, weight(it));
            if (total <= 0) return default;
            double r = roll01 * total;
            foreach (var it in items)
            {
                float w = Math.Max(0f, weight(it));
                if (w <= 0) continue;
                if (r < w) return it;
                r -= w;
            }
            // Floating point fallback: last positive-weight item.
            for (int i = items.Count - 1; i >= 0; i--) if (weight(items[i]) > 0) return items[i];
            return default;
        }
    }
}

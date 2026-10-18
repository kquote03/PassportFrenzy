using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>
    /// Pure helpers for the parallax layers (unit-tested). A layer's content is a function of
    /// x' = worldX - camX * (1 - motion), where motion = the fraction of world movement the layer follows
    /// (0 = glued to the camera, 1 = fixed in the world). Tiles of width W are placed at k*W in that space.
    /// </summary>
    public static class ParallaxMath
    {
        /// <summary>First tile index and how many tiles cover the view [camX - halfWidth, camX + halfWidth].</summary>
        public static (int kMin, int count) TileRange(float camX, float motion, float halfWidth, float tileWidth)
        {
            float local = camX * motion;
            int kMin = Mathf.FloorToInt((local - halfWidth) / tileWidth);
            int kMax = Mathf.FloorToInt((local + halfWidth) / tileWidth);
            return (kMin, kMax - kMin + 1);
        }

        /// <summary>World X of tile k's left edge.</summary>
        public static float TileLeftX(int k, float camX, float motion, float tileWidth) => k * tileWidth + camX * (1f - motion);

        /// <summary>
        /// Silhouette tint with atmospheric perspective: blend from the sky toward a darkened primary colour, and make
        /// sure it stays at least <paramref name="minLuminanceDelta"/> darker than the sky (near-white palettes).
        /// </summary>
        public static Color LayerTint(Color sky, Color primary, float blend, float minLuminanceDelta = 0.08f)
        {
            var target = new Color(primary.r * 0.6f, primary.g * 0.6f, primary.b * 0.6f, 1f);
            var c = Color.Lerp(sky, target, blend);
            c.a = 1f;
            float skyLum = Luminance(sky), lum = Luminance(c);
            if (skyLum - lum < minLuminanceDelta && lum > 0.001f)
            {
                float k = Mathf.Clamp01((skyLum - minLuminanceDelta) / lum);
                c = new Color(c.r * k, c.g * k, c.b * k, 1f);
            }
            return c;
        }

        public static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}

using NUnit.Framework;
using UnityEngine;

namespace PassportFrenzy.Tests
{
    public class ParallaxMathTests
    {
        const float Hw = 10.7f, W = 32f;

        [Test]
        public void TileRange_AtOrigin_TwoTilesAroundZero()
        {
            foreach (float p in new[] { 0f, 0.15f, 0.4f, 1f })
            {
                var (kMin, count) = ParallaxMath.TileRange(0f, p, Hw, W);
                Assert.AreEqual(-1, kMin, $"motion {p}");
                Assert.AreEqual(2, count, $"motion {p}");
            }
        }

        [Test]
        public void TileRange_FarAway_CoversTheView()
        {
            var (kMin, count) = ParallaxMath.TileRange(1000f, 0.15f, Hw, W);
            Assert.AreEqual(4, kMin);
            Assert.AreEqual(2, count);
            float left = ParallaxMath.TileLeftX(kMin, 1000f, 0.15f, W);
            float right = ParallaxMath.TileLeftX(kMin + count, 1000f, 0.15f, W);
            Assert.LessOrEqual(left, 1000f - Hw);
            Assert.GreaterOrEqual(right, 1000f + Hw);
        }

        [Test]
        public void Tiles_AlwaysCoverTheView_AndAbut()
        {
            foreach (float p in new[] { 0f, 0.15f, 0.4f, 1f })
            for (float camX = -50f; camX <= 500f; camX += 7f)
            {
                var (kMin, count) = ParallaxMath.TileRange(camX, p, Hw, W);
                Assert.That(count, Is.InRange(1, 3));
                float left = ParallaxMath.TileLeftX(kMin, camX, p, W);
                float right = ParallaxMath.TileLeftX(kMin + count, camX, p, W);
                Assert.LessOrEqual(left, camX - Hw + 1e-3f, $"camX {camX} motion {p}");
                Assert.GreaterOrEqual(right, camX + Hw - 1e-3f, $"camX {camX} motion {p}");
                Assert.AreEqual(W, ParallaxMath.TileLeftX(kMin + 1, camX, p, W) - left, 1e-3f);
            }
        }

        [Test]
        public void Motion_ZeroFollowsCamera_OneStaysInWorld()
        {
            Assert.AreEqual(100f, ParallaxMath.TileLeftX(0, 100f, 0f, W), 1e-4f);  // glued to the camera
            Assert.AreEqual(0f, ParallaxMath.TileLeftX(0, 100f, 1f, W), 1e-4f);    // fixed in the world
            Assert.AreEqual(85f, ParallaxMath.TileLeftX(0, 100f, 0.15f, W), 1e-4f); // scrolls at 15% of world speed
        }

        [Test]
        public void LayerTint_StaysDarkerThanSky_EvenForWhitePrimary()
        {
            var sky = new Color(1f, 0.89f, 0.92f);     // Japan
            var primary = new Color(0.95f, 0.95f, 0.95f);
            var far = ParallaxMath.LayerTint(sky, primary, 0.35f);
            Assert.GreaterOrEqual(ParallaxMath.Luminance(sky) - ParallaxMath.Luminance(far), 0.08f - 1e-3f);
            var mid = ParallaxMath.LayerTint(sky, primary, 0.6f);
            Assert.LessOrEqual(ParallaxMath.Luminance(mid), ParallaxMath.Luminance(far) + 1e-3f);
            Assert.AreEqual(1f, far.a);
        }
    }
}

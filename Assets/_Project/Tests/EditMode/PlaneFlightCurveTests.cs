using NUnit.Framework;
using UnityEngine;
using Kind = PassportFrenzy.PlaneFlightCurve.Kind;

namespace PassportFrenzy.Tests
{
    public class PlaneFlightCurveTests
    {
        [Test]
        public void Takeoff_StartsParked()
        {
            var p = PlaneFlightCurve.Evaluate(Kind.Takeoff, 0f);
            Assert.AreEqual(0f, p.dx, 1e-4f);
            Assert.AreEqual(0f, p.dy, 1e-4f);
            Assert.AreEqual(0f, p.angleDeg, 1e-4f);
            Assert.AreEqual(1f, p.scale, 1e-4f);
        }

        [Test]
        public void Takeoff_TaxisFlatThenClimbs()
        {
            float lastDx = -1f, lastDy = -1f;
            for (float t = 0f; t <= 1f; t += 0.02f)
            {
                var p = PlaneFlightCurve.Evaluate(Kind.Takeoff, t);
                Assert.GreaterOrEqual(p.dx, lastDx - 1e-5f, $"dx must not go back at t={t}");
                Assert.GreaterOrEqual(p.dy, lastDy - 1e-5f, $"dy must not drop at t={t}");
                if (t <= PlaneFlightCurve.TaxiFraction) { Assert.AreEqual(0f, p.dy, 1e-4f); Assert.AreEqual(0f, p.angleDeg, 1e-4f); }
                Assert.That(p.scale, Is.InRange(0.7f, 1f));
                lastDx = p.dx; lastDy = p.dy;
            }
            var end = PlaneFlightCurve.Evaluate(Kind.Takeoff, 1f);
            Assert.GreaterOrEqual(end.dy, 6f);
            Assert.AreEqual(PlaneFlightCurve.PitchDegrees, end.angleDeg, 1e-3f);
            Assert.AreEqual(PlaneFlightCurve.TakeoffDistance, end.dx, 1e-3f);
        }

        [Test]
        public void Takeoff_IsContinuousAtLiftOff()
        {
            var a = PlaneFlightCurve.Evaluate(Kind.Takeoff, PlaneFlightCurve.TaxiFraction - 1e-4f);
            var b = PlaneFlightCurve.Evaluate(Kind.Takeoff, PlaneFlightCurve.TaxiFraction + 1e-4f);
            Assert.AreEqual(a.dx, b.dx, 0.01f);
            Assert.AreEqual(a.dy, b.dy, 0.01f);
            Assert.AreEqual(a.angleDeg, b.angleDeg, 0.1f);
        }

        [Test]
        public void FlyPast_CrossesLeftwardsAndClearsTheScreen()
        {
            float hw = 10.7f, distance = 2f * hw + 14f;
            float last = 1f;
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                var p = PlaneFlightCurve.Evaluate(Kind.FlyPast, t, distance);
                Assert.LessOrEqual(p.dx, last + 1e-5f);
                Assert.GreaterOrEqual(p.dy, -1e-4f);
                last = p.dx;
            }
            var end = PlaneFlightCurve.Evaluate(Kind.FlyPast, 1f, distance);
            Assert.Greater(-end.dx, 2f * hw + 8f);
        }

        [Test]
        public void Evaluate_ClampsTime()
        {
            var over = PlaneFlightCurve.Evaluate(Kind.Takeoff, 1.5f);
            var one = PlaneFlightCurve.Evaluate(Kind.Takeoff, 1f);
            Assert.AreEqual(one.dx, over.dx, 1e-5f);
            Assert.AreEqual(one.dy, over.dy, 1e-5f);
        }
    }
}

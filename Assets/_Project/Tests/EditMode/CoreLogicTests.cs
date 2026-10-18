using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PassportFrenzy.Tests
{
    public class CoreLogicTests
    {
        readonly List<Object> _created = new List<Object>();

        CountryDefinition Country(string id, int requiredYears = 3, bool home = false)
        {
            var c = ScriptableObject.CreateInstance<CountryDefinition>();
            c.id = id;
            c.displayName = id.ToUpperInvariant();
            c.requiredResidencyYears = requiredYears;
            c.isHome = home;
            c.visaYearsRange = new Vector2Int(2, 5);
            _created.Add(c);
            return c;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        [Test]
        public void Residency_AccumulatesAcrossVisits_AndChecksEligibility()
        {
            var jp = Country("jp", requiredYears: 3);
            var es = Country("es");
            var run = new RunState(jp);
            run.AddTimeInCurrent(100f);
            Assert.IsFalse(run.IsResidencyEligible(jp, 60f));

            run.Current = es;
            run.AddTimeInCurrent(500f);
            run.Current = jp;
            run.AddTimeInCurrent(80f);

            Assert.AreEqual(180f, run.SecondsIn("jp"), 0.001f);
            Assert.IsTrue(run.IsResidencyEligible(jp, 60f));
        }

        [Test]
        public void Residency_HomeNeverCounts()
        {
            var uae = Country("ae", requiredYears: 0, home: true);
            var run = new RunState(uae);
            run.AddTimeInCurrent(1000f);
            Assert.AreEqual(0f, run.SecondsIn("ae"));
            Assert.IsFalse(run.IsResidencyEligible(uae, 1f));
        }

        [Test]
        public void TimeFormat_ShowsWholeMinutesOrSeconds()
        {
            Assert.AreEqual("5 years (5 min)", TimeFormat.Years(5, 60f));
            Assert.AreEqual("1 year (1 min)", TimeFormat.Years(1, 60f));
            Assert.AreEqual("5 years (30 s)", TimeFormat.Years(5, 6f));
            Assert.AreEqual("1:05", TimeFormat.Clock(64.2f));
            Assert.AreEqual("0:00", TimeFormat.Clock(-3f));
        }

        [Test]
        public void Booking_ExcludesOwnedHomeAndCurrent()
        {
            var home = Country("ae", home: true);
            var all = new List<CountryDefinition> { home, Country("jp"), Country("es"), Country("in"), Country("fr") };
            var owned = new List<string> { "es" };
            var current = all[1]; // jp
            for (int seed = 0; seed < 20; seed++)
            {
                var opts = BookingRules.PickOptions(all, owned, current, 3, new System.Random(seed));
                Assert.AreEqual(2, opts.Count); // in, fr
                foreach (var o in opts)
                {
                    Assert.AreNotEqual("ae", o.country.id);
                    Assert.AreNotEqual("es", o.country.id);
                    Assert.AreNotEqual("jp", o.country.id);
                    Assert.That(o.visaYears, Is.InRange(2, 5));
                }
            }
        }

        [Test]
        public void Booking_FallsBackToCurrentWhenItIsTheLastUnowned()
        {
            var jp = Country("jp");
            var es = Country("es");
            var opts = BookingRules.PickOptions(new List<CountryDefinition> { jp, es }, new List<string> { "es" }, jp, 3, new System.Random(1));
            Assert.AreEqual(1, opts.Count);
            Assert.AreEqual("jp", opts[0].country.id);
        }

        [Test]
        public void WeightedPicker_RespectsWeights()
        {
            var items = new List<(string id, float w)> { ("a", 0f), ("b", 1f), ("c", 3f) };
            Assert.AreEqual("b", WeightedPicker.Pick(items, i => i.w, 0.0).id);
            Assert.AreEqual("b", WeightedPicker.Pick(items, i => i.w, 0.24).id);
            Assert.AreEqual("c", WeightedPicker.Pick(items, i => i.w, 0.26).id);
            Assert.AreEqual("c", WeightedPicker.Pick(items, i => i.w, 0.9999).id);
            Assert.AreEqual(default((string, float)), WeightedPicker.Pick(new List<(string, float)>(), i => i.Item2, 0.5));
        }

        [Test]
        public void Save_RoundTrips()
        {
            string path = Path.Combine(Path.GetTempPath(), "pf_save_test.json");
            var d = new SaveData { miles = 1234, countriesVisited = 7, photosTaken = 9, secondsPlayed = 42.5f };
            d.passports.Add("jp");
            d.passports.Add("es");
            SaveSystem.SaveTo(path, d);
            var back = SaveSystem.LoadFrom(path);
            File.Delete(path);
            Assert.AreEqual(1234, back.miles);
            CollectionAssert.AreEqual(new[] { "jp", "es" }, back.passports);
            Assert.AreEqual(42.5f, back.secondsPlayed, 0.001f);
            Assert.IsTrue(back.Owns("jp"));
        }

        [Test]
        public void Miles_ScaleWithStay()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            _created.Add(t);
            t.milesBasePerFlight = 100;
            t.milesPerSecondStayed = 2f;
            Assert.AreEqual(100, BookingRules.MilesForFlight(t, 0f));
            Assert.AreEqual(160, BookingRules.MilesForFlight(t, 30f));
        }

        [Test]
        public void Tuning_DemoModeClampsRunAndChase()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            _created.Add(t);
            t.jamDemoMode = false;
            Assert.AreEqual(300f, t.CountryRunSeconds(5), 0.01f);
            t.SetDemoOverride(true);
            Assert.AreEqual(30f, t.CountryRunSeconds(5), 0.01f);
            Assert.AreEqual(30f, t.CountryRunSeconds(9), 0.01f);
            Assert.That(t.ExpectedChaseSeconds(3), Is.LessThanOrEqualTo(t.demoChaseSeconds + 0.01f));
        }

        [Test]
        public void FlightSeconds_PhoneOverlapsTheChase()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            _created.Add(t);
            float run = t.spawnAheadDistance / t.baseRunSpeed + t.ExpectedChaseSeconds(2) + t.flightTimerPaddingSeconds;
            // Only the overlap allowance of the phone time is added on top of the run itself.
            Assert.AreEqual(run + t.menuOverlapAllowance * t.shopWindowSeconds, t.FlightSeconds(2, false), 0.01f);
            Assert.That(t.FlightSeconds(2, true), Is.GreaterThan(t.FlightSeconds(2, false)));
            t.menuOverlapAllowance = 0f;
            Assert.AreEqual(run, t.FlightSeconds(2, true), 0.01f);
        }
    }
}

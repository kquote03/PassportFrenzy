using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace PassportFrenzy.Tests
{
    public class FestivalPlannerTests
    {
        readonly List<Object> _created = new List<Object>();
        GameTuning _t;

        [SetUp]
        public void SetUp()
        {
            _t = ScriptableObject.CreateInstance<GameTuning>();
            _created.Add(_t);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        Chunk FestivalChunk(string nameOverride = null)
        {
            var go = new GameObject("FestivalChunk");
            _created.Add(go);
            var chunk = go.AddComponent<Chunk>();
            var zoneGo = new GameObject("Zone");
            zoneGo.transform.SetParent(go.transform);
            zoneGo.AddComponent<BoxCollider2D>();
            var zone = zoneGo.AddComponent<FestivalZone>();
            if (nameOverride != null)
                typeof(FestivalZone).GetField("festivalNameOverride", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(zone, nameOverride);
            return chunk;
        }

        CountryDefinition Country(params string[] festivals)
        {
            var c = ScriptableObject.CreateInstance<CountryDefinition>();
            c.id = "xx";
            c.displayName = "Testland";
            c.festivalNames = festivals;
            _created.Add(c);
            return c;
        }

        [Test]
        public void RollCount_FollowsChancesAndRunLength()
        {
            Assert.AreEqual(0, FestivalPlanner.RollCount(0.0, 60f, _t));
            Assert.AreEqual(0, FestivalPlanner.RollCount(0.049, 60f, _t));
            Assert.AreEqual(1, FestivalPlanner.RollCount(0.051, 60f, _t)); // 0.05f as double is 0.0500000007
            Assert.AreEqual(1, FestivalPlanner.RollCount(0.5, 60f, _t));
            Assert.AreEqual(1, FestivalPlanner.RollCount(0.949, 60f, _t));
            Assert.AreEqual(2, FestivalPlanner.RollCount(0.95, 60f, _t));
            Assert.AreEqual(1, FestivalPlanner.RollCount(0.99, 10f, _t), "two festivals only when the run is long enough");
        }

        [Test]
        public void RollCount_DistributionIsRoughly_5_90_5()
        {
            var rng = new System.Random(42);
            var counts = new int[3];
            for (int i = 0; i < 20000; i++) counts[FestivalPlanner.RollCount(rng.NextDouble(), 60f, _t)]++;
            Assert.That(counts[0] / 20000f, Is.InRange(0.04f, 0.06f));
            Assert.That(counts[1] / 20000f, Is.InRange(0.88f, 0.92f));
            Assert.That(counts[2] / 20000f, Is.InRange(0.04f, 0.06f));
        }

        [Test]
        public void Plan_PlacesFestivalsInBands_WithDistinctNames_SignatureFirst()
        {
            var generic = FestivalChunk();
            var signature = FestivalChunk("La Tomatina");
            var c = Country("La Tomatina", "San Fermin", "Las Fallas");
            c.signatureFestivalChunk = signature;
            _t.noFestivalChance = 0f;
            _t.twoFestivalChance = 1f; // force two
            for (int seed = 0; seed < 50; seed++)
            {
                var plan = FestivalPlanner.Plan(c, generic, 200f, 60f, _t, new System.Random(seed));
                Assert.AreEqual(2, plan.Count);
                Assert.AreSame(signature, plan[0].chunk);
                Assert.AreEqual("La Tomatina", plan[0].festivalName);
                Assert.AreSame(generic, plan[1].chunk);
                Assert.AreNotEqual(plan[0].festivalName, plan[1].festivalName);
                Assert.That(plan[0].distance, Is.InRange(200f * 0.15f, 200f * 0.40f));
                Assert.That(plan[1].distance, Is.InRange(200f * 0.60f, 200f * 0.85f));
            }
        }

        [Test]
        public void Plan_SingleFestival_InMiddleBand()
        {
            var generic = FestivalChunk();
            var c = Country("Holi", "Diwali");
            _t.noFestivalChance = 0f;
            _t.twoFestivalChance = 0f;
            var plan = FestivalPlanner.Plan(c, generic, 100f, 30f, _t, new System.Random(3));
            Assert.AreEqual(1, plan.Count);
            Assert.That(plan[0].distance, Is.InRange(25f, 75f));
            Assert.Contains(plan[0].festivalName, new[] { "Holi", "Diwali" });
        }

        [Test]
        public void Plan_FallsBackToOne_WhenNoDistinctNameLeft()
        {
            var generic = FestivalChunk();
            var c = Country("Only Festival");
            _t.noFestivalChance = 0f;
            _t.twoFestivalChance = 1f;
            var plan = FestivalPlanner.Plan(c, generic, 100f, 60f, _t, new System.Random(1));
            Assert.AreEqual(1, plan.Count);
        }

        [Test]
        public void CountryLength_UsesVisaTimeSpeedAndSlack()
        {
            _t.jamDemoMode = false;
            _t.secondsPerYear = 60f;
            _t.baseRunSpeed = 8f;
            _t.visaDistanceSlack = 0.85f;
            Assert.AreEqual(3 * 60f * 8f * 0.85f, _t.CountryLength(3), 0.01f);
        }
    }
}

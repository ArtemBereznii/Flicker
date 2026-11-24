using System.Collections.Generic;
using NUnit.Framework;

namespace Flicker.Generation.Tests
{
    public class RngTests
    {
        // FNV-1a of "kyiv", pinned so word seeds never silently change.
        private const int KyivSeed = -1775116094;

        [Test] public void SameSeed_SameSequence() { var a = new Rng(42); var b = new Rng(42); for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt()); }
        [Test] public void DifferentSeeds_DifferentSequence() { Assert.AreNotEqual(new Rng(1).NextUInt(), new Rng(2).NextUInt()); }
        [Test] public void Range_StaysInBounds() { var r = new Rng(7); for (int i = 0; i < 10000; i++) Assert.That(r.Range(-3, 5), Is.InRange(-3, 4)); }
        [Test] public void Range_CoversWholeRange() { var r = new Rng(7); var seen = new HashSet<int>(); for (int i = 0; i < 1000; i++) seen.Add(r.Range(-3, 5)); Assert.AreEqual(8, seen.Count); }
        [Test] public void Range_ExtremeBounds_DoNotOverflow() { var r = new Rng(5); for (int i = 0; i < 1000; i++) Assert.That(r.Range(int.MinValue, int.MaxValue), Is.LessThan(int.MaxValue)); }
        [Test] public void Range_EmptyOrSingle_ReturnsMin() { Assert.AreEqual(4, new Rng(1).Range(4, 5)); Assert.AreEqual(4, new Rng(1).Range(4, 4)); }
        [Test] public void Value_InUnitInterval() { var r = new Rng(9); for (int i = 0; i < 10000; i++) Assert.That(r.Value(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f)); }
        [Test] public void Chance_ZeroNeverOneAlways() { var r = new Rng(2); for (int i = 0; i < 1000; i++) { Assert.IsFalse(r.Chance(0f)); Assert.IsTrue(r.Chance(1f)); } }
        [Test] public void Pick_ReturnsElements() { var r = new Rng(4); var items = new[] { "a", "b", "c" }; var seen = new HashSet<string>(); for (int i = 0; i < 200; i++) seen.Add(r.Pick(items)); CollectionAssert.AreEquivalent(items, seen); }
        [Test] public void PickWeighted_NeverPicksZeroWeight() { var r = new Rng(3); for (int i = 0; i < 1000; i++) Assert.AreNotEqual(1, r.PickWeighted(new[] { 1f, 0f, 2f })); }
        [Test] public void PickWeighted_AllZero_ReturnsMinusOne() { Assert.AreEqual(-1, new Rng(3).PickWeighted(new[] { 0f, 0f })); }

        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void ExtremeSeeds_ProduceVariedOutput(int seed) { var r = new Rng(seed); var set = new HashSet<uint>(); for (int i = 0; i < 100; i++) set.Add(r.NextUInt()); Assert.Greater(set.Count, 95); }

        [Test] public void Derive_AttemptZeroIsSeed_OthersDiffer() { Assert.AreEqual(123, Rng.Derive(123, 0)); Assert.AreNotEqual(Rng.Derive(123, 1), Rng.Derive(123, 2)); Assert.AreNotEqual(123, Rng.Derive(123, 1)); }

        [Test] public void SeedText_Numeric() { Assert.IsTrue(SeedText.TryParse(" 8819273 ", out int s)); Assert.AreEqual(8819273, s); Assert.IsTrue(SeedText.TryParse("-5", out s)); Assert.AreEqual(-5, s); }
        [Test] public void SeedText_Words_StableAndCaseInsensitive() { Assert.IsTrue(SeedText.TryParse("Kyiv", out int a)); Assert.IsTrue(SeedText.TryParse("kyiv", out int b)); Assert.AreEqual(a, b); Assert.AreEqual(KyivSeed, a); }
        [Test] public void SeedText_Empty_False() { Assert.IsFalse(SeedText.TryParse("  ", out _)); Assert.IsFalse(SeedText.TryParse(null, out _)); }
    }
}

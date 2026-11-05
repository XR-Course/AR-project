using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    public class RouteSamplerTests
    {
        private static void AssertPoints(Vector3[] expected, List<Vector3> actual)
        {
            Assert.AreEqual(expected.Length, actual.Count, "Point count");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Less(
                    Vector3.Distance(expected[i], actual[i]),
                    0.001f,
                    $"Point {i}: expected {expected[i]}, got {actual[i]}");
            }
        }

        [Test]
        public void Resample_StraightLine_EvenSpacing()
        {
            List<Vector3> points = RouteSampler.Resample(
                new[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0) }, 2f);

            AssertPoints(
                new[]
                {
                    new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(4, 0, 0),
                    new Vector3(6, 0, 0), new Vector3(8, 0, 0), new Vector3(10, 0, 0)
                },
                points);
        }

        [Test]
        public void Resample_SpacingNotDividingLength_NeverExceedsMaxSpacing()
        {
            List<Vector3> points = RouteSampler.Resample(
                new[] { new Vector3(0, 0, 0), new Vector3(10, 0, 0) }, 3f);

            // 10 m / 3 m rounds up to 4 steps of 2.5 m.
            Assert.AreEqual(5, points.Count);
            for (int i = 1; i < points.Count; i++)
                Assert.LessOrEqual(Vector3.Distance(points[i - 1], points[i]), 3f);
        }

        [Test]
        public void Resample_KeepsCorners()
        {
            List<Vector3> points = RouteSampler.Resample(
                new[] { new Vector3(0, 0, 0), new Vector3(4, 0, 0), new Vector3(4, 0, 4) }, 3f);

            AssertPoints(
                new[]
                {
                    new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(4, 0, 0),
                    new Vector3(4, 0, 2), new Vector3(4, 0, 4)
                },
                points);
        }

        [Test]
        public void Resample_SpacingLongerThanSegment_KeepsOnlyEndpoints()
        {
            List<Vector3> points = RouteSampler.Resample(
                new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0) }, 5f);

            AssertPoints(new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0) }, points);
        }

        [Test]
        public void Resample_DuplicatePoints_AreNotRepeated()
        {
            List<Vector3> points = RouteSampler.Resample(
                new[] { new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(2, 0, 0) }, 1f);

            AssertPoints(
                new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(2, 0, 0) },
                points);
        }

        [Test]
        public void Resample_EmptyAndSingle()
        {
            Assert.IsEmpty(RouteSampler.Resample(Array.Empty<Vector3>(), 1f));
            Assert.AreEqual(1, RouteSampler.Resample(new[] { Vector3.one }, 1f).Count);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Resample_InvalidSpacing_Throws(float spacing)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RouteSampler.Resample(new[] { Vector3.zero, Vector3.one }, spacing));
        }

        [Test]
        public void Resample_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RouteSampler.Resample(null, 1f));
        }
    }
}

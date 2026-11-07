using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Tests
{
    public class RouteArrowLayoutTests
    {
        [Test]
        public void Build_StraightSegment_PlacesArrowsBetweenSamples()
        {
            List<RouteArrowPlacement> arrows = RouteArrowLayout.Build(
                new[] { Vector3.zero, new Vector3(6, 0, 0) }, 2f);

            Assert.AreEqual(3, arrows.Count);
            Assert.Less(Vector3.Distance(new Vector3(1, 0, 0), arrows[0].Position), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(3, 0, 0), arrows[1].Position), 0.001f);
            Assert.Less(Vector3.Distance(new Vector3(5, 0, 0), arrows[2].Position), 0.001f);

            foreach (RouteArrowPlacement arrow in arrows)
            {
                Assert.AreEqual(Vector3.right, arrow.Direction);
                Assert.AreEqual(1, arrow.TargetWaypointIndex);
            }
        }

        [Test]
        public void Build_Corner_ArrowsFollowEachSegment()
        {
            List<RouteArrowPlacement> arrows = RouteArrowLayout.Build(
                new[] { Vector3.zero, new Vector3(4, 0, 0), new Vector3(4, 0, 4) }, 2f);

            Assert.AreEqual(4, arrows.Count);
            Assert.AreEqual(Vector3.right, arrows[1].Direction);
            Assert.AreEqual(1, arrows[1].TargetWaypointIndex);
            Assert.AreEqual(Vector3.forward, arrows[2].Direction);
            Assert.AreEqual(2, arrows[2].TargetWaypointIndex);
        }

        [Test]
        public void Build_ShortSegment_StillGetsOneArrow()
        {
            List<RouteArrowPlacement> arrows = RouteArrowLayout.Build(
                new[] { Vector3.zero, new Vector3(0.5f, 0, 0) }, 2f);

            Assert.AreEqual(1, arrows.Count);
            Assert.AreEqual(new Vector3(0.25f, 0, 0), arrows[0].Position);
        }

        [Test]
        public void Build_DuplicatePointsAndTooFewPoints_AreSkipped()
        {
            Assert.IsEmpty(RouteArrowLayout.Build(new Vector3[0], 1f));
            Assert.IsEmpty(RouteArrowLayout.Build(new[] { Vector3.one }, 1f));
            Assert.IsEmpty(RouteArrowLayout.Build(new[] { Vector3.one, Vector3.one }, 1f));
        }

        [Test]
        public void Build_IgnoresHeightForDirection()
        {
            List<RouteArrowPlacement> arrows = RouteArrowLayout.Build(
                new[] { Vector3.zero, new Vector3(2, 1, 0) }, 5f);

            Assert.AreEqual(Vector3.right, arrows[0].Direction);
        }

        [TestCase(0, 2, RouteVisualState.Passed)]
        [TestCase(2, 2, RouteVisualState.Next)]
        [TestCase(3, 2, RouteVisualState.Upcoming)]
        public void StateFor_ComparesWithCurrentWaypoint(int index, int current, RouteVisualState expected)
        {
            Assert.AreEqual(expected, RouteArrowLayout.StateFor(index, current));
        }

        [Test]
        public void FloorArrowMesh_FacesUpAndPointsForward()
        {
            Mesh mesh = FloorArrowMesh.Create();

            try
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;

                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]];
                    Vector3 normal = Vector3.Cross(vertices[triangles[t + 1]] - a, vertices[triangles[t + 2]] - a);
                    Assert.Greater(normal.y, 0f, $"Triangle {t / 3} faces down");
                }

                Assert.Greater(mesh.bounds.max.z, 0.29f, "Tip should point +Z");
                Assert.AreEqual(0f, mesh.bounds.size.y, 0.0001f, "Arrow should be flat");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
    }
}

using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Small hard-coded corridor for tests and for other modules to develop
    /// against before the real hallway is measured.
    ///
    ///                 xr-lab (10,0,8)
    ///                   |
    ///   entrance ---- junction-a ---- junction-b ---- classroom-2
    ///   (0,0,0)       (10,0,0)        (20,0,0)        (30,0,0)
    ///                                   |
    ///                 reception (20,0,-5)
    ///
    ///   storage (50,0,50) is not connected (used to test unreachable routes).
    /// </summary>
    public static class MockCampusMap
    {
        public const string Entrance = "entrance";
        public const string JunctionA = "junction-a";
        public const string JunctionB = "junction-b";
        public const string XrLab = "xr-lab";
        public const string Reception = "reception";
        public const string Classroom2 = "classroom-2";
        public const string Storage = "storage";

        public static CampusGraph Create()
        {
            var graph = new CampusGraph();

            graph.AddNode(new CampusNode(Entrance, new Vector3(0, 0, 0), "Entrance", markerName: "EntranceMarker"));
            graph.AddNode(new CampusNode(JunctionA, new Vector3(10, 0, 0), markerName: "HallwayCheckpoint"));
            graph.AddNode(new CampusNode(JunctionB, new Vector3(20, 0, 0), markerName: "Number2Checkpoint"));
            graph.AddNode(new CampusNode(XrLab, new Vector3(10, 0, 8), "XR Lab", isDestination: true));
            graph.AddNode(new CampusNode(Reception, new Vector3(20, 0, -5), "Reception", isDestination: true));
            graph.AddNode(new CampusNode(Classroom2, new Vector3(30, 0, 0), "Classroom 2", isDestination: true));
            graph.AddNode(new CampusNode(Storage, new Vector3(50, 0, 50), "Storage", isDestination: true));

            graph.AddEdge(Entrance, JunctionA);
            graph.AddEdge(JunctionA, XrLab);
            graph.AddEdge(JunctionA, JunctionB);
            graph.AddEdge(JunctionB, Reception);
            graph.AddEdge(JunctionB, Classroom2);

            return graph;
        }
    }
}

using System.Globalization;
using CampusNavigation.Navigation;
using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    public enum NavigationScreen
    {
        DestinationSelection,
        Navigation,
        Arrived
    }

    public enum BannerSeverity
    {
        None,
        Info,
        Warning
    }

    public readonly struct StatusBanner
    {
        public BannerSeverity Severity { get; }
        public string Title { get; }
        public string Message { get; }
        public bool Visible => Severity != BannerSeverity.None;

        public StatusBanner(BannerSeverity severity, string title, string message)
        {
            Severity = severity;
            Title = title;
            Message = message;
        }

        public static StatusBanner Hidden => new(BannerSeverity.None, string.Empty, string.Empty);
    }

    /// <summary>
    /// All texts and screen decisions of the UI, kept free of UI components so they
    /// can be unit tested and changed in one place.
    /// </summary>
    public static class NavigationUIText
    {
        /// <summary>Average indoor walking speed used for time estimates (m/s).</summary>
        public const float WalkingSpeed = 1.2f;

        public const string SafetyReminder = "Watch your surroundings, not only the screen.";

        public static NavigationScreen ScreenFor(NavigationState state) => state switch
        {
            NavigationState.Idle => NavigationScreen.DestinationSelection,
            NavigationState.Arrived => NavigationScreen.Arrived,
            _ => NavigationScreen.Navigation
        };

        public static StatusBanner BannerFor(NavigationState state) => state switch
        {
            NavigationState.Scanning => new StatusBanner(
                BannerSeverity.Info,
                "Scan a marker",
                "Point your camera at the nearest campus marker to start."),

            NavigationState.Relocalizing => new StatusBanner(
                BannerSeverity.Warning,
                "Tracking lost",
                "Arrows may be wrong. Scan the nearest marker to recalibrate."),

            _ => StatusBanner.Hidden
        };

        public static string FormatDistance(float meters)
        {
            if (meters < 1f)
                return "< 1 m";

            // Invariant culture: the UI is English, so "1.5 km" even on a Turkish-locale phone.
            return meters < 1000f
                ? $"{Mathf.RoundToInt(meters)} m"
                : (meters / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km";
        }

        public static string FormatWalkingTime(float meters)
        {
            int minutes = Mathf.CeilToInt(meters / WalkingSpeed / 60f);
            return minutes <= 1 ? "1 min" : $"{minutes} min";
        }

        /// <summary>e.g. "18 m · 1 min"</summary>
        public static string DescribeRoute(PathResult route) =>
            route != null && route.Success
                ? $"{FormatDistance(route.TotalDistance)} · {FormatWalkingTime(route.TotalDistance)}"
                : string.Empty;

        /// <summary>Detail text of a destination row: distance and time, "You are here" or "No route".</summary>
        public static string DescribeDestination(PathResult route)
        {
            if (route == null || !route.Success)
                return "No route";

            return route.TotalDistance < 1f ? "You are here" : DescribeRoute(route);
        }

        /// <summary>e.g. "12 m left · 33%"</summary>
        public static string FormatProgress(float remainingMeters, float progress) =>
            $"{FormatDistance(remainingMeters)} left · {Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";

        public static string RouteError(PathStatus status, string destinationName) => status switch
        {
            PathStatus.Unreachable => $"No walkable route to {destinationName}.",
            PathStatus.DestinationNotFound => $"{destinationName} is not on the campus map.",
            PathStatus.StartNotFound => "Your position is not on the campus map. Scan a marker first.",
            _ => string.Empty
        };

        public static string DestinationCaption(string destinationName) =>
            string.IsNullOrEmpty(destinationName) ? string.Empty : $"To {destinationName}";

        public static string ArrivedMessage(string destinationName) =>
            string.IsNullOrEmpty(destinationName) ? "You have arrived" : $"You have arrived at {destinationName}";

        /// <summary>
        /// Z rotation (degrees, counter-clockwise) for an up-pointing arrow icon so it
        /// shows the turn direction.
        /// </summary>
        public static float IconRotation(TurnDirection direction) => direction switch
        {
            TurnDirection.SlightLeft => 45f,
            TurnDirection.Left => 90f,
            TurnDirection.SharpLeft => 135f,
            TurnDirection.SlightRight => -45f,
            TurnDirection.Right => -90f,
            TurnDirection.SharpRight => -135f,
            TurnDirection.UTurn => 180f,
            _ => 0f
        };
    }
}

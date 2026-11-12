namespace CampusNavigation.Navigation
{
    public enum TurnDirection
    {
        Straight,
        SlightLeft,
        Left,
        SharpLeft,
        SlightRight,
        Right,
        SharpRight,
        UTurn,

        /// <summary>The next waypoint is the destination.</summary>
        Arrive
    }

    /// <summary>
    /// What the user should do next, e.g. "Turn left in 4 m".
    /// Produced by the navigation engine, displayed by the UI.
    /// </summary>
    public readonly struct NavigationInstruction
    {
        public TurnDirection Direction { get; }

        /// <summary>Meters from the user to the point where the instruction applies.</summary>
        public float Distance { get; }

        /// <summary>Ready-to-display text.</summary>
        public string Text { get; }

        public NavigationInstruction(TurnDirection direction, float distance, string text)
        {
            Direction = direction;
            Distance = distance;
            Text = text;
        }

        public static NavigationInstruction None => new(TurnDirection.Straight, 0f, string.Empty);

        public override string ToString() => Text;
    }
}

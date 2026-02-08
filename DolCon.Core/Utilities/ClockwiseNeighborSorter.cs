namespace DolCon.Core.Utilities;

/// <summary>
/// Sorts neighboring cells clockwise from north and assigns sequential selection numbers.
/// Blocked (marine) cells are included in the sort order but receive no selection number.
/// </summary>
public static class ClockwiseNeighborSorter
{
    public record NeighborInput(int CellId, double X, double Y, bool IsBlocked);

    public record NeighborEntry(int CellId, int? SelectionNumber, double AngleFromCenter);

    /// <summary>
    /// Sorts neighbors clockwise starting from north (top of screen).
    /// In screen coordinates where Y increases downward, clockwise means:
    /// North (top) → East (right) → South (bottom) → West (left).
    /// </summary>
    public static List<NeighborEntry> SortNeighborsClockwise(
        double centerX, double centerY,
        IReadOnlyList<NeighborInput> neighbors)
    {
        if (neighbors.Count == 0)
            return new List<NeighborEntry>();

        // Calculate angle from center to each neighbor.
        // Math.Atan2 returns angle in radians where:
        //   - 0 = East, PI/2 = South (in screen coords), PI = West, -PI/2 = North
        // We want clockwise from north, so we compute a "clockwise angle from north":
        //   clockwiseAngle = Atan2(dx, -dy)
        // This gives: North=0, East=PI/2, South=PI, West=3PI/2 (after normalization)
        var sorted = neighbors
            .Select(n =>
            {
                double dx = n.X - centerX;
                double dy = n.Y - centerY;
                // Atan2(dx, -dy) gives angle clockwise from north in screen coords
                double angle = Math.Atan2(dx, -dy);
                // Normalize to [0, 2*PI)
                if (angle < 0) angle += 2 * Math.PI;
                return (Neighbor: n, Angle: angle);
            })
            .OrderBy(x => x.Angle)
            .ToList();

        var result = new List<NeighborEntry>();
        int selectionNumber = 1;

        foreach (var (neighbor, angle) in sorted)
        {
            int? number = null;
            if (!neighbor.IsBlocked)
            {
                number = selectionNumber++;
            }
            result.Add(new NeighborEntry(neighbor.CellId, number, angle));
        }

        return result;
    }
}

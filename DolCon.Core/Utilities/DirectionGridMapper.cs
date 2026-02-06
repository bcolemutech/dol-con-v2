namespace DolCon.Core.Utilities;

using DolCon.Core.Enums;

/// <summary>
/// Maps 16-direction compass directions to a 3x3 grid layout for navigation UI.
/// Uses numpad-style key mapping where:
///   7 8 9   →   NW  N  NE
///   4 5 6   →   W   C  E
///   1 2 3   →   SW  S  SE
/// </summary>
public static class DirectionGridMapper
{
    /// <summary>
    /// Grid positions corresponding to numpad key layout.
    /// </summary>
    public enum GridPosition
    {
        SouthWest = 1,
        South = 2,
        SouthEast = 3,
        West = 4,
        Center = 5,
        East = 6,
        NorthWest = 7,
        North = 8,
        NorthEast = 9
    }

    /// <summary>
    /// Maps a 16-direction compass direction to an 8-position grid location.
    /// Intermediate directions (like NNE) are collapsed to the nearest cardinal/ordinal direction.
    /// </summary>
    /// <param name="direction">The 16-direction compass direction.</param>
    /// <returns>The corresponding grid position, or null for undefined directions.</returns>
    public static GridPosition? MapDirectionToGrid(Direction direction)
    {
        return direction switch
        {
            Direction.North or Direction.NorthNortheast or Direction.NorthNorthwest => GridPosition.North,
            Direction.Northeast or Direction.EastNortheast => GridPosition.NorthEast,
            Direction.East or Direction.EastSoutheast => GridPosition.East,
            Direction.Southeast or Direction.SouthSoutheast => GridPosition.SouthEast,
            Direction.South or Direction.SouthSouthwest => GridPosition.South,
            Direction.Southwest or Direction.WestSouthwest => GridPosition.SouthWest,
            Direction.West or Direction.WestNorthwest => GridPosition.West,
            Direction.Northwest => GridPosition.NorthWest,
            _ => null
        };
    }

    /// <summary>
    /// Gets the numpad key for a grid position.
    /// </summary>
    /// <param name="position">The grid position.</param>
    /// <returns>The numpad key (1-9).</returns>
    public static int GetNumpadKey(GridPosition position) => (int)position;

    /// <summary>
    /// Gets the grid position from a numpad key (1-9).
    /// </summary>
    /// <param name="numpadKey">The numpad key.</param>
    /// <returns>The corresponding grid position, or null if invalid.</returns>
    public static GridPosition? GetGridPosition(int numpadKey)
    {
        if (numpadKey < 1 || numpadKey > 9) return null;
        return (GridPosition)numpadKey;
    }

    /// <summary>
    /// Converts a grid position to a 0-based array index for a row-major 3x3 array.
    /// Layout:
    ///   0 1 2   →   NW  N  NE
    ///   3 4 5   →   W   C  E
    ///   6 7 8   →   SW  S  SE
    /// </summary>
    /// <param name="position">The grid position.</param>
    /// <returns>The array index (0-8).</returns>
    public static int GetArrayIndex(GridPosition position)
    {
        return position switch
        {
            GridPosition.NorthWest => 0,
            GridPosition.North => 1,
            GridPosition.NorthEast => 2,
            GridPosition.West => 3,
            GridPosition.Center => 4,
            GridPosition.East => 5,
            GridPosition.SouthWest => 6,
            GridPosition.South => 7,
            GridPosition.SouthEast => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(position))
        };
    }

    /// <summary>
    /// Gets the grid position from a 0-based array index.
    /// </summary>
    /// <param name="index">The array index (0-8).</param>
    /// <returns>The corresponding grid position, or null if invalid.</returns>
    public static GridPosition? GetGridPositionFromIndex(int index)
    {
        return index switch
        {
            0 => GridPosition.NorthWest,
            1 => GridPosition.North,
            2 => GridPosition.NorthEast,
            3 => GridPosition.West,
            4 => GridPosition.Center,
            5 => GridPosition.East,
            6 => GridPosition.SouthWest,
            7 => GridPosition.South,
            8 => GridPosition.SouthEast,
            _ => null
        };
    }
}

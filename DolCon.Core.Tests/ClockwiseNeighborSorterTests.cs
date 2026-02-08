namespace DolCon.Core.Tests;

using DolCon.Core.Utilities;
using FluentAssertions;

public class ClockwiseNeighborSorterTests
{
    [Fact]
    public void SortNeighborsClockwise_CardinalDirections_ReturnsClockwiseFromNorth()
    {
        // Arrange: neighbors at N, E, S, W (screen coords: Y increases downward)
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 0, -10, false),  // North (above center)
            new(2, 10, 0, false),   // East (right of center)
            new(3, 0, 10, false),   // South (below center)
            new(4, -10, 0, false),  // West (left of center)
        };

        // Act
        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        // Assert: clockwise from north = N, E, S, W
        result.Should().HaveCount(4);
        result[0].CellId.Should().Be(1); // North
        result[1].CellId.Should().Be(2); // East
        result[2].CellId.Should().Be(3); // South
        result[3].CellId.Should().Be(4); // West
    }

    [Fact]
    public void SortNeighborsClockwise_AssignsSequentialNumbers()
    {
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 0, -10, false),  // North
            new(2, 10, 0, false),   // East
            new(3, 0, 10, false),   // South
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        result[0].SelectionNumber.Should().Be(1);
        result[1].SelectionNumber.Should().Be(2);
        result[2].SelectionNumber.Should().Be(3);
    }

    [Fact]
    public void SortNeighborsClockwise_BlockedCells_GetNullSelectionNumber()
    {
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 0, -10, false),  // North - traversable
            new(2, 10, 0, true),    // East - blocked (marine)
            new(3, 0, 10, false),   // South - traversable
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        result.Should().HaveCount(3);
        result[0].SelectionNumber.Should().Be(1);  // North gets 1
        result[1].SelectionNumber.Should().BeNull(); // East (blocked) gets null
        result[2].SelectionNumber.Should().Be(2);  // South gets 2 (skips blocked)
    }

    [Fact]
    public void SortNeighborsClockwise_OrdinalDirections_CorrectOrder()
    {
        // NE, SE, SW, NW
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 10, -10, false),   // NE
            new(2, 10, 10, false),    // SE
            new(3, -10, 10, false),   // SW
            new(4, -10, -10, false),  // NW
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        // Clockwise from north: NE, SE, SW, NW
        result[0].CellId.Should().Be(1); // NE
        result[1].CellId.Should().Be(2); // SE
        result[2].CellId.Should().Be(3); // SW
        result[3].CellId.Should().Be(4); // NW
    }

    [Fact]
    public void SortNeighborsClockwise_MixedCardinalAndOrdinal_CorrectOrder()
    {
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 0, -10, false),    // N
            new(2, 10, -10, false),   // NE
            new(3, 10, 0, false),     // E
            new(4, 10, 10, false),    // SE
            new(5, 0, 10, false),     // S
            new(6, -10, 10, false),   // SW
            new(7, -10, 0, false),    // W
            new(8, -10, -10, false),  // NW
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        result[0].CellId.Should().Be(1); // N
        result[1].CellId.Should().Be(2); // NE
        result[2].CellId.Should().Be(3); // E
        result[3].CellId.Should().Be(4); // SE
        result[4].CellId.Should().Be(5); // S
        result[5].CellId.Should().Be(6); // SW
        result[6].CellId.Should().Be(7); // W
        result[7].CellId.Should().Be(8); // NW
    }

    [Fact]
    public void SortNeighborsClockwise_SingleNeighbor_ReturnsIt()
    {
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(42, 5, 3, false),
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        result.Should().HaveCount(1);
        result[0].CellId.Should().Be(42);
        result[0].SelectionNumber.Should().Be(1);
    }

    [Fact]
    public void SortNeighborsClockwise_EmptyList_ReturnsEmpty()
    {
        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0,
            new List<ClockwiseNeighborSorter.NeighborInput>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void SortNeighborsClockwise_AllBlocked_AllNullSelectionNumbers()
    {
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 0, -10, true),
            new(2, 10, 0, true),
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.SelectionNumber.Should().BeNull());
    }

    [Fact]
    public void SortNeighborsClockwise_NonZeroCenter_CalculatesCorrectly()
    {
        // Center at (100, 200), neighbors relative to that
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 100, 190, false),  // North (above center)
            new(2, 100, 210, false),  // South (below center)
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(100, 200, neighbors);

        result[0].CellId.Should().Be(1); // North first
        result[1].CellId.Should().Be(2); // South second
    }

    [Fact]
    public void SortNeighborsClockwise_ClusteredNeighbors_StillSortsCorrectly()
    {
        // Three neighbors all to the east, slightly different angles
        var neighbors = new List<ClockwiseNeighborSorter.NeighborInput>
        {
            new(1, 10, -2, false),   // Slightly NE
            new(2, 10, 0, false),    // Due East
            new(3, 10, 2, false),    // Slightly SE
        };

        var result = ClockwiseNeighborSorter.SortNeighborsClockwise(0, 0, neighbors);

        // Clockwise from north: slightly NE first, then E, then slightly SE
        result[0].CellId.Should().Be(1);
        result[1].CellId.Should().Be(2);
        result[2].CellId.Should().Be(3);
    }
}

namespace DolCon.Core.Tests;

using DolCon.Core.Enums;
using DolCon.Core.Utilities;
using FluentAssertions;
using static DolCon.Core.Utilities.DirectionGridMapper;

public class DirectionGridMapperTests
{
    [Theory]
    [InlineData(Direction.North, GridPosition.North)]
    [InlineData(Direction.NorthNortheast, GridPosition.North)]
    [InlineData(Direction.NorthNorthwest, GridPosition.North)]
    [InlineData(Direction.Northeast, GridPosition.NorthEast)]
    [InlineData(Direction.EastNortheast, GridPosition.NorthEast)]
    [InlineData(Direction.East, GridPosition.East)]
    [InlineData(Direction.EastSoutheast, GridPosition.East)]
    [InlineData(Direction.Southeast, GridPosition.SouthEast)]
    [InlineData(Direction.SouthSoutheast, GridPosition.SouthEast)]
    [InlineData(Direction.South, GridPosition.South)]
    [InlineData(Direction.SouthSouthwest, GridPosition.South)]
    [InlineData(Direction.Southwest, GridPosition.SouthWest)]
    [InlineData(Direction.WestSouthwest, GridPosition.SouthWest)]
    [InlineData(Direction.West, GridPosition.West)]
    [InlineData(Direction.WestNorthwest, GridPosition.West)]
    [InlineData(Direction.Northwest, GridPosition.NorthWest)]
    public void MapDirectionToGrid_MapsCorrectly(Direction direction, GridPosition expected)
    {
        MapDirectionToGrid(direction).Should().Be(expected);
    }

    [Fact]
    public void MapDirectionToGrid_UndefinedDirection_ReturnsNull()
    {
        MapDirectionToGrid(Direction.Undefined).Should().BeNull();
    }

    [Theory]
    [InlineData(7, GridPosition.NorthWest)]
    [InlineData(8, GridPosition.North)]
    [InlineData(9, GridPosition.NorthEast)]
    [InlineData(4, GridPosition.West)]
    [InlineData(5, GridPosition.Center)]
    [InlineData(6, GridPosition.East)]
    [InlineData(1, GridPosition.SouthWest)]
    [InlineData(2, GridPosition.South)]
    [InlineData(3, GridPosition.SouthEast)]
    public void GetGridPosition_ReturnsCorrectPosition(int key, GridPosition expected)
    {
        GetGridPosition(key).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public void GetGridPosition_InvalidKey_ReturnsNull(int key)
    {
        GetGridPosition(key).Should().BeNull();
    }

    [Theory]
    [InlineData(GridPosition.NorthWest, 7)]
    [InlineData(GridPosition.North, 8)]
    [InlineData(GridPosition.NorthEast, 9)]
    [InlineData(GridPosition.West, 4)]
    [InlineData(GridPosition.Center, 5)]
    [InlineData(GridPosition.East, 6)]
    [InlineData(GridPosition.SouthWest, 1)]
    [InlineData(GridPosition.South, 2)]
    [InlineData(GridPosition.SouthEast, 3)]
    public void GetNumpadKey_ReturnsCorrectKey(GridPosition position, int expected)
    {
        GetNumpadKey(position).Should().Be(expected);
    }

    [Theory]
    [InlineData(GridPosition.NorthWest, 0)]
    [InlineData(GridPosition.North, 1)]
    [InlineData(GridPosition.NorthEast, 2)]
    [InlineData(GridPosition.West, 3)]
    [InlineData(GridPosition.Center, 4)]
    [InlineData(GridPosition.East, 5)]
    [InlineData(GridPosition.SouthWest, 6)]
    [InlineData(GridPosition.South, 7)]
    [InlineData(GridPosition.SouthEast, 8)]
    public void GetArrayIndex_ReturnsCorrectIndex(GridPosition position, int expected)
    {
        GetArrayIndex(position).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, GridPosition.NorthWest)]
    [InlineData(1, GridPosition.North)]
    [InlineData(2, GridPosition.NorthEast)]
    [InlineData(3, GridPosition.West)]
    [InlineData(4, GridPosition.Center)]
    [InlineData(5, GridPosition.East)]
    [InlineData(6, GridPosition.SouthWest)]
    [InlineData(7, GridPosition.South)]
    [InlineData(8, GridPosition.SouthEast)]
    public void GetGridPositionFromIndex_ReturnsCorrectPosition(int index, GridPosition expected)
    {
        GetGridPositionFromIndex(index).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void GetGridPositionFromIndex_InvalidIndex_ReturnsNull(int index)
    {
        GetGridPositionFromIndex(index).Should().BeNull();
    }
}

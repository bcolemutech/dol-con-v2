namespace DolCon.Tests;

using System.Drawing;
using Enums;
using FluentAssertions;
using Models.BaseTypes;
using Services;

public class MapServiceTests
{
    [Theory]
    [InlineData(4, 6, 4, 4, Direction.North)]
    [InlineData(4, 4, 4, 6, Direction.South)]
    [InlineData(4, 4, 6, 4, Direction.East)]
    [InlineData(9, 3, 4, 3, Direction.West)]
    [InlineData(4, 4, 6, 6, Direction.Southeast)]
    [InlineData(4, 4, 6, 2, Direction.Northeast)]
    [InlineData(4, 4, 2, 6, Direction.Southwest)]
    [InlineData(4, 4, 2, 2, Direction.Northwest)]
    [InlineData(4, 6, 6, 2, Direction.NorthNortheast)]
    [InlineData(4, 6, 6, 5, Direction.EastNortheast)]
    [InlineData(2, 2, 8, 5, Direction.EastSoutheast)]
    [InlineData(2, 2, 4, 6, Direction.SouthSoutheast)]
    [InlineData(6, 0, 4, 4, Direction.SouthSouthwest)]
    [InlineData(6, 0, 2, 2, Direction.WestSouthwest)]
    [InlineData(2, 2, 0, 1, Direction.WestNorthwest)]
    [InlineData(2, 2, 1, 0, Direction.NorthNorthwest)]
    public void WhenGetDirectionReturnsProperDirection(double x1, double y1, double x2, double y2, Direction expected)
    {
        var actual = MapService.GetDirection(x1, y1, x2, y2);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, BurgSize.Town, 2.4)]
    [InlineData(100, BurgSize.Megalopolis, 450)]
    [InlineData(.5, BurgSize.Village, .1)]
    [InlineData(71, BurgSize.Megalopolis, 331.2)]
    [InlineData(3.5, BurgSize.Town, 14.18)]
    [InlineData(7.6, BurgSize.Town, 33.44)]
    public void GivenABurgWithAPopulationThenExpectSizeAndPopulationToBeAdjusted(double burgPop, BurgSize size,
        double near)
    {
        var burg = new Burg()
        {
            population = burgPop,
            isCityOfLight = false,
        };

        MapService.AdjustBurgSize(burg, .5);

        burg.size.Should().Be(size);
        burg.population.Should().BeApproximately(near, .1);
    }
}

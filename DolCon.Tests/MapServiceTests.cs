namespace DolCon.Tests;

using System.Drawing;
using Enums;
using FluentAssertions;
using Services;

public class MapServiceTests
{
    [Fact]
    public void WhenGetDirectionReturnsProperDirection()
    {
        var destination = new Point((int)426.95, (int)82.37);
        var origin = new Point((int)519.37, (int)164.33);

        var actual = MapService.GetDirection(origin, destination);

        actual.Should().Be(Direction.Northwest);
    }
}

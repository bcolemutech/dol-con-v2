namespace DolCon.Core.Tests.World;

using DolCon.Core.Enums;
using DolCon.Core.Models.BaseTypes;
using DolCon.Core.Services;
using FluentAssertions;

public class WorldProvisioningServiceTests
{
    [Theory]
    [InlineData(1, BurgSize.Town, 2.4)]
    [InlineData(100, BurgSize.Megalopolis, 450)]
    [InlineData(.5, BurgSize.Village, .1)]
    [InlineData(71, BurgSize.Megalopolis, 331.2)]
    [InlineData(3.5, BurgSize.Town, 14.18)]
    [InlineData(7.6, BurgSize.Town, 33.44)]
    public void AdjustBurgSize_AdjustsSizeAndPopulation(double burgPop, BurgSize size, double near)
    {
        var burg = new Burg { population = burgPop, isCityOfLight = false };

        WorldProvisioningService.AdjustBurgSize(burg, .5);

        burg.size.Should().Be(size);
        burg.population.Should().BeApproximately(near, .1);
    }

    [Fact]
    public void CalculateChallengeRating_ScalesWithDistance()
    {
        var cell = new Cell { p = new List<double> { 50, 50 } };

        var actual = WorldProvisioningService.CalculateChallengeRating(cell, 25, 25, 50);

        actual.Should().Be(14.125);
    }

    [Fact]
    public void CalculateChallengeRating_BeyondCrDistance_KeepsScaling()
    {
        var cell = new Cell { p = new List<double> { 75, 75 } };

        var actual = WorldProvisioningService.CalculateChallengeRating(cell, 25, 25, 50);

        actual.Should().Be(28.25);
    }
}

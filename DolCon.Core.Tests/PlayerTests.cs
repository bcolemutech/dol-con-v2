namespace DolCon.Core.Tests;

using FluentAssertions;
using Models;

public class PlayerTests
{
    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(10, 0, 1, 0)]
    [InlineData(100, 0, 10, 0)]
    [InlineData(1000, 0, 0, 1)]
    [InlineData(1001, 1, 0, 1)]
    [InlineData(1010, 0, 1, 1)]
    [InlineData(1100, 0, 10, 1)]
    [InlineData(1110, 0, 11, 1)]
    [InlineData(1111, 1, 11, 1)]
    [InlineData(10000, 0, 0, 10)]
    [InlineData(100000, 0, 0, 100)]
    [InlineData(1000000, 0, 0, 1000)]
    [InlineData(10000000, 0, 0, 10000)]
    [InlineData(100000000, 0, 0, 100000)]
    [InlineData(1000000000, 0, 0, 1000000)]
    [InlineData(10000000000, 0, 0, 10000000)]
    [InlineData(100000000000, 0, 0, 100000000)]
    [InlineData(1000000000000, 0, 0, 1000000000)]
    public void GivenANumberOfCoinCalculateTheCorrectAmountOfEachCurrency(long coin, long copper, long silver, long gold)
    {
        var player = new Player { coin = coin };

        player.copper.Should().Be(copper);
        player.silver.Should().Be(silver);
        player.gold.Should().Be(gold);
    }
    
}

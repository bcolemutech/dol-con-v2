﻿namespace DolCon.Tests;

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
    [InlineData(760, 0, 76, 0)]
    public void GivenANumberOfCoinCalculateTheCorrectAmountOfEachCurrency(int coin, int copper, int silver, int gold)
    {
        var player = new Player { coin = coin };

        player.copper.Should().Be(copper);
        player.silver.Should().Be(silver);
        player.gold.Should().Be(gold);
    }
    
}

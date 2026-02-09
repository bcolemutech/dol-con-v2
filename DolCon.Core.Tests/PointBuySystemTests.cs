using DolCon.Core.Models;
using DolCon.Core.Services;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class PointBuySystemTests
{
    [Theory]
    [InlineData(8, 0)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(12, 4)]
    [InlineData(13, 5)]
    [InlineData(14, 7)]
    [InlineData(15, 9)]
    public void GetCost_returns_correct_cost_for_score(int score, int expectedCost)
    {
        PointBuySystem.GetCost(score).Should().Be(expectedCost);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(16)]
    [InlineData(-1)]
    public void GetCost_throws_for_out_of_range_score(int score)
    {
        var act = () => PointBuySystem.GetCost(score);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetTotalCost_returns_zero_for_all_eights()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 8, Dexterity = 8, Constitution = 8,
            Intelligence = 8, Wisdom = 8, Charisma = 8
        };

        PointBuySystem.GetTotalCost(abilities).Should().Be(0);
    }

    [Fact]
    public void GetTotalCost_returns_correct_sum()
    {
        // 15(9) + 14(7) + 13(5) + 12(4) + 10(2) + 8(0) = 27
        var abilities = new PlayerAbilities
        {
            Strength = 15, Dexterity = 14, Constitution = 13,
            Intelligence = 12, Wisdom = 10, Charisma = 8
        };

        PointBuySystem.GetTotalCost(abilities).Should().Be(27);
    }

    [Fact]
    public void GetRemainingPoints_returns_27_for_all_eights()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 8, Dexterity = 8, Constitution = 8,
            Intelligence = 8, Wisdom = 8, Charisma = 8
        };

        PointBuySystem.GetRemainingPoints(abilities).Should().Be(27);
    }

    [Fact]
    public void GetRemainingPoints_returns_zero_when_all_points_spent()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 15, Dexterity = 14, Constitution = 13,
            Intelligence = 12, Wisdom = 10, Charisma = 8
        };

        PointBuySystem.GetRemainingPoints(abilities).Should().Be(0);
    }

    [Fact]
    public void IsValid_returns_true_when_all_points_spent()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 15, Dexterity = 14, Constitution = 13,
            Intelligence = 12, Wisdom = 10, Charisma = 8
        };

        PointBuySystem.IsValid(abilities).Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_false_when_points_remain()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 8, Dexterity = 8, Constitution = 8,
            Intelligence = 8, Wisdom = 8, Charisma = 8
        };

        PointBuySystem.IsValid(abilities).Should().BeFalse();
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(14, true)]
    [InlineData(15, false)]
    public void CanIncrease_returns_expected(int score, bool expected)
    {
        PointBuySystem.CanIncrease(score).Should().Be(expected);
    }

    [Theory]
    [InlineData(8, false)]
    [InlineData(9, true)]
    [InlineData(15, true)]
    public void CanDecrease_returns_expected(int score, bool expected)
    {
        PointBuySystem.CanDecrease(score).Should().Be(expected);
    }

    [Fact]
    public void Constants_have_expected_values()
    {
        PointBuySystem.TotalPoints.Should().Be(27);
        PointBuySystem.MinScore.Should().Be(8);
        PointBuySystem.MaxScore.Should().Be(15);
    }

    [Fact]
    public void CanAffordIncrease_returns_false_when_not_enough_points()
    {
        // All 15s except one - spend 45 points (impossible, but test the check)
        // Use a scenario: 5 scores at 13 (cost 25), 1 at 10 (cost 2) = 27 total, 0 remaining
        var abilities = new PlayerAbilities
        {
            Strength = 13, Dexterity = 13, Constitution = 13,
            Intelligence = 13, Wisdom = 13, Charisma = 10
        };

        PointBuySystem.CanAffordIncrease(abilities, 10).Should().BeFalse();
    }

    [Fact]
    public void CanAffordIncrease_returns_true_when_enough_points()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 8, Dexterity = 8, Constitution = 8,
            Intelligence = 8, Wisdom = 8, Charisma = 8
        };

        // Cost to go from 8 to 9 is 1, we have 27 remaining
        PointBuySystem.CanAffordIncrease(abilities, 8).Should().BeTrue();
    }

    [Fact]
    public void CanAffordIncrease_returns_false_at_max_score()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 15, Dexterity = 8, Constitution = 8,
            Intelligence = 8, Wisdom = 8, Charisma = 8
        };

        PointBuySystem.CanAffordIncrease(abilities, 15).Should().BeFalse();
    }
}

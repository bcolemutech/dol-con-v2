using DolCon.Core.Enums;
using DolCon.Core.Models.Combat;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class EnemyTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(0.125, 25)]
    [InlineData(0.25, 50)]
    [InlineData(0.5, 100)]
    [InlineData(1, 200)]
    [InlineData(2, 450)]
    [InlineData(3, 700)]
    [InlineData(5, 1800)]
    [InlineData(10, 5900)]
    public void CalculateExperienceValue_StandardCRs_ReturnsCorrectXP(double cr, int expectedXP)
    {
        // Arrange
        var enemy = new Enemy
        {
            Name = "Test Enemy",
            ChallengeRating = cr
        };

        // Act
        enemy.CalculateExperienceValue();

        // Assert
        enemy.ExperienceValue.Should().Be(expectedXP);
    }

    [Fact]
    public void CalculateExperienceValue_HighCR_ReturnsScaledXP()
    {
        // Arrange - CR 12 enemy (higher than the explicit cases)
        var enemy = new Enemy
        {
            Name = "High CR Enemy",
            ChallengeRating = 12
        };

        // Act
        enemy.CalculateExperienceValue();

        // Assert - Should use the scaling formula, not return 0
        enemy.ExperienceValue.Should().BeGreaterThan(0);
        // CR 12 should be roughly 5900 * 1.5^2 = 13275
        enemy.ExperienceValue.Should().BeGreaterThan(10000);
    }

    [Fact]
    public void CalculateExperienceValue_VeryHighCR_ReturnsNonZeroXP()
    {
        // Arrange - Very high CR enemy
        var enemy = new Enemy
        {
            Name = "Boss Enemy",
            ChallengeRating = 20
        };

        // Act
        enemy.CalculateExperienceValue();

        // Assert - Should use scaling formula and return a large value
        enemy.ExperienceValue.Should().BeGreaterThan(0);
        enemy.ExperienceValue.Should().BeGreaterThan(50000);
    }

    [Fact]
    public void CalculateExperienceValue_DoesNotDependOnPreviousValue()
    {
        // Arrange - Enemy with ExperienceValue set to 0 initially
        var enemy = new Enemy
        {
            Name = "Test Enemy",
            ChallengeRating = 15, // Non-standard CR
            ExperienceValue = 0 // Explicitly 0
        };

        // Act
        enemy.CalculateExperienceValue();

        // Assert - Should calculate based on CR, not multiply by 0
        enemy.ExperienceValue.Should().BeGreaterThan(0);
    }
}

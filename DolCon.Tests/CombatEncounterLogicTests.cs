namespace DolCon.Tests;

using FluentAssertions;

/// <summary>
/// Tests for combat encounter logic including CR calculations and roll-based modifiers.
/// These tests verify the core combat encounter algorithm independently of the full EventService.
/// </summary>
public class CombatEncounterLogicTests
{
    #region Challenge Rating Modifier Tests

    [Theory]
    [InlineData(1, false, 10.0, 10.0)]
    [InlineData(2, false, 10.0, 10.0)]
    [InlineData(3, false, 10.0, 10.0)]
    [InlineData(4, false, 10.0, 10.0)]
    [InlineData(5, false, 10.0, 10.0)]
    public void CombatEncounter_WhenRollIsFiveOrLess_NoEncounterOccursAndCRUnchanged(
        int roll, bool expectedEncounter, double baseCR, double expectedCR)
    {
        // Act
        var (encounterOccurred, actualRoll, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().Be(expectedEncounter,
            $"roll of {roll} should not trigger combat encounter");
        actualRoll.Should().Be(roll);
        adjustedCR.Should().Be(expectedCR,
            "CR should remain unchanged when no encounter occurs");
    }

    [Theory]
    [InlineData(6, true, 10.0, 10.0)]
    [InlineData(7, true, 10.0, 10.0)]
    [InlineData(8, true, 10.0, 10.0)]
    [InlineData(9, true, 10.0, 10.0)]
    [InlineData(10, true, 10.0, 10.0)]
    public void CombatEncounter_WhenRollIsSixToTen_EncounterOccursWithBaseCR(
        int roll, bool expectedEncounter, double baseCR, double expectedCR)
    {
        // Act
        var (encounterOccurred, actualRoll, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().Be(expectedEncounter,
            $"roll of {roll} should trigger combat encounter");
        actualRoll.Should().Be(roll);
        adjustedCR.Should().Be(expectedCR,
            $"roll of {roll} should use base CR without modifier");
    }

    [Theory]
    [InlineData(11, true, 10.0, 11.0)]  // 10 * 1.10
    [InlineData(12, true, 10.0, 11.0)]
    [InlineData(13, true, 10.0, 11.0)]
    [InlineData(14, true, 10.0, 11.0)]
    [InlineData(15, true, 10.0, 11.0)]
    public void CombatEncounter_WhenRollIsElevenToFifteen_EncounterOccursWith10PercentBonus(
        int roll, bool expectedEncounter, double baseCR, double expectedCR)
    {
        // Act
        var (encounterOccurred, actualRoll, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().Be(expectedEncounter,
            $"roll of {roll} should trigger combat encounter");
        actualRoll.Should().Be(roll);
        adjustedCR.Should().Be(expectedCR,
            $"roll of {roll} should apply +10% CR modifier");
    }

    [Theory]
    [InlineData(16, true, 10.0, 11.5)]  // 10 * 1.15
    [InlineData(17, true, 10.0, 11.5)]
    [InlineData(18, true, 10.0, 11.5)]
    [InlineData(19, true, 10.0, 11.5)]
    public void CombatEncounter_WhenRollIsSixteenToNineteen_EncounterOccursWith15PercentBonus(
        int roll, bool expectedEncounter, double baseCR, double expectedCR)
    {
        // Act
        var (encounterOccurred, actualRoll, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().Be(expectedEncounter,
            $"roll of {roll} should trigger combat encounter");
        actualRoll.Should().Be(roll);
        adjustedCR.Should().Be(expectedCR,
            $"roll of {roll} should apply +15% CR modifier");
    }

    [Fact]
    public void CombatEncounter_WhenRollIsTwenty_EncounterOccursWith20PercentBonus()
    {
        // Arrange
        const int roll = 20;
        const double baseCR = 10.0;
        const double expectedCR = 12.0; // 10 * 1.20

        // Act
        var (encounterOccurred, actualRoll, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().BeTrue("roll of 20 should trigger combat encounter");
        actualRoll.Should().Be(roll);
        adjustedCR.Should().Be(expectedCR, "roll of 20 should apply +20% CR modifier (critical hit)");
    }

    #endregion

    #region Challenge Rating Edge Cases

    [Theory]
    [InlineData(0.5, 6, 0.5)]    // Low CR, base roll
    [InlineData(0.5, 11, 0.55)]  // Low CR with 10% bonus
    [InlineData(0.5, 16, 0.575)] // Low CR with 15% bonus
    [InlineData(0.5, 20, 0.6)]   // Low CR with 20% bonus
    public void CombatEncounter_WithLowChallengeRating_CalculatesCorrectModifiers(
        double baseCR, int roll, double expectedCR)
    {
        // Act
        var (encounterOccurred, _, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().BeTrue();
        adjustedCR.Should().BeApproximately(expectedCR, 0.001);
    }

    [Theory]
    [InlineData(100.0, 6, 100.0)]   // High CR, base roll
    [InlineData(100.0, 11, 110.0)]  // High CR with 10% bonus
    [InlineData(100.0, 16, 115.0)]  // High CR with 15% bonus
    [InlineData(100.0, 20, 120.0)]  // High CR with 20% bonus
    public void CombatEncounter_WithHighChallengeRating_CalculatesCorrectModifiers(
        double baseCR, int roll, double expectedCR)
    {
        // Act
        var (encounterOccurred, _, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().BeTrue();
        adjustedCR.Should().BeApproximately(expectedCR, 0.001);
    }

    [Theory]
    [InlineData(1.0, 6, 1.0)]
    [InlineData(1.0, 11, 1.1)]
    [InlineData(1.0, 16, 1.15)]
    [InlineData(1.0, 20, 1.2)]
    public void CombatEncounter_WithDefaultChallengeRating_CalculatesCorrectModifiers(
        double baseCR, int roll, double expectedCR)
    {
        // Act
        var (encounterOccurred, _, adjustedCR) = SimulateCombatEncounter(baseCR, roll);

        // Assert
        encounterOccurred.Should().BeTrue();
        adjustedCR.Should().BeApproximately(expectedCR, 0.001);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Simulates the combat encounter logic from EventService.RollForCombatEncounter
    /// This mirrors the actual implementation for testing purposes.
    /// </summary>
    private static (bool encounterOccurred, int roll, double adjustedCR) SimulateCombatEncounter(
        double baseChallengeRating, int mockRoll)
    {
        bool encounterOccurred = mockRoll > 5;
        double adjustedCR = baseChallengeRating;

        if (encounterOccurred)
        {
            adjustedCR = mockRoll switch
            {
                20 => baseChallengeRating * 1.20,      // +20%
                >= 16 => baseChallengeRating * 1.15,   // +15%
                >= 11 => baseChallengeRating * 1.10,   // +10%
                _ => baseChallengeRating
            };
        }

        return (encounterOccurred, mockRoll, adjustedCR);
    }

    #endregion
}

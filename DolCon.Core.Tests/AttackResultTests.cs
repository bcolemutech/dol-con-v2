using DolCon.Core.Models.Combat;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class AttackResultTests
{
    [Fact]
    public void GetAttackFormula_FormatsCorrectly_WithPositiveModifier()
    {
        // Arrange
        var result = new AttackResult
        {
            D20Roll = 15,
            AttackModifier = 3,
            TotalAttack = 18,
            TargetAC = 14
        };

        // Act
        var formula = result.GetAttackFormula();

        // Assert
        formula.Should().Be("d20(15) + 3 = 18 vs AC 14");
    }

    [Fact]
    public void GetAttackFormula_FormatsCorrectly_WithNegativeModifier()
    {
        // Arrange
        var result = new AttackResult
        {
            D20Roll = 12,
            AttackModifier = -1,
            TotalAttack = 11,
            TargetAC = 12
        };

        // Act
        var formula = result.GetAttackFormula();

        // Assert
        formula.Should().Be("d20(12) - 1 = 11 vs AC 12");
    }

    [Fact]
    public void GetAttackFormula_FormatsCorrectly_WithZeroModifier()
    {
        // Arrange
        var result = new AttackResult
        {
            D20Roll = 10,
            AttackModifier = 0,
            TotalAttack = 10,
            TargetAC = 10
        };

        // Act
        var formula = result.GetAttackFormula();

        // Assert
        formula.Should().Be("d20(10) + 0 = 10 vs AC 10");
    }

    [Fact]
    public void GetDamageFormula_ReturnsEmpty_OnMiss()
    {
        // Arrange
        var result = new AttackResult { IsHit = false };

        // Act
        var formula = result.GetDamageFormula();

        // Assert
        formula.Should().BeEmpty();
    }

    [Fact]
    public void GetDamageFormula_ShowsBaseDamageOnly_WhenNoBonus()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = true,
            BaseDamage = 4,
            BonusDamage = 0,
            TotalDamage = 4
        };

        // Act
        var formula = result.GetDamageFormula();

        // Assert
        formula.Should().Be("4 base = 4");
    }

    [Fact]
    public void GetDamageFormula_ShowsBaseAndBonus_WhenBonusPresent()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = true,
            BaseDamage = 4,
            BonusDamage = 2,
            TotalDamage = 6
        };

        // Act
        var formula = result.GetDamageFormula();

        // Assert
        formula.Should().Be("4 base + 2 bonus = 6");
    }

    [Fact]
    public void GetDamageFormula_ShowsCritical_OnNatural20()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = true,
            IsCritical = true,
            BaseDamage = 4,
            BonusDamage = 2,
            TotalDamage = 12
        };

        // Act
        var formula = result.GetDamageFormula();

        // Assert
        formula.Should().Contain("x2 CRIT");
        formula.Should().Contain("= 12");
    }

    [Fact]
    public void GetResultSummary_ShowsNatural1()
    {
        // Arrange
        var result = new AttackResult { IsNatural1 = true, IsHit = false };

        // Act
        var summary = result.GetResultSummary();

        // Assert
        summary.Should().Contain("Natural 1");
        summary.Should().Contain("MISS");
    }

    [Fact]
    public void GetResultSummary_ShowsCriticalHit()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = true,
            IsCritical = true,
            TotalDamage = 12
        };

        // Act
        var summary = result.GetResultSummary();

        // Assert
        summary.Should().Contain("CRITICAL HIT");
        summary.Should().Contain("12 damage");
    }

    [Fact]
    public void GetResultSummary_ShowsHit()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = true,
            IsCritical = false,
            TotalDamage = 6
        };

        // Act
        var summary = result.GetResultSummary();

        // Assert
        summary.Should().Contain("HIT");
        summary.Should().Contain("6 damage");
    }

    [Fact]
    public void GetResultSummary_ShowsMiss()
    {
        // Arrange
        var result = new AttackResult
        {
            IsHit = false,
            IsNatural1 = false
        };

        // Act
        var summary = result.GetResultSummary();

        // Assert
        summary.Should().Contain("MISS");
        summary.Should().NotContain("Natural 1");
    }
}

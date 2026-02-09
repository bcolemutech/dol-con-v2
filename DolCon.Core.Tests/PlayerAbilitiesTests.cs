using DolCon.Core.Models;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class PlayerAbilitiesTests
{
    [Fact]
    public void NewPlayerAbilities_AllValuesDefaultToTen()
    {
        var abilities = new PlayerAbilities();

        abilities.Strength.Should().Be(10);
        abilities.Dexterity.Should().Be(10);
        abilities.Constitution.Should().Be(10);
        abilities.Intelligence.Should().Be(10);
        abilities.Wisdom.Should().Be(10);
        abilities.Charisma.Should().Be(10);
    }

    [Fact]
    public void PlayerAbilities_CanBeModified()
    {
        var abilities = new PlayerAbilities
        {
            Strength = 18,
            Dexterity = 14,
            Constitution = 16,
            Intelligence = 8,
            Wisdom = 12,
            Charisma = 10
        };

        abilities.Strength.Should().Be(18);
        abilities.Dexterity.Should().Be(14);
        abilities.Constitution.Should().Be(16);
        abilities.Intelligence.Should().Be(8);
        abilities.Wisdom.Should().Be(12);
        abilities.Charisma.Should().Be(10);
    }
}

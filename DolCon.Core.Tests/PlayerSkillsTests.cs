using DolCon.Core.Models;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class PlayerSkillsTests
{
    [Fact]
    public void NewPlayerSkills_AllValuesDefaultToZero()
    {
        var skills = new PlayerSkills();

        skills.Unarmed.Should().Be(0.0);
        skills.OneHanded.Should().Be(0.0);
        skills.TwoHanded.Should().Be(0.0);
        skills.Armor.Should().Be(0.0);
        skills.Shield.Should().Be(0.0);
        skills.LightAptitude.Should().Be(0.0);
    }
}

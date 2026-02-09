namespace DolCon.Core.Tests;

using System.Text.Json;
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

    [Fact]
    public void NewPlayer_HasNonNullSkillsWithZeroValues()
    {
        var player = new Player();

        player.Skills.Should().NotBeNull();
        player.Skills.Unarmed.Should().Be(0.0);
        player.Skills.OneHanded.Should().Be(0.0);
        player.Skills.TwoHanded.Should().Be(0.0);
        player.Skills.Armor.Should().Be(0.0);
        player.Skills.Shield.Should().Be(0.0);
        player.Skills.LightAptitude.Should().Be(0.0);
    }

    [Fact]
    public void Player_SkillsSerializeAndDeserialize_WithSystemTextJson()
    {
        var player = new Player { Name = "Test" };
        player.Skills.OneHanded = 5.5;
        player.Skills.LightAptitude = 12.3;

        var json = JsonSerializer.Serialize(player);
        var deserialized = JsonSerializer.Deserialize<Player>(json);

        deserialized!.Skills.OneHanded.Should().Be(5.5);
        deserialized.Skills.LightAptitude.Should().Be(12.3);
        deserialized.Skills.Unarmed.Should().Be(0.0);
    }

    [Fact]
    public void Player_DeserializeWithoutSkills_DefaultsToZero()
    {
        var json = """{"Id":"00000000-0000-0000-0000-000000000001","Name":"Old","coin":0,"Inventory":[]}""";

        var player = JsonSerializer.Deserialize<Player>(json);

        player!.Skills.Should().NotBeNull();
        player.Skills.Unarmed.Should().Be(0.0);
        player.Skills.LightAptitude.Should().Be(0.0);
    }

    [Fact]
    public void NewPlayer_HasNonNullAbilitiesWithDefaultValues()
    {
        var player = new Player();

        player.Abilities.Should().NotBeNull();
        player.Abilities.Strength.Should().Be(10);
        player.Abilities.Dexterity.Should().Be(10);
        player.Abilities.Constitution.Should().Be(10);
        player.Abilities.Intelligence.Should().Be(10);
        player.Abilities.Wisdom.Should().Be(10);
        player.Abilities.Charisma.Should().Be(10);
    }

    [Fact]
    public void Player_AbilitiesSerializeAndDeserialize_WithSystemTextJson()
    {
        var player = new Player { Name = "Test" };
        player.Abilities.Strength = 18;
        player.Abilities.Dexterity = 14;

        var json = JsonSerializer.Serialize(player);
        var deserialized = JsonSerializer.Deserialize<Player>(json);

        deserialized!.Abilities.Strength.Should().Be(18);
        deserialized.Abilities.Dexterity.Should().Be(14);
        deserialized.Abilities.Constitution.Should().Be(10);
    }

    [Fact]
    public void Player_DeserializeWithoutAbilities_DefaultsToTen()
    {
        var json = """{"Id":"00000000-0000-0000-0000-000000000001","Name":"Old","coin":0,"Inventory":[]}""";

        var player = JsonSerializer.Deserialize<Player>(json);

        player!.Abilities.Should().NotBeNull();
        player.Abilities.Strength.Should().Be(10);
        player.Abilities.Charisma.Should().Be(10);
    }

    [Fact]
    public void Player_DeserializeWithExplicitNullAbilities_DefaultsToTen()
    {
        var json = """{"Id":"00000000-0000-0000-0000-000000000001","Name":"Old","coin":0,"Inventory":[],"Abilities":null}""";

        var player = JsonSerializer.Deserialize<Player>(json);

        player!.Abilities.Should().NotBeNull();
        player.Abilities.Strength.Should().Be(10);
        player.Abilities.Charisma.Should().Be(10);
    }
}

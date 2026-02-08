using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.Combat;
using DolCon.Core.Services;
using FluentAssertions;

namespace DolCon.Core.Tests;

public class SkillServiceTests
{
    private readonly SkillService _skillService = new();

    private static Player CreatePlayer(Equipment weaponSlot = Equipment.None, bool hasArmor = false,
        bool hasShield = false)
    {
        var player = new Player { Name = "Test" };

        if (weaponSlot is Equipment.OneHanded or Equipment.TwoHanded)
        {
            player.Inventory.Add(new Item
            {
                Name = "Test Weapon",
                Equipment = weaponSlot,
                Equipped = true,
                Tags = [new Tag("Weapon", TagType.Good)]
            });
        }

        if (hasArmor)
        {
            player.Inventory.Add(new Item
            {
                Name = "Test Armor",
                Equipment = Equipment.Body,
                Equipped = true,
                Tags = [new Tag("Armor", TagType.Good)]
            });
        }

        if (hasShield)
        {
            player.Inventory.Add(new Item
            {
                Name = "Test Shield",
                Equipment = Equipment.Shield,
                Equipped = true,
                Tags = [new Tag("Armor", TagType.Good)]
            });
        }

        return player;
    }

    private static CombatState CreateVictoryState(EnemyCategory category = EnemyCategory.Human, int xp = 200)
    {
        var state = new CombatState
        {
            Result = CombatResult.Victory,
            TotalXPEarned = xp
        };
        state.Enemies.Add(new Enemy { Category = category });
        return state;
    }

    [Fact]
    public void ApplySkillGains_WithNoWeapon_IncrementsUnarmed()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.Unarmed.Should().BeGreaterThan(0.0);
        player.Skills.OneHanded.Should().Be(0.0);
        player.Skills.TwoHanded.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_WithOneHandedWeapon_IncrementsOneHanded()
    {
        var player = CreatePlayer(Equipment.OneHanded);
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.OneHanded.Should().BeGreaterThan(0.0);
        player.Skills.Unarmed.Should().Be(0.0);
        player.Skills.TwoHanded.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_WithTwoHandedWeapon_IncrementsTwoHanded()
    {
        var player = CreatePlayer(Equipment.TwoHanded);
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.TwoHanded.Should().BeGreaterThan(0.0);
        player.Skills.Unarmed.Should().Be(0.0);
        player.Skills.OneHanded.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_WithArmorEquipped_IncrementsArmor()
    {
        var player = CreatePlayer(hasArmor: true);
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.Armor.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ApplySkillGains_WithShieldEquipped_IncrementsShield()
    {
        var player = CreatePlayer(hasShield: true);
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.Shield.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ApplySkillGains_WithNoArmorOrShield_DoesNotIncrementArmorOrShield()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState();

        _skillService.ApplySkillGains(player, state);

        player.Skills.Armor.Should().Be(0.0);
        player.Skills.Shield.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_VsUndeadEnemy_IncrementsLightAptitudeSmall()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState(EnemyCategory.Undead);

        _skillService.ApplySkillGains(player, state);

        player.Skills.LightAptitude.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public void ApplySkillGains_VsDemonEnemy_IncrementsLightAptitudeMore()
    {
        var playerVsUndead = CreatePlayer();
        var playerVsDemon = CreatePlayer();
        var undeadState = CreateVictoryState(EnemyCategory.Undead);
        var demonState = CreateVictoryState(EnemyCategory.Demon);

        _skillService.ApplySkillGains(playerVsUndead, undeadState);
        _skillService.ApplySkillGains(playerVsDemon, demonState);

        playerVsDemon.Skills.LightAptitude.Should().BeGreaterThan(playerVsUndead.Skills.LightAptitude);
    }

    [Fact]
    public void ApplySkillGains_VsNatureEnemy_DoesNotIncrementLightAptitude()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState(EnemyCategory.Nature);

        _skillService.ApplySkillGains(player, state);

        player.Skills.LightAptitude.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_VsHumanEnemy_DoesNotIncrementLightAptitude()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState(EnemyCategory.Human);

        _skillService.ApplySkillGains(player, state);

        player.Skills.LightAptitude.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_HigherXp_ProducesLargerGain()
    {
        var playerLowXp = CreatePlayer();
        var playerHighXp = CreatePlayer();
        var lowXpState = CreateVictoryState(xp: 50);
        var highXpState = CreateVictoryState(xp: 1000);

        _skillService.ApplySkillGains(playerLowXp, lowXpState);
        _skillService.ApplySkillGains(playerHighXp, highXpState);

        playerHighXp.Skills.Unarmed.Should().BeGreaterThan(playerLowXp.Skills.Unarmed);
    }

    [Fact]
    public void ApplySkillGains_NonVictory_DoesNotIncrementSkills()
    {
        var player = CreatePlayer();
        var state = CreateVictoryState();
        state.Result = CombatResult.Defeat;

        _skillService.ApplySkillGains(player, state);

        player.Skills.Unarmed.Should().Be(0.0);
    }

    [Fact]
    public void ApplySkillGains_SkillsAccumulateAcrossMultipleCombats()
    {
        var player = CreatePlayer();
        var state1 = CreateVictoryState();
        var state2 = CreateVictoryState();

        _skillService.ApplySkillGains(player, state1);
        var afterFirst = player.Skills.Unarmed;
        _skillService.ApplySkillGains(player, state2);

        player.Skills.Unarmed.Should().BeGreaterThan(afterFirst);
    }
}

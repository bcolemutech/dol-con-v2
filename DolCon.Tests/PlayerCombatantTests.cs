using DolCon.Enums;
using DolCon.Models;
using DolCon.Models.Combat;
using FluentAssertions;

namespace DolCon.Tests;

public class PlayerCombatantTests
{
    [Theory]
    [InlineData(1.0, 100)]
    [InlineData(0.5, 50)]
    [InlineData(0.25, 25)]
    [InlineData(0.0, 0)]
    public void Constructor_CalculatesHPFromStamina(double stamina, int expectedHP)
    {
        // Arrange
        var player = new Player { Name = "TestPlayer" };

        // Act
        var combatant = new PlayerCombatant(player, stamina);

        // Assert
        combatant.MaxHitPoints.Should().Be(expectedHP);
        combatant.CurrentHitPoints.Should().Be(expectedHP);
    }

    [Fact]
    public void Constructor_CopiesPlayerNameAndId()
    {
        // Arrange
        var player = new Player { Name = "TestPlayer" };

        // Act
        var combatant = new PlayerCombatant(player, 1.0);

        // Assert
        combatant.Name.Should().Be("TestPlayer");
        combatant.Id.Should().Be(player.Id);
        combatant.SourcePlayer.Should().Be(player);
    }

    [Fact]
    public void Constructor_SetsBaseACWithNoArmor()
    {
        // Arrange
        var player = new Player { Name = "TestPlayer" };

        // Act
        var combatant = new PlayerCombatant(player, 1.0);

        // Assert
        combatant.ArmorClass.Should().Be(10); // Base AC
    }

    [Theory]
    [InlineData(Equipment.Body, Rarity.Common, 13)]    // Base 10 + 3 (body armor)
    [InlineData(Equipment.Body, Rarity.Uncommon, 14)]  // Base 10 + 3 + 1 (rarity)
    [InlineData(Equipment.Body, Rarity.Rare, 15)]      // Base 10 + 3 + 2 (rarity)
    [InlineData(Equipment.Shield, Rarity.Common, 12)] // Base 10 + 2 (shield)
    public void Constructor_CalculatesACFromEquippedArmor(Equipment slot, Rarity rarity, int expectedAC)
    {
        // Arrange
        var player = new Player
        {
            Name = "TestPlayer",
            Inventory = new List<Item>
            {
                new Item
                {
                    Name = "Test Armor",
                    Equipment = slot,
                    Equipped = true,
                    Rarity = rarity,
                    Tags = new List<Tag> { new Tag("Armor", TagType.Good) }
                }
            }
        };

        // Act
        var combatant = new PlayerCombatant(player, 1.0);

        // Assert
        combatant.ArmorClass.Should().Be(expectedAC);
    }

    [Fact]
    public void Constructor_CombinesMultipleArmorPieces()
    {
        // Arrange
        var player = new Player
        {
            Name = "TestPlayer",
            Inventory = new List<Item>
            {
                new Item
                {
                    Name = "Body Armor",
                    Equipment = Equipment.Body,
                    Equipped = true,
                    Rarity = Rarity.Common,
                    Tags = new List<Tag> { new Tag("Armor", TagType.Good) }
                },
                new Item
                {
                    Name = "Shield",
                    Equipment = Equipment.Shield,
                    Equipped = true,
                    Rarity = Rarity.Common,
                    Tags = new List<Tag> { new Tag("Armor", TagType.Good) }
                }
            }
        };

        // Act
        var combatant = new PlayerCombatant(player, 1.0);

        // Assert
        combatant.ArmorClass.Should().Be(15); // Base 10 + 3 (body) + 2 (shield)
    }

    [Fact]
    public void GetWeaponDamage_ReturnsUnarmedDamageWhenNoWeapon()
    {
        // Arrange
        var player = new Player { Name = "TestPlayer" };
        var combatant = new PlayerCombatant(player, 1.0);

        // Act
        var damage = combatant.GetWeaponDamage();

        // Assert
        damage.Should().Be(2); // Unarmed damage
    }

    [Theory]
    [InlineData(Equipment.OneHanded, Rarity.Common, 4)]
    [InlineData(Equipment.OneHanded, Rarity.Uncommon, 6)]
    [InlineData(Equipment.OneHanded, Rarity.Rare, 8)]
    [InlineData(Equipment.TwoHanded, Rarity.Common, 6)]
    [InlineData(Equipment.TwoHanded, Rarity.Rare, 10)]
    public void GetWeaponDamage_CalculatesFromEquippedWeapon(Equipment slot, Rarity rarity, int expectedDamage)
    {
        // Arrange
        var player = new Player
        {
            Name = "TestPlayer",
            Inventory = new List<Item>
            {
                new Item
                {
                    Name = "Test Weapon",
                    Equipment = slot,
                    Equipped = true,
                    Rarity = rarity,
                    Tags = new List<Tag> { new Tag("Weapon", TagType.Good) }
                }
            }
        };
        var combatant = new PlayerCombatant(player, 1.0);

        // Act
        var damage = combatant.GetWeaponDamage();

        // Assert
        damage.Should().Be(expectedDamage);
    }

    [Theory]
    [InlineData(1.0, 50, 0.5)]
    [InlineData(0.5, 25, 0.25)]
    [InlineData(1.0, 100, 0.0)]
    public void GetRemainingStaminaPercent_ReflectsDamage(double startStamina, int damageTaken, double expectedStamina)
    {
        // Arrange
        var player = new Player { Name = "TestPlayer" };
        var combatant = new PlayerCombatant(player, startStamina);
        combatant.TakeDamage(damageTaken);

        // Act
        var remainingStamina = combatant.GetRemainingStaminaPercent();

        // Assert
        remainingStamina.Should().BeApproximately(expectedStamina, 0.001);
    }

    [Fact]
    public void IgnoresUnequippedItems()
    {
        // Arrange
        var player = new Player
        {
            Name = "TestPlayer",
            Inventory = new List<Item>
            {
                new Item
                {
                    Name = "Legendary Armor",
                    Equipment = Equipment.Body,
                    Equipped = false, // Not equipped!
                    Rarity = Rarity.Legendary,
                    Tags = new List<Tag> { new Tag("Armor", TagType.Good) }
                }
            }
        };

        // Act
        var combatant = new PlayerCombatant(player, 1.0);

        // Assert
        combatant.ArmorClass.Should().Be(10); // Just base AC
    }
}

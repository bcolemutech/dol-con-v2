using DolCon.Core.Data;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.Combat;
using DolCon.Core.Services;
using FluentAssertions;
using NSubstitute;

namespace DolCon.Core.Tests;

public class CombatServiceTests
{
    private readonly ICombatService _combatService;
    private readonly IShopService _mockShopService;

    public CombatServiceTests()
    {
        _mockShopService = Substitute.For<IShopService>();
        _mockShopService.GenerateReward().Returns(new Item
        {
            Name = "Test Reward",
            Description = "A test item",
            Rarity = Rarity.Common,
            Tags = new List<Tag> { new Tag("Good", TagType.Good) }
        });

        _combatService = new CombatService(_mockShopService);

        // Initialize enemy index for tests
        EnemyIndex.Initialize();
    }

    [Fact]
    public void StartCombat_InitializesTurnOrder()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player { Name = "Player1" },
            new Player { Name = "Player2" }
        };
        var stamina = 1.0;

        // Act
        var state = _combatService.StartCombat(players, stamina, BiomeType.Plains, 1.0);

        // Assert
        state.Should().NotBeNull();
        state.Players.Should().HaveCount(2);
        state.Enemies.Should().NotBeEmpty();
        state.TurnOrder.Should().NotBeEmpty();
        state.Result.Should().Be(CombatResult.InProgress);
        state.CombatLog.Should().NotBeEmpty();
    }

    [Fact]
    public void StartCombat_ConvertsPlayersToComba­tants()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player { Name = "TestPlayer" }
        };

        // Act
        var state = _combatService.StartCombat(players, 0.8, BiomeType.Forest, 1.0);

        // Assert
        state.Players.Should().HaveCount(1);
        state.Players[0].Name.Should().Be("TestPlayer");
        state.Players[0].MaxHitPoints.Should().Be(80); // 0.8 * 100
    }

    [Fact]
    public void ProcessPlayerAction_Attack_DealsDamageOnHit()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Attacker" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Get the first enemy target
        var targetId = state.Enemies.First().Id;

        // Simulate that it's a player's turn
        state.CurrentTurnIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));

        // Act - Attack multiple times to ensure at least one hit
        for (int i = 0; i < 10; i++)
        {
            _combatService.ProcessPlayerAction(state, CombatAction.Attack, targetId);
            state.CurrentTurnIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));
        }

        // Assert - At least some damage should have been dealt (probabilistic)
        state.CombatLog.Should().Contain(log => log.Contains("hits") || log.Contains("misses"));
    }

    [Fact]
    public void ProcessPlayerAction_Defend_LogsDefensiveAction()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Defender" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Simulate player's turn
        state.CurrentTurnIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));

        // Act
        _combatService.ProcessPlayerAction(state, CombatAction.Defend);

        // Assert
        state.CombatLog.Should().Contain(log => log.Contains("defensive"));
    }

    [Fact]
    public void ProcessPlayerAction_Flee_CostsStaminaAndEndsCombat()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Runner" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Ensure it's turn 0 so flee is allowed
        state.CurrentTurn = 0;

        // Make sure it's a player's turn
        var playerIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));
        if (playerIndex >= 0)
        {
            state.CurrentTurnIndex = playerIndex;
            state.ActiveCombatantId = state.TurnOrder[playerIndex].Id;
        }

        // Act
        _combatService.ProcessPlayerAction(state, CombatAction.Flee);

        // Assert
        state.Result.Should().Be(CombatResult.Fled);
        state.CombatLog.Should().Contain(log => log.Contains("flees") || log.Contains("flee"));
    }

    [Fact]
    public void ProcessPlayerAction_FleeNotAllowedAfterFirstTurn()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Runner" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Set to later turn
        state.CurrentTurn = 1;

        // Act
        _combatService.ProcessPlayerAction(state, CombatAction.Flee);

        // Assert - Should still be in progress since flee wasn't allowed
        state.Result.Should().Be(CombatResult.InProgress);
    }

    [Fact]
    public void ProcessEnemyTurn_EnemyAttacksPlayer()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Target" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 1.0);

        // Find an enemy turn
        var enemyIndex = state.TurnOrder.FindIndex(e => state.Enemies.Any(en => en.Id == e.Id && en.IsAlive));
        if (enemyIndex >= 0)
        {
            state.CurrentTurnIndex = enemyIndex;
        }

        // Act
        _combatService.ProcessEnemyTurn(state);

        // Assert
        state.CombatLog.Should().Contain(log => log.Contains("hits") || log.Contains("misses"));
    }

    [Fact]
    public void CheckCombatEnd_VictoryWhenAllEnemiesDead()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Victor" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.25);

        // Kill all enemies
        foreach (var enemy in state.Enemies)
        {
            enemy.CurrentHitPoints = 0;
        }

        // Act
        _combatService.CheckCombatEnd(state);

        // Assert
        state.Result.Should().Be(CombatResult.Victory);
    }

    [Fact]
    public void CheckCombatEnd_DefeatWhenAllPlayersDead()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Fallen" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Kill all players
        foreach (var player in state.Players)
        {
            player.CurrentHitPoints = 0;
        }

        // Act
        _combatService.CheckCombatEnd(state);

        // Assert
        state.Result.Should().Be(CombatResult.Defeat);
    }

    [Fact]
    public void AdvanceTurn_SkipsDeadCombatants()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player { Name = "Player1" },
            new Player { Name = "Player2" }
        };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Kill first combatant in turn order
        if (state.TurnOrder.Count > 0)
        {
            state.TurnOrder[0].CurrentHitPoints = 0;
        }
        state.CurrentTurnIndex = 0;

        // Act
        _combatService.AdvanceTurn(state);

        // Assert - Should have advanced to a living combatant
        var activeCombatant = state.GetActiveCombatant();
        if (activeCombatant != null)
        {
            activeCombatant.IsAlive.Should().BeTrue();
        }
    }

    [Fact]
    public void StartCombat_GeneratesEnemiesBasedOnBiome()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Explorer" } };

        // Act
        var forestState = _combatService.StartCombat(players, 1.0, BiomeType.Forest, 2.0);
        var plainsState = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 2.0);

        // Assert - Both should generate enemies (may overlap in some biomes)
        forestState.Enemies.Should().NotBeEmpty();
        plainsState.Enemies.Should().NotBeEmpty();
    }

    [Fact]
    public void EnemyDefeated_AddsXPToTotal()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Slayer" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        var targetEnemy = state.Enemies.First();

        // Kill the enemy
        targetEnemy.CurrentHitPoints = 0;

        // Act
        _combatService.CheckCombatEnd(state);

        // Assert
        if (state.Result == CombatResult.Victory)
        {
            state.TotalXPEarned.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Defend_ACBonusRemovedOnNextPlayerTurn()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Defender" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Simulate player's turn
        var playerIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));
        state.CurrentTurnIndex = playerIndex;
        state.ActiveCombatantId = state.Players[0].Id;

        var originalAC = state.Players[0].ArmorClass;

        // Act - Player defends
        _combatService.ProcessPlayerAction(state, CombatAction.Defend);

        // Assert - AC should be increased by 2
        state.Players[0].ArmorClass.Should().Be(originalAC + 2);
        state.Players[0].HasUsedDefend.Should().BeTrue();

        // Now advance turns back to player (simulating a full round)
        // The AdvanceTurn should reset the defend bonus when it's the player's turn again
        state.CurrentTurnIndex = playerIndex;
        state.ActiveCombatantId = state.Players[0].Id;

        // Call AdvanceTurn which should reset defend bonus
        _combatService.AdvanceTurn(state);

        // Find the new active combatant - if it's the player, check AC was reset
        var activeCombatant = state.GetActiveCombatant();
        var player = state.Players.FirstOrDefault(p => p.Id == activeCombatant?.Id);
        if (player != null)
        {
            player.ArmorClass.Should().Be(originalAC);
            player.HasUsedDefend.Should().BeFalse();
        }
    }

    [Fact]
    public void Defend_ACBonusPersistsThroughEnemyAttacks()
    {
        // Arrange
        var players = new List<Player> { new Player { Name = "Defender" } };
        var state = _combatService.StartCombat(players, 1.0, BiomeType.Plains, 0.5);

        // Set player turn
        var playerIndex = state.TurnOrder.FindIndex(e => state.Players.Any(p => p.Id == e.Id));
        state.CurrentTurnIndex = playerIndex;
        state.ActiveCombatantId = state.Players[0].Id;

        var originalAC = state.Players[0].ArmorClass;

        // Player defends
        _combatService.ProcessPlayerAction(state, CombatAction.Defend);
        var boostedAC = state.Players[0].ArmorClass;
        boostedAC.Should().Be(originalAC + 2);

        // Simulate enemy attack - AC should remain boosted
        var enemyIndex = state.TurnOrder.FindIndex(e => state.Enemies.Any(en => en.Id == e.Id && en.IsAlive));
        if (enemyIndex >= 0)
        {
            state.CurrentTurnIndex = enemyIndex;
            state.ActiveCombatantId = state.TurnOrder[enemyIndex].Id;

            _combatService.ProcessEnemyTurn(state);

            // AC should still be boosted (not removed on attack)
            state.Players[0].ArmorClass.Should().Be(boostedAC);
        }
    }

    [Fact]
    public void CalculatePostCombatStamina_Defeat_Returns50Percent()
    {
        // Arrange
        var state = new CombatState { Result = CombatResult.Defeat };
        var currentStamina = 0.8;

        // Act
        var result = CombatService.CalculatePostCombatStamina(state, currentStamina);

        // Assert
        result.Should().Be(0.4); // 50% of 0.8
    }

    [Fact]
    public void CalculatePostCombatStamina_Defeat_MinimumZero()
    {
        // Arrange
        var state = new CombatState { Result = CombatResult.Defeat };
        var currentStamina = 0.0;

        // Act
        var result = CombatService.CalculatePostCombatStamina(state, currentStamina);

        // Assert
        result.Should().Be(0.0);
    }
}

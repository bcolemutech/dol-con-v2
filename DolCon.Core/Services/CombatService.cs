using DolCon.Core.Data;
using DolCon.Core.Enums;
using DolCon.Core.Models;
using DolCon.Core.Models.Combat;

namespace DolCon.Core.Services;

public interface ICombatService
{
    /// <summary>
    /// Start a new combat encounter
    /// </summary>
    CombatState StartCombat(List<Player> players, double stamina, BiomeType biome, double challengeRating);

    /// <summary>
    /// Process a player action during combat
    /// </summary>
    void ProcessPlayerAction(CombatState state, CombatAction action, Guid? targetId = null);

    /// <summary>
    /// Process an enemy's turn
    /// </summary>
    void ProcessEnemyTurn(CombatState state);

    /// <summary>
    /// Check if combat has ended (victory, defeat, or fled)
    /// </summary>
    void CheckCombatEnd(CombatState state);

    /// <summary>
    /// Advance to the next turn, skipping dead combatants
    /// </summary>
    void AdvanceTurn(CombatState state);
}

public class CombatService : ICombatService
{
    private readonly IShopService _shopService;
    private readonly Random _rng = new();

    public CombatService(IShopService shopService)
    {
        _shopService = shopService;
    }

    public CombatState StartCombat(List<Player> players, double stamina, BiomeType biome, double challengeRating)
    {
        var state = new CombatState();

        // Convert players to combatants
        state.Players = players
            .Select(p => new PlayerCombatant(p, stamina))
            .ToList();

        // Generate enemies based on biome and challenge rating
        var builder = new EncounterBuilder(_rng);
        var difficulty = GetDifficultyFromCR(challengeRating);
        state.Enemies = builder.BuildEncounter(biome, 1, players.Count, difficulty);

        // If no enemies found for this biome, try with Plains as fallback
        if (state.Enemies.Count == 0)
        {
            state.Enemies = builder.BuildEncounter(BiomeType.Plains, 1, players.Count, difficulty);
        }

        // If still no enemies, create a basic enemy
        if (state.Enemies.Count == 0)
        {
            state.Enemies.Add(CreateFallbackEnemy(challengeRating));
        }

        // Roll initiative and set turn order
        RollInitiative(state);

        state.CombatLog.Add($"Combat begins! {state.Enemies.Count} enemies appear.");

        return state;
    }

    private EncounterDifficulty GetDifficultyFromCR(double challengeRating)
    {
        return challengeRating switch
        {
            <= 0.5 => EncounterDifficulty.Easy,
            <= 1.5 => EncounterDifficulty.Medium,
            <= 3.0 => EncounterDifficulty.Hard,
            _ => EncounterDifficulty.Deadly
        };
    }

    private Enemy CreateFallbackEnemy(double challengeRating)
    {
        return new Enemy
        {
            Name = "Wild Beast",
            Description = "A hostile creature",
            Category = EnemyCategory.Nature,
            Subcategory = EnemySubcategory.Beast,
            ChallengeRating = Math.Max(0.25, challengeRating),
            MaxHitPoints = (int)(20 * (1 + challengeRating)),
            CurrentHitPoints = (int)(20 * (1 + challengeRating)),
            ArmorClass = 10 + (int)challengeRating,
            Strength = 12,
            Dexterity = 12,
            ExperienceValue = (int)(50 * challengeRating),
            BehaviorType = EnemyBehaviorType.Aggressive
        };
    }

    private void RollInitiative(CombatState state)
    {
        var allCombatants = new List<(CombatEntity entity, int initiative)>();

        // Roll for players
        foreach (var player in state.Players)
        {
            var roll = _rng.Next(1, 21) + player.GetModifier(player.Dexterity);
            player.Initiative = roll;
            allCombatants.Add((player, roll));
        }

        // Roll for enemies
        foreach (var enemy in state.Enemies)
        {
            var roll = _rng.Next(1, 21) + enemy.GetModifier(enemy.Dexterity);
            enemy.Initiative = roll;
            allCombatants.Add((enemy, roll));
        }

        // Sort by initiative (highest first), with random tiebreaker
        state.TurnOrder = allCombatants
            .OrderByDescending(x => x.initiative)
            .ThenBy(_ => _rng.Next())
            .Select(x => x.entity)
            .ToList();

        state.CurrentTurnIndex = 0;
        if (state.TurnOrder.Count > 0)
        {
            state.ActiveCombatantId = state.TurnOrder[0].Id;
        }
    }

    public void ProcessPlayerAction(CombatState state, CombatAction action, Guid? targetId = null)
    {
        if (state.Result != CombatResult.InProgress)
            return;

        var player = GetActivePlayer(state);
        if (player == null)
            return;

        switch (action)
        {
            case CombatAction.Attack:
                ProcessAttack(state, player, targetId);
                break;
            case CombatAction.Defend:
                ProcessDefend(state, player);
                break;
            case CombatAction.Flee:
                ProcessFlee(state);
                return; // Don't advance turn on flee
        }

        CheckCombatEnd(state);

        if (state.Result == CombatResult.InProgress)
        {
            AdvanceTurn(state);
        }
    }

    private PlayerCombatant? GetActivePlayer(CombatState state)
    {
        var active = state.GetActiveCombatant();
        return state.Players.FirstOrDefault(p => p.Id == active?.Id);
    }

    private void ProcessAttack(CombatState state, PlayerCombatant player, Guid? targetId)
    {
        var target = targetId.HasValue
            ? state.Enemies.FirstOrDefault(e => e.Id == targetId.Value && e.IsAlive)
            : state.GetAliveEnemies().FirstOrDefault();

        if (target == null)
        {
            state.CombatLog.Add($"{player.Name} has no valid target!");
            return;
        }

        // Attack roll (d20 + strength modifier vs AC)
        var roll = _rng.Next(1, 21);
        var attackBonus = player.GetModifier(player.Strength);
        var totalAttack = roll + attackBonus;

        // Create attack result for UI display
        var attackResult = new AttackResult
        {
            AttackerName = player.Name,
            TargetName = target.Name,
            D20Roll = roll,
            AttackModifier = attackBonus,
            TotalAttack = totalAttack,
            TargetAC = target.ArmorClass,
            IsNatural1 = roll == 1,
            IsCritical = roll == 20,
            DamageSource = GetPlayerWeaponName(player)
        };

        if (roll == 20 || (roll != 1 && totalAttack >= target.ArmorClass))
        {
            attackResult.IsHit = true;
            var baseDamage = player.GetWeaponDamage();
            var rarityBonus = GetPlayerWeaponRarityBonus(player);

            attackResult.BaseDamage = baseDamage - rarityBonus; // Base without rarity
            attackResult.BonusDamage = rarityBonus;

            var damage = baseDamage;
            if (roll == 20) damage *= 2; // Critical hit
            attackResult.TotalDamage = damage;

            target.TakeDamage(damage);
            state.CombatLog.Add($"{player.Name} hits {target.Name} for {damage} damage!{(roll == 20 ? " (Critical!)" : "")}");

            if (!target.IsAlive)
            {
                state.CombatLog.Add($"{target.Name} is defeated!");
                state.TotalXPEarned += target.ExperienceValue;
            }
        }
        else
        {
            attackResult.IsHit = false;
            state.CombatLog.Add($"{player.Name} misses {target.Name}!");
        }

        state.LastAttackResult = attackResult;
    }

    private string GetPlayerWeaponName(PlayerCombatant player)
    {
        var weapon = player.SourcePlayer.Inventory
            .FirstOrDefault(i => i.Equipped && i.Tags.Any(t => t.Name.Contains("Weapon")));

        return weapon?.Name ?? "Unarmed";
    }

    private int GetPlayerWeaponRarityBonus(PlayerCombatant player)
    {
        var weapon = player.SourcePlayer.Inventory
            .FirstOrDefault(i => i.Equipped && i.Tags.Any(t => t.Name.Contains("Weapon")));

        return weapon != null ? (int)weapon.Rarity * 2 : 0;
    }

    private void ProcessDefend(CombatState state, PlayerCombatant player)
    {
        // Defending gives +2 AC until their next turn
        player.ArmorClass += 2;
        player.HasUsedDefend = true;
        state.CombatLog.Add($"{player.Name} takes a defensive stance. (+2 AC)");
    }

    private void ResetDefendBonusIfNeeded(PlayerCombatant player)
    {
        if (player.HasUsedDefend)
        {
            player.ArmorClass -= 2;
            player.HasUsedDefend = false;
        }
    }

    private void ProcessFlee(CombatState state)
    {
        if (!state.CanFlee)
        {
            state.CombatLog.Add("Cannot flee after the first turn!");
            return;
        }

        state.Result = CombatResult.Fled;
        state.CombatLog.Add("The party flees from battle!");
    }

    public void ProcessEnemyTurn(CombatState state)
    {
        if (state.Result != CombatResult.InProgress)
            return;

        var enemy = GetActiveEnemy(state);
        if (enemy == null || !enemy.IsAlive)
        {
            AdvanceTurn(state);
            return;
        }

        // Simple AI: attack random alive player
        var target = state.GetAlivePlayers()
            .OrderBy(_ => _rng.Next())
            .FirstOrDefault();

        if (target == null)
        {
            AdvanceTurn(state);
            return;
        }

        // Enemy attack roll
        var roll = _rng.Next(1, 21);
        var attackBonus = enemy.GetModifier(enemy.Strength);
        var totalAttack = roll + attackBonus;

        // Create attack result for UI display
        var attackResult = new AttackResult
        {
            AttackerName = enemy.Name,
            TargetName = target.Name,
            D20Roll = roll,
            AttackModifier = attackBonus,
            TotalAttack = totalAttack,
            TargetAC = target.ArmorClass,
            IsNatural1 = roll == 1,
            IsCritical = roll == 20,
            DamageSource = GetEnemyAttackName(enemy)
        };

        if (roll == 20 || (roll != 1 && totalAttack >= target.ArmorClass))
        {
            attackResult.IsHit = true;

            // Base damage from enemy CR
            var baseDamage = Math.Max(2, (int)(3 + enemy.ChallengeRating * 2));
            attackResult.BaseDamage = baseDamage;
            attackResult.BonusDamage = 0; // Enemies don't have bonus damage

            var damage = roll == 20 ? baseDamage * 2 : baseDamage;
            attackResult.TotalDamage = damage;

            target.TakeDamage(damage);
            state.TotalDamageTaken += damage;
            state.CombatLog.Add($"{enemy.Name} hits {target.Name} for {damage} damage!{(roll == 20 ? " (Critical!)" : "")}");
        }
        else
        {
            attackResult.IsHit = false;
            state.CombatLog.Add($"{enemy.Name} misses {target.Name}!");
        }

        state.LastAttackResult = attackResult;

        // Set enemy turn display flags for UI pause
        state.IsDisplayingEnemyTurn = true;
        state.EnemyTurnDisplayStart = DateTime.Now;

        CheckCombatEnd(state);

        // Note: Turn advancement is now handled by the UI after the display pause
    }

    private string GetEnemyAttackName(Enemy enemy)
    {
        // Determine attack name based on category and subcategory
        return enemy.Category switch
        {
            EnemyCategory.Nature => enemy.Subcategory switch
            {
                EnemySubcategory.Beast => "Claws",
                EnemySubcategory.Plant => "Thorns",
                EnemySubcategory.Elemental => "Elemental Blast",
                _ => "Natural Attack"
            },
            EnemyCategory.Undead => "Deathly Touch",
            EnemyCategory.Human => enemy.Subcategory switch
            {
                EnemySubcategory.Bandit => "Weapon Strike",
                EnemySubcategory.Cultist => "Dark Magic",
                EnemySubcategory.Soldier => "Sword Slash",
                EnemySubcategory.Mercenary => "Mercenary Strike",
                _ => "Attack"
            },
            EnemyCategory.Demon => enemy.Subcategory switch
            {
                EnemySubcategory.DemonHuman => "Corrupted Strike",
                EnemySubcategory.DemonCreature => "Demonic Claw",
                _ => "Demonic Strike"
            },
            _ => "Attack"
        };
    }

    private Enemy? GetActiveEnemy(CombatState state)
    {
        var active = state.GetActiveCombatant();
        return state.Enemies.FirstOrDefault(e => e.Id == active?.Id);
    }

    public void CheckCombatEnd(CombatState state)
    {
        if (state.Result != CombatResult.InProgress)
            return;

        // Check for victory
        if (state.Enemies.All(e => !e.IsAlive))
        {
            state.Result = CombatResult.Victory;

            // Calculate total XP from all enemies
            state.TotalXPEarned = state.Enemies.Sum(e => e.ExperienceValue);

            // Generate loot from defeated enemies
            var lootDrops = state.Enemies
                .SelectMany(e => e.PossibleLoot)
                .Where(loot => _rng.NextDouble() <= loot.DropChance);
            foreach (var lootDrop in lootDrops)
            {
                state.PendingLoot.Add(lootDrop);
            }

            state.CombatLog.Add("Victory! The party is victorious!");
            return;
        }

        // Check for defeat
        if (state.Players.All(p => !p.IsAlive))
        {
            state.Result = CombatResult.Defeat;
            state.CombatLog.Add("Defeat! The party has fallen.");
        }
    }

    public void AdvanceTurn(CombatState state)
    {
        if (state.TurnOrder.Count == 0)
            return;

        var loopCount = 0;

        do
        {
            state.CurrentTurnIndex = (state.CurrentTurnIndex + 1) % state.TurnOrder.Count;

            // Check if we've gone around once
            if (state.CurrentTurnIndex == 0)
            {
                state.CurrentTurn++;
            }

            loopCount++;
            if (loopCount > state.TurnOrder.Count)
            {
                // All combatants are dead, combat should have ended
                break;
            }
        }
        while (!state.TurnOrder[state.CurrentTurnIndex].IsAlive);

        state.ActiveCombatantId = state.TurnOrder[state.CurrentTurnIndex].Id;

        // Reset defend bonus when player's turn starts
        var activePlayer = state.Players.FirstOrDefault(p => p.Id == state.ActiveCombatantId);
        if (activePlayer != null)
        {
            ResetDefendBonusIfNeeded(activePlayer);
        }
    }

    /// <summary>
    /// Calculate stamina changes after combat ends
    /// </summary>
    public static double CalculatePostCombatStamina(CombatState state, double currentStamina)
    {
        switch (state.Result)
        {
            case CombatResult.Victory:
                // Restore half of lost stamina
                var staminaLost = state.TotalDamageTaken / 100.0;
                return Math.Min(1.0, currentStamina + staminaLost / 2);

            case CombatResult.Fled:
                // Small stamina cost for fleeing (5%)
                return Math.Max(0, currentStamina - 0.05);

            case CombatResult.Defeat:
                // Reduce stamina by 50% on defeat
                return Math.Max(0, currentStamina * 0.5);

            default:
                return currentStamina;
        }
    }
}

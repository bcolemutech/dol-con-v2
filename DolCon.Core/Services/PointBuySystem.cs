using DolCon.Core.Models;

namespace DolCon.Core.Services;

/// <summary>
/// D&D 5e point-buy system for ability score allocation.
/// 27 points to distribute across 6 ability scores (range 8-15).
/// </summary>
public static class PointBuySystem
{
    public const int TotalPoints = 27;
    public const int MinScore = 8;
    public const int MaxScore = 15;

    private static readonly int[] CostTable = { 0, 1, 2, 3, 4, 5, 7, 9 };

    public static int GetCost(int score)
    {
        if (score < MinScore || score > MaxScore)
            throw new ArgumentOutOfRangeException(nameof(score),
                $"Score must be between {MinScore} and {MaxScore}");

        return CostTable[score - MinScore];
    }

    public static int GetTotalCost(PlayerAbilities abilities)
    {
        return GetCost(abilities.Strength) + GetCost(abilities.Dexterity) +
               GetCost(abilities.Constitution) + GetCost(abilities.Intelligence) +
               GetCost(abilities.Wisdom) + GetCost(abilities.Charisma);
    }

    public static int GetRemainingPoints(PlayerAbilities abilities)
    {
        return TotalPoints - GetTotalCost(abilities);
    }

    public static bool IsValid(PlayerAbilities abilities)
    {
        if (!IsInRange(abilities.Strength) || !IsInRange(abilities.Dexterity) ||
            !IsInRange(abilities.Constitution) || !IsInRange(abilities.Intelligence) ||
            !IsInRange(abilities.Wisdom) || !IsInRange(abilities.Charisma))
        {
            return false;
        }

        return GetRemainingPoints(abilities) == 0;
    }

    private static bool IsInRange(int score) => score >= MinScore && score <= MaxScore;

    public static bool CanIncrease(int score) => score < MaxScore;

    public static bool CanDecrease(int score) => score > MinScore;

    public static bool CanAffordIncrease(PlayerAbilities abilities, int currentScore)
    {
        if (!CanIncrease(currentScore)) return false;
        var costDiff = GetCost(currentScore + 1) - GetCost(currentScore);
        return GetRemainingPoints(abilities) >= costDiff;
    }
}

// <copyright file="AttackActionValidator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Skills;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Validates whether a player may execute an attack skill.
/// </summary>
internal static class AttackActionValidator
{
    /// <summary>The item group of bows and crossbows.</summary>
    private const byte BowGroup = 4;

    /// <summary>
    /// The skills shot with a bow: Triple Shot (24), Ice Arrow (51), Penetration (52), Multi-Shot (235),
    /// and the master skills built on them (Multi-Shot 411/431, Triple Shot 414/418, Penetration 416, Ice Arrow 424).
    /// </summary>
    private static readonly HashSet<short> BowSkills = [24, 51, 52, 235, 411, 414, 416, 418, 424, 431];

    /// <summary>
    /// Determines whether the player is currently unable to act due to a status effect.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c> if the player is stunned or asleep; otherwise, <c>false</c>.</returns>
    internal static bool IsIncapacitated(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return true;
        }

        if (attributes[Stats.IsStunned] > 0)
        {
            player.Logger.LogWarning("Probably Hacker - player {player} is attacking in stunned state", player);
            return true;
        }

        if (attributes[Stats.IsAsleep] > 0)
        {
            player.Logger.LogWarning("Probably Hacker - player {player} is attacking in sleep state", player);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the skill is an attack skill instead of a supportive skill.
    /// </summary>
    /// <param name="skill">The skill.</param>
    /// <returns><c>true</c> if the skill attacks a target; otherwise, <c>false</c>.</returns>
    internal static bool IsAttackSkill(Skill skill) =>
        skill.SkillType is not (SkillType.Buff or SkillType.Regeneration or SkillType.SummonMonster);

    /// <summary>
    /// Determines whether the player may attack with the specified skill.
    /// </summary>
    /// <param name="player">The attacking player.</param>
    /// <param name="skill">The skill which is used for the attack, or <c>null</c> for a regular hit.</param>
    /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
    internal static async ValueTask<bool> CanAttackAsync(Player player, Skill? skill)
    {
        if (player.Attributes is not { } attributes)
        {
            return false;
        }

        if (IsIncapacitated(player))
        {
            return false;
        }

        if (player.IsAtSafezone())
        {
            player.Logger.LogWarning("Probably Hacker - player {player} is attacking from safezone", player);
            return false;
        }

        if (skill is null)
        {
            return await player.IsAttackAllowedAsync(null).ConfigureAwait(false);
        }

        // Skills that move attacker to target (e.g., Twisting Slash, Death Stab) require a weapon
        if (skill.MovesToTarget && attributes[Stats.EquippedWeaponCount] == 0)
        {
            return false;
        }

        if (attributes[Stats.AmmunitionConsumptionRate] > attributes[Stats.AmmunitionAmount])
        {
            return false;
        }

        // The bow skills need a bow or crossbow in hand, as in the original: without one the ammunition
        // rate above is 0 (it comes from the bow) and an elf cast Penetration with a rapier (2026-11-28).
        // Infinity Arrow also brings the rate to 0, so the check is on the weapon, not the rate.
        if (BowSkills.Contains(skill.Number) && !HoldsBow(player))
        {
            return false;
        }

        return !IsAttackSkill(skill) || await player.IsAttackAllowedAsync(skill).ConfigureAwait(false);
    }

    /// <summary>Whether a bow or crossbow is in one of the player's hands.</summary>
    private static bool HoldsBow(Player player) =>
        player.Inventory is { } inventory
        && (inventory.GetItem(InventoryConstants.LeftHandSlot)?.Definition?.Group == BowGroup
            || inventory.GetItem(InventoryConstants.RightHandSlot)?.Definition?.Group == BowGroup);
}

// <copyright file="PvmHitFloors.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Floors of a monster's hits on a player (mu-cow): with many stat points a player's defense,
/// defense rate, health and Soul Barrier outgrow the monsters, which then hardly hit and do almost
/// no damage. A server can set floors which only apply to a monster's hit on a player; the
/// reductions after the defense (wings, pets, harmony, excellent options) still apply to them.
/// </summary>
public static class PvmHitFloors
{
    /// <summary>
    /// Gets or sets the floors of the monster's hit on the player; null, or <c>default</c>, for the
    /// original hit.
    /// </summary>
    public static Func<IAttacker, IAttackable, PvmFloors>? For { get; set; }
}

/// <summary>
/// The floors of a monster's hit on a player, each between 0 and 1; 0 leaves that part as it is.
/// </summary>
/// <param name="DamageFloor">The share of the monster's raw damage which stays after the defense.</param>
/// <param name="HitFloor">The lowest chance of the monster to hit.</param>
/// <param name="HealthFloor">The share of the player's maximum health which a hit does at least, before the reductions after the defense.</param>
/// <param name="SoulBarrierCap">The most the Soul Barrier takes from the hit (OpenMU's own cap is 0.9).</param>
/// <param name="DamageScale">
/// How many times stronger than its definition the monster hits (a server's stronger monsters); 0 or 1
/// for the original. With floors, the pets wear from the original hit: the monster's damage divided by
/// this, without the floors, so a stronger monster threatens the player but not the pet.
/// </param>
/// <param name="AttackRateScale">
/// How many times its definition's attack rate the monster has; 0 or 1 for the original. The pets only
/// wear from the hits which the original monster would land too.
/// </param>
public readonly record struct PvmFloors(float DamageFloor, float HitFloor, float HealthFloor, float SoulBarrierCap, float DamageScale = 0, float AttackRateScale = 0);

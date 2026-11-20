// <copyright file="PvpModifiers.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Modifiers of the fights between players (mu-cow): a server can shape, per hit, what a player's
/// attack does to another player without touching what it does to monsters. Everything here applies
/// on top of the original formulas, at their end: the gear, the buffs, the pets and the skills still
/// count as they do. Without a hook set, nothing changes.
/// </summary>
public static class PvpModifiers
{
    /// <summary>
    /// Gets or sets the modifiers of a hit of a player, their pet or their summoned monster on a
    /// player: called once per hit, before the chance to hit is rolled. A <c>default</c> result (all
    /// zeros) leaves the hit as it is.
    /// </summary>
    public static Func<Player, Player, SkillEntry?, PvpHitSource, PvpHit>? For { get; set; }

    /// <summary>
    /// Gets or sets the factor of what a player's regeneration skill (Heal) restores on another
    /// player; 0 or 1 for the original. The heal on oneself stays.
    /// </summary>
    public static Func<Player, Player, SkillEntry, float>? HealScale { get; set; }

    /// <summary>
    /// Gets or sets the factor of the power-ups of a player's buff skill (e.g. Greater Defense), for
    /// any target; 0 or 1 for the original. Applied when the skill's power-ups are created, so an
    /// online player gets a change after re-entering the game.
    /// </summary>
    public static Func<Player, Skill, float>? BuffScale { get; set; }

    /// <summary>
    /// Gets or sets the factors of the duration and the chance of a player's skill effect on
    /// players (freeze, stun, ...); <c>default</c>, 0 or 1 for the original. Applied when the
    /// skill's power-ups are created, as <see cref="BuffScale"/>.
    /// </summary>
    public static Func<Player, Skill, PvpEffect>? EffectOnPlayers { get; set; }
}

/// <summary>
/// Who hits the player.
/// </summary>
public enum PvpHitSource
{
    /// <summary>The player themself.</summary>
    Player,

    /// <summary>The player's pet (Dark Raven).</summary>
    Pet,

    /// <summary>A monster the player summoned.</summary>
    Summon,
}

/// <summary>
/// The modifiers of a hit between players; each 0 for the original.
/// </summary>
/// <param name="DamageMultiplier">Multiplies the final damage, after the combo and the double damage.</param>
/// <param name="HitChanceMultiplier">Multiplies the chance to hit (the result stays between 3 % and 100 %).</param>
/// <param name="ShieldShare">The share of the damage the defender's shield (SD) takes; OpenMU's is 0.9.</param>
public readonly record struct PvpHit(float DamageMultiplier, float HitChanceMultiplier, float ShieldShare);

/// <summary>
/// The modifiers of a skill's effect on players; each 0 for the original.
/// </summary>
/// <param name="DurationScale">Multiplies the duration of the effect on a player.</param>
/// <param name="ChanceScale">Multiplies the chance of the effect on a player.</param>
public readonly record struct PvpEffect(float DurationScale, float ChanceScale);

/// <summary>
/// An element whose value is another's times a constant; it keeps the other's aggregate type and
/// follows its changes.
/// </summary>
public sealed class ScaledElement : IElement
{
    private readonly IElement _inner;
    private readonly float _scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScaledElement"/> class.
    /// </summary>
    /// <param name="inner">The element to scale.</param>
    /// <param name="scale">The factor.</param>
    public ScaledElement(IElement inner, float scale)
    {
        this._inner = inner;
        this._scale = scale;
        inner.ValueChanged += (_, _) => this.ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public event EventHandler? ValueChanged;

    /// <inheritdoc />
    public float Value => this._inner.Value * this._scale;

    /// <inheritdoc />
    public AggregateType AggregateType => this._inner.AggregateType;

    /// <inheritdoc />
    public override string ToString() => $"{this._inner} x {this._scale}";
}

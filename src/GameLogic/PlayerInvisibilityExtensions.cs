// <copyright file="PlayerInvisibilityExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Extensions to add and remove the invisibility effect of a <see cref="Player"/>.
/// </summary>
public static class PlayerInvisibilityExtensions
{
    /// <summary>
    /// Adds the invisible effect.
    /// </summary>
    /// <param name="player">The player.</param>
    public static async ValueTask AddInvisibleEffectAsync(this Player player)
    {
        var invisibleEffect = player.GameContext.Configuration.MagicEffects.FirstOrDefault(e => e.PowerUpDefinitions.Any(e => e.TargetAttribute == Stats.IsInvisible));
        if (invisibleEffect is null)
        {
            player.Logger.LogError("Invisible effect not found!");
        }
        else
        {
            var (duration, powerUps) = player.CreateMagicEffectPowerUp(invisibleEffect);
            var magicEffect = new MagicEffect(TimeSpan.FromSeconds(duration.Value), invisibleEffect, powerUps.Select(p => new MagicEffect.ElementWithTarget(p.BuffPowerUp, p.Target)).ToArray());
            await player.MagicEffectList.AddEffectAsync(magicEffect).ConfigureAwait(false);

            await RespawnIfOnMapAsync(player).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes the invisible effect.
    /// </summary>
    /// <param name="player">The player.</param>
    public static async ValueTask RemoveInvisibleEffectAsync(this Player player)
    {
        var invisibleEffect = player.GameContext.Configuration.MagicEffects.FirstOrDefault(e => e.PowerUpDefinitions.Any(e => e.TargetAttribute == Stats.IsInvisible));
        if (invisibleEffect is null)
        {
            return;
        }

        if (!player.MagicEffectList.TryGetEffect(invisibleEffect, out var activeEffect))
        {
            return;
        }

        await activeEffect.DisposeAsync().ConfigureAwait(false);
        await RespawnIfOnMapAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the player again to the others on its map, so they see it (dis)appear; nothing while it is
    /// between maps. MuWeb: a duel's spectator leaving warps out, and the duel map's ObjectRemoved (its
    /// EndDuelWhenLeavingDuelMapPlugIn) took the invisibility off while CurrentMap still named the duel
    /// map, so the respawn put the player back on it, now visible: the duelists kept seeing it there.
    /// </summary>
    private static async ValueTask RespawnIfOnMapAsync(Player player)
    {
        if (player.CurrentMap is { } currentMap && ReferenceEquals(currentMap.GetObject(player.Id), player))
        {
            await currentMap.RespawnAsync(player).ConfigureAwait(false);
        }
    }
}

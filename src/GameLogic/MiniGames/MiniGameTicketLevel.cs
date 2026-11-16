// <copyright file="MiniGameTicketLevel.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;

/// <summary>
/// MuWeb: a mini game reward which gives another game's ticket (Devil Square's third place gets an
/// Invisibility Cloak) has no level in its group, since one square covers several Blood Castles. The
/// ticket then gets the level of the game the player can enter now, so it's never a useless one.
/// Called from <see cref="MiniGameRewardService"/>, patched in by server/openmu/Dockerfile.
/// </summary>
internal static class MiniGameTicketLevel
{
    /// <summary>
    /// Sets the level of a ticket reward to the game the player fits in.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="player">The rewarded player.</param>
    /// <param name="group">The reward's item group.</param>
    /// <param name="item">The generated item.</param>
    public static void Apply(IGameContext gameContext, Player player, DropItemGroup group, Item item)
    {
        if (group.ItemLevel is not null || player.SelectedCharacter is null)
        {
            return;
        }

        var game = gameContext.Configuration.MiniGameDefinitions
            .Where(d => d.TicketItem == item.Definition)
            .OrderBy(d => d.GameLevel)
            .FirstOrDefault(d => d.IsInLevelRange(player));
        if (game is not null)
        {
            item.Level = (byte)game.TicketItemLevel;
        }
    }
}

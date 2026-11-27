// <copyright file="IItemPickedUpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a player picked up an item from the ground, into a free
/// slot or onto a stack (MuCow: the game masters' audit log follows what a game master dropped or
/// created with /item to whoever picks it up).
/// </summary>
[PlugInPoint("Item picked up", "Is called when a player picked up an item from the ground.")]
[Guid("3F7A9C25-1B6D-4E80-A4C3-5D2E7F9B1A64")]
public interface IItemPickedUpPlugIn
{
    /// <summary>
    /// Is called when a player picked up an item from the ground.
    /// </summary>
    /// <param name="player">The player who picked it up.</param>
    /// <param name="item">The item as it lay on the ground.</param>
    /// <param name="dropper">The player who dropped it (or created it with /item), if a player did.</param>
    void ItemPickedUp(Player player, Item item, Player? dropper);
}

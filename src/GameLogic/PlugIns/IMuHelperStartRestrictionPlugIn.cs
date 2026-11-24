// <copyright file="IMuHelperStartRestrictionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Plugins which can prevent a player from starting the MU Helper, e.g. during an event.
/// </summary>
[Guid("8E2C4B71-3D9A-4F05-B6E8-1A7C5D2F9B34")]
[PlugInPoint("MU Helper start restriction", "Plugins which can prevent a player from starting the MU Helper.")]
public interface IMuHelperStartRestrictionPlugIn
{
    /// <summary>
    /// Checks if the player may start the MU Helper. It's checked before the helper charges its money.
    /// </summary>
    /// <param name="player">The player who wants to start the helper.</param>
    /// <param name="eventArgs">The event args. A plugin sets <see cref="CancelEventArgs.Cancel"/> to prevent the start, and tells the player why.</param>
    /// <returns>The task.</returns>
    ValueTask CheckStartAsync(Player player, CancelEventArgs eventArgs);
}

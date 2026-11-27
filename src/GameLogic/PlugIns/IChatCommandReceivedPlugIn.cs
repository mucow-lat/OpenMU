// <copyright file="IChatCommandReceivedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a player sends a chat command that exists and whose
/// requirements the player meets, right before it is handled (MuCow: the game masters' audit log;
/// commands never reach <see cref="IChatMessageReceivedPlugIn"/>).
/// </summary>
[PlugInPoint("Chat command received", "Is called when a player sends a chat command he may use, right before it is handled.")]
[Guid("8E2B6F14-5C3A-4D7E-9B1F-0A6C2D4E8F35")]
public interface IChatCommandReceivedPlugIn
{
    /// <summary>
    /// Is called when a player sends a chat command he may use, right before it is handled.
    /// </summary>
    /// <param name="sender">The player who sent it.</param>
    /// <param name="command">The whole line, the command key included.</param>
    /// <param name="requiredStatus">The character status the command requires.</param>
    void ChatCommandReceived(Player sender, string command, CharacterStatus requiredStatus);
}

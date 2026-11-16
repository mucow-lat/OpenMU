// <copyright file="MuWebDiscordBridge.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.AccountLinking;

/// <summary>
/// What MuWeb's linking from the web client (Aurora's book, OAuth with Discord) needs from the Discord
/// bot, which runs in the same process: the bot sets these when it is ready (Discord/DiscordBot.MuWeb.cs)
/// and the season plugin calls them (VersionSeasonTwo/MuWebDiscordLink.cs). Null while the bot is off.
/// </summary>
public static class MuWebDiscordBridge
{
    /// <summary>
    /// Gets or sets the identifier of the bot's Discord application, which is the OAuth client.
    /// </summary>
    public static ulong? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets what the bot does after a link, as for its own /link: gives the role of linked
    /// players to the user (the first argument), takes it from the user who was linked to the account
    /// before (the second, if any), and lets the user into the channels of their guild.
    /// </summary>
    public static Func<ulong, ulong?, Task>? Linked { get; set; }

    /// <summary>
    /// Gets or sets the reading of a Discord user's avatar hash (null when they have none).
    /// </summary>
    public static Func<ulong, Task<string?>>? FetchAvatar { get; set; }
}

// <copyright file="DiscordBot.MuWeb.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using global::Discord.WebSocket;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.AccountLinking;

/// <summary>
/// The bot's part of MuWeb's linking from the web client (GameLogic/MuWebDiscordBridge.cs): copied into
/// OpenMU's Discord project as another part of the bot, so it reaches the role logic of its own /link.
/// </summary>
public sealed partial class DiscordBot
{
    /// <summary>
    /// Called when the bot is ready (patched into OnReadyAsync by the Dockerfile).
    /// </summary>
    /// <param name="client">The connected client.</param>
    private void ConnectMuWeb(DiscordSocketClient client)
    {
        MuWebDiscordBridge.ApplicationId = client.CurrentUser?.Id;
        MuWebDiscordBridge.Linked = async (userId, replacedUserId) =>
        {
            try
            {
                // As ExecuteAccountCommandAsync does for /link.
                await this.PendingRoleRevocations.LinkAsync(userId, () => this.UpdateLinkedRoleAsync(new DiscordRoleChange(userId, true))).ConfigureAwait(false);
                if (replacedUserId is { } replaced && replaced != userId)
                {
                    this.PendingRoleRevocations.Add(replaced, this.GetRoleGuildId(), this.GetLinkedRoleName());
                }

                await this.ProcessPendingRoleRevocationsAsync(false).ConfigureAwait(false);
                await this.CreateMissingHostedChannelsAsync().ConfigureAwait(false);
                await this.SyncHostedChannelsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogWarning(ex, "[MuWeb] The role of the Discord user {userId}, linked from the web client, couldn't be given.", userId);
            }
        };
        MuWebDiscordBridge.FetchAvatar = async userId =>
        {
            if (this._client is not { } current)
            {
                return null;
            }

            var user = await current.Rest.GetUserAsync(userId).ConfigureAwait(false);
            return user?.AvatarId;
        };
    }
}

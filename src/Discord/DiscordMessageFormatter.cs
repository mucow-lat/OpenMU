// <copyright file="DiscordMessageFormatter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using System.Globalization;
using System.Reflection;
using System.Text;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Discord.Properties;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Formats <see cref="GameEvent"/>s as Discord messages.
/// </summary>
public sealed class DiscordMessageFormatter
{
    /// <summary>
    /// The color of event messages, like mini games and invasions (blue).
    /// </summary>
    internal const int EventColor = 0x3498DB;

    /// <summary>
    /// The color of castle siege messages (orange).
    /// </summary>
    internal const int CastleSiegeColor = 0xE67E22;

    /// <summary>
    /// The color of item drop messages (green).
    /// </summary>
    internal const int ItemDropColor = 0x2ECC71;

    /// <summary>
    /// The color of boss kill messages (red).
    /// </summary>
    internal const int BossKillColor = 0xE74C3C;

    /// <summary>
    /// The color of level milestone messages (yellow).
    /// </summary>
    internal const int MilestoneColor = 0xF1C40F;

    /// <summary>
    /// The color of notices of game masters (purple).
    /// </summary>
    internal const int NoticeColor = 0x9B59B6;

    private const string MarkdownCharacters = @"\*_~`|>[]()#";

    /// <summary>
    /// The names of the <see cref="Stats"/> by their identifiers, which name the resources of the item options, e.g. <c>ItemOption_Excellent_ExcellentDamageChance</c>.
    /// </summary>
    private static readonly IReadOnlyDictionary<Guid, string> StatNames = typeof(Stats)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(property => property.PropertyType == typeof(AttributeDefinition))
        .Select(property => (Attribute: (AttributeDefinition?)property.GetValue(null), property.Name))
        .Where(stat => stat.Attribute is not null)
        .GroupBy(stat => stat.Attribute!.Id)
        .ToDictionary(group => group.Key, group => group.First().Name);

    /// <summary>
    /// The names of the option types in the resources of the item options.
    /// </summary>
    private static readonly IReadOnlyDictionary<Guid, string> OptionTypeNames = new Dictionary<Guid, string>
    {
        { ItemOptionTypes.Excellent.Id, "Excellent" },
        { ItemOptionTypes.Option.Id, "Option" },
        { ItemOptionTypes.Luck.Id, "Luck" },
        { ItemOptionTypes.AncientBonus.Id, "AncientBonus" },
        { ItemOptionTypes.Wing.Id, "Wing" },
    };

    private readonly CultureInfo _culture;
    private readonly Func<byte, string?> _getServerName;
    private readonly Func<Guid, ValueTask<string?>> _getGuildName;
    private readonly Func<Guid, ValueTask<ulong?>>? _getContactUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordMessageFormatter"/> class.
    /// </summary>
    /// <param name="culture">The culture of the messages.</param>
    /// <param name="getServerName">The function which gets the name of a game server by its identifier.</param>
    /// <param name="getGuildName">The function which gets the name of a guild by its persistent identifier.</param>
    /// <param name="getContactUser">The function which gets the Discord user of an account, if its player wants to be contacted about their rare drops.</param>
    public DiscordMessageFormatter(CultureInfo culture, Func<byte, string?> getServerName, Func<Guid, ValueTask<string?>> getGuildName, Func<Guid, ValueTask<ulong?>>? getContactUser = null)
    {
        this._culture = culture;
        this._getServerName = getServerName;
        this._getGuildName = getGuildName;
        this._getContactUser = getContactUser;
    }

    /// <summary>
    /// Formats the game event as Discord message.
    /// </summary>
    /// <param name="gameEvent">The game event.</param>
    /// <returns>The category and the message; or <see langword="null"/>, if the event isn't posted.</returns>
    public async ValueTask<(DiscordChannelCategory Category, DiscordEmbed Embed)?> FormatAsync(GameEvent gameEvent)
    {
        return gameEvent switch
        {
            MiniGameEntranceOpenedEvent e => (DiscordChannelCategory.Events, this.CreateEmbed(
                e,
                this.Format(nameof(Resources.MiniGameEntranceOpened_Title), this.Translate(e.MiniGameName)),
                this.Format(nameof(Resources.MiniGameEntranceOpened_Description), RelativeTime(e.EnterEndsAtUtc)),
                EventColor)),
            MiniGameEndedEvent { WinnerName: { } winner } e => (DiscordChannelCategory.Events, this.CreateEmbed(
                e,
                this.Translate(e.MiniGameName),
                this.Format(nameof(Resources.MiniGameWon_Description), Escape(winner), $"{this.Translate(e.MiniGameName)} {e.GameLevel}"),
                EventColor)),
            InvasionStartedEvent e => (DiscordChannelCategory.Events, this.CreateEmbed(
                e,
                e.InvasionName,
                this.Format(nameof(Resources.InvasionStarted_Description), string.Join(", ", e.MapNames.Select(this.Translate))),
                EventColor)),
            InvasionEndedEvent e => (DiscordChannelCategory.Events, this.CreateEmbed(
                e,
                e.InvasionName,
                this.Format(nameof(Resources.InvasionEnded_Description)),
                EventColor)),
            CastleSiegeStateChangedEvent e => await this.FormatCastleSiegeAsync(e).ConfigureAwait(false) is { } embed
                ? (DiscordChannelCategory.CastleSiege, embed)
                : null,
            MonsterItemDroppedEvent e => (DiscordChannelCategory.WorldNews, await this.FormatItemDropAsync(e).ConfigureAwait(false)),
            BossKilledEvent e => (DiscordChannelCategory.WorldNews, this.CreateEmbed(
                e,
                this.Format(nameof(Resources.BossKilled_Title)),
                this.Format(nameof(Resources.BossKilled_Description), Escape(e.KillerName), this.Translate(e.MonsterName), this.Translate(e.MapName)),
                BossKillColor)),
            CharacterLevelMilestoneEvent e => (DiscordChannelCategory.WorldNews, this.CreateEmbed(
                e,
                this.Format(nameof(Resources.LevelMilestone_Title)),
                this.Format(
                    e.IsMasterLevel ? nameof(Resources.MasterLevelMilestone_Description) : nameof(Resources.LevelMilestone_Description),
                    Escape(e.CharacterName),
                    this.Translate(e.CharacterClassName),
                    e.Level),
                MilestoneColor)),
            GlobalNoticeEvent e => (DiscordChannelCategory.Events, this.CreateEmbed(
                e,
                Escape(e.SenderName),
                Escape(e.Message),
                EventColor)),
            _ => null,
        };
    }

    /// <summary>
    /// Escapes the characters of the text which have a meaning in Discord markdown.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The escaped text.</returns>
    internal static string Escape(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (MarkdownCharacters.Contains(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the Discord markup for a time, which each Discord user sees relative to the current time in their own language, e.g. "in 5 minutes".
    /// </summary>
    /// <param name="utc">The time, in UTC.</param>
    /// <returns>The markup.</returns>
    internal static string RelativeTime(DateTime utc) => $"<t:{new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds()}:R>";

    /// <summary>
    /// Gets the Discord markup for a time, which each Discord user sees as date and time in their own time zone.
    /// </summary>
    private static string DateAndTime(DateTime utc) => $"<t:{new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds()}:f>";

    private async ValueTask<DiscordEmbed?> FormatCastleSiegeAsync(CastleSiegeStateChangedEvent e)
    {
        var title = this.Format(nameof(Resources.CastleSiege_Title));
        string? description = e.State switch
        {
            "RegisterGuild" => this.Format(nameof(Resources.CastleSiegeRegistration_Description), DateAndTime(e.StateEndsAtUtc)),
            "Ready" => this.Format(nameof(Resources.CastleSiegeReady_Description), RelativeTime(e.StateEndsAtUtc)),
            "Start" => this.Format(nameof(Resources.CastleSiegeStarted_Description), RelativeTime(e.StateEndsAtUtc)),
            "End" => e.OwnerGuildId is { } ownerId && await this._getGuildName(ownerId).ConfigureAwait(false) is { } guildName
                ? this.Format(nameof(Resources.CastleSiegeEndedWithOwner_Description), Escape(guildName))
                : this.Format(nameof(Resources.CastleSiegeEndedWithoutOwner_Description)),
            _ => null,
        };

        return description is null ? null : this.CreateEmbed(e, title, description, CastleSiegeColor);
    }

    private DiscordEmbed CreateEmbed(GameEvent gameEvent, string title, string description, int color)
    {
        var serverName = this._getServerName(gameEvent.ServerId);
        var footer = serverName is null ? null : this.Format(nameof(Resources.ServerFooter), serverName);
        return new DiscordEmbed(title, description, color, gameEvent.TimestampUtc, footer);
    }

    private async ValueTask<DiscordEmbed> FormatItemDropAsync(MonsterItemDroppedEvent e)
    {
        var description = new StringBuilder(this.Format(nameof(Resources.ItemDropped_Description), Escape(e.KillerName), this.GetItemName(e), this.Translate(e.MonsterName), this.Translate(e.MapName)));
        var lines = new List<string>();
        if (e.HasSkill)
        {
            lines.Add(this.Format(nameof(Resources.ItemDropped_Skill)));
        }

        lines.AddRange(e.Options.Select(this.GetOptionText).OfType<string>().Distinct());
        if (e.AncientSetName is { } setName)
        {
            lines.Add(this.Format(nameof(Resources.ItemDropped_AncientSet), this.Translate(setName)));
        }

        if (lines.Count > 0)
        {
            description.Append('\n');
            foreach (var line in lines)
            {
                description.Append("\n• ").Append(line);
            }
        }

        var contact = e.KillerAccountId is { } accountId && this._getContactUser is { } getContactUser
            ? await getContactUser(accountId).ConfigureAwait(false)
            : null;
        description.Append("\n\n").Append(contact is { } userId
            ? this.Format(nameof(Resources.ItemDropped_Contact), $"<@{userId}>")
            : this.Format(nameof(Resources.ItemDropped_AskInGame), Escape(e.KillerName)));

        return this.CreateEmbed(e, this.Format(nameof(Resources.ItemDropped_Title)), description.ToString(), ItemDropColor);
    }

    /// <summary>
    /// Gets the text of an item option, from the resource of its type and attribute, e.g. <c>ItemOption_Excellent_ExcellentDamageChance</c>;
    /// the attribute's name and value, if there is none.
    /// </summary>
    private string? GetOptionText(DroppedItemOption option)
    {
        OptionTypeNames.TryGetValue(option.TypeId, out var typeName);
        if (typeName == "Luck")
        {
            return this.Format("ItemOption_Luck");
        }

        // Factors are shown as percent, e.g. 1.04 as 4; fractions too, e.g. 0.1 as 10.
        var value = option.IsMultiplier ? (option.Value - 1) * 100 : option.Value is > 0 and < 1 ? option.Value * 100 : option.Value;
        var number = Math.Round(value, 2).ToString("0.##", this._culture);
        var statName = option.AttributeId is { } attributeId && StatNames.TryGetValue(attributeId, out var name) ? name : null;
        var text = typeName is not null && statName is not null
            ? Resources.ResourceManager.GetString($"ItemOption_{typeName}_{statName}", this._culture)
            : null;
        if (text is not null)
        {
            return string.Format(this._culture, text, number);
        }

        return string.IsNullOrEmpty(option.AttributeName) ? null : $"{Escape(option.AttributeName)} +{number}";
    }

    private string GetItemName(MonsterItemDroppedEvent e)
    {
        var parts = new List<string>();
        if (e.IsExcellent)
        {
            parts.Add(this.Format(nameof(Resources.ItemQuality_Excellent)));
        }

        if (e.IsAncient)
        {
            parts.Add(this.Format(nameof(Resources.ItemQuality_Ancient)));
        }

        parts.Add(this.Translate(e.ItemName));
        if (e.ItemLevel > 0)
        {
            parts.Add($"+{e.ItemLevel}");
        }

        return string.Join(' ', parts);
    }

    private string Translate(string localizedValue)
    {
        return Escape(new LocalizedString(localizedValue).GetTranslation(this._culture) ?? localizedValue);
    }

    private string Format(string resourceKey, params object[] args)
    {
        var text = Resources.ResourceManager.GetString(resourceKey, this._culture) ?? resourceKey;
        return args.Length == 0 ? text : string.Format(this._culture, text, args);
    }
}

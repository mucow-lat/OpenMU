// <copyright file="MuWebRanking.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>
/// MuWeb's ranking for the bot's /rank, in place of OpenMU's: that one reads
/// IPlayerContext.GetCharacterRankingAsync, which fails on Season 2 characters (no master level). This
/// one reads the ranking which the OnlineStatusPlugIn serves at http://127.0.0.1:8095/ranking in this
/// same process (server/openmu/VersionSeasonTwo/SeasonTwoRanking.cs, the one of the web client's
/// window): the best characters by resets, and the guilds by their members' resets. Copied into
/// OpenMU's Discord project by server/openmu/Dockerfile, which makes DiscordCommands.RankAsync use it.
/// </summary>
internal static class MuWebRanking
{
    /// <summary>How many characters /rank lists.</summary>
    private const int Characters = 10;

    /// <summary>How many guilds /rank lists.</summary>
    private const int Guilds = 5;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    /// <summary>The Season 2 classes by number, as the web client names them (packages/client/src/game/classes.ts).</summary>
    private static readonly Dictionary<int, string> ClassNames = new()
    {
        [0] = "Dark Wizard", [2] = "Soul Master", [3] = "Grand Master",
        [4] = "Dark Knight", [6] = "Blade Knight", [7] = "Blade Master",
        [8] = "Fairy Elf", [10] = "Muse Elf", [11] = "High Elf",
        [12] = "Magic Gladiator", [13] = "Duel Master",
        [16] = "Dark Lord", [17] = "Lord Emperor",
    };

    /// <summary>
    /// The text of /rank, in Spanish; null when the ranking can't be read (the status plugin off),
    /// and OpenMU's own answer is given instead.
    /// </summary>
    /// <returns>The embed's description.</returns>
    public static async ValueTask<string?> DescriptionAsync()
    {
        var port = Environment.GetEnvironmentVariable("MUWEB_STATUS_PORT") is { Length: > 0 } value ? value : "8095";
        if (port == "0")
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(await Http.GetStringAsync($"http://127.0.0.1:{port}/ranking").ConfigureAwait(false));
            var root = json.RootElement;
            var text = new StringBuilder();
            text.AppendLine("**🏆 Resets**");
            var rank = 0;
            foreach (var c in Items(root, "top").Take(Characters))
            {
                rank++;
                var name = DiscordMessageFormatter.Escape(c.GetProperty("name").GetString() ?? string.Empty);
                var className = ClassNames.GetValueOrDefault(c.GetProperty("classNumber").GetInt32(), "?");
                var resets = c.GetProperty("resets").GetInt32();
                var level = c.GetProperty("level").GetInt32();
                var online = c.TryGetProperty("online", out var o) && o.ValueKind == JsonValueKind.String ? " 🟢" : string.Empty;
                text.AppendLine(CultureInfo.InvariantCulture, $"{rank}. **{name}** ({className}) – {Plural(resets, "reset", "resets")} · nivel {level}{online}");
            }

            if (rank == 0)
            {
                text.AppendLine("Todavía nadie reseteó.");
            }

            text.AppendLine().AppendLine("**🛡️ Guilds**");
            rank = 0;
            foreach (var g in Items(root, "guilds").Take(Guilds))
            {
                rank++;
                var name = DiscordMessageFormatter.Escape(g.GetProperty("name").GetString() ?? string.Empty);
                var master = DiscordMessageFormatter.Escape(g.GetProperty("master").GetString() ?? string.Empty);
                var members = g.GetProperty("members").GetInt32();
                var resets = g.GetProperty("resets").GetInt32();
                var by = master.Length > 0 ? $" · GM {master}" : string.Empty;
                text.AppendLine(CultureInfo.InvariantCulture, $"{rank}. **{name}** – {Plural(resets, "reset", "resets")} · {Plural(members, "miembro", "miembros")}{by}");
            }

            if (rank == 0)
            {
                text.AppendLine("Todavía no hay guilds.");
            }

            text.AppendLine().Append("🟢 conectado ahora · el ranking completo está en el juego.");
            return text.ToString();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static IEnumerable<JsonElement> Items(JsonElement root, string name)
        => root.TryGetProperty(name, out var list) && list.ValueKind == JsonValueKind.Array ? list.EnumerateArray() : [];

    private static string Plural(int n, string one, string many) => string.Create(CultureInfo.InvariantCulture, $"{n} {(n == 1 ? one : many)}");
}

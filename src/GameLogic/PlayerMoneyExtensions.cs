// <copyright file="PlayerMoneyExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.Runtime.CompilerServices;

/// <summary>
/// Extensions to move money between the inventory and the vault of a <see cref="Player"/>.
/// </summary>
public static class PlayerMoneyExtensions
{
    /// <summary>
    /// Gets or sets who hears every change of a player's inventory money made through
    /// <see cref="TryAddMoney"/> and <see cref="TryRemoveMoney"/> (MuCow's zen ledger): the player,
    /// the signed amount and where it came from (the calling file and member, e.g.
    /// "WarpAction.WarpToAsync"). It must not throw; it is called after the money changed.
    /// </summary>
    public static Action<Player, int, string>? MoneyChanged { get; set; }

    /// <summary>
    /// Tries to remove the money from the player inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="value">The value that should be removed.</param>
    /// <returns><c>True</c>, if the player inventory had enough money to remove; Otherwise, <c>false</c>.</returns>
    /// <param name="callerFile">Filled by the compiler: who removes it, for <see cref="MoneyChanged"/>.</param>
    /// <param name="callerMember">Filled by the compiler: who removes it, for <see cref="MoneyChanged"/>.</param>
    public static bool TryRemoveMoney(this Player player, int value, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMember = "")
    {
        if (player.Money < value)
        {
            return false;
        }

        player.Money = checked(player.Money - value);
        Notify(player, -value, callerFile, callerMember);
        return true;
    }

    /// <summary>
    /// Tries to add the money to the player inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="value">The value that should be added.</param>
    /// <returns><c>True</c>, if the player inventory had space to add money; Otherwise, <c>false</c>.</returns>
    /// <param name="callerFile">Filled by the compiler: who adds it, for <see cref="MoneyChanged"/>.</param>
    /// <param name="callerMember">Filled by the compiler: who adds it, for <see cref="MoneyChanged"/>.</param>
    public static bool TryAddMoney(this Player player, int value, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMember = "")
    {
        if (player.Money + value > player.GameContext?.Configuration?.MaximumInventoryMoney)
        {
            return false;
        }

        if (player.Money + value < 0)
        {
            return false;
        }

        player.Money = checked(player.Money + value);
        Notify(player, value, callerFile, callerMember);
        return true;
    }

    /// <summary>
    /// Tries to deposit the money from the player inventory.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="value">The value that should be moved to the vault.</param>
    /// <returns><c>True</c>, if the player inventory had enough money to move; Otherwise, <c>false</c>.</returns>
    public static bool TryDepositVaultMoney(this Player player, uint value)
    {
        if (player.Vault is null)
        {
            return false;
        }

        // The sum is calculated as long and limited to int.MaxValue, so it can't wrap and the value fits into an int.
        var maximum = player.GameContext?.Configuration?.MaximumVaultMoney ?? int.MaxValue;
        if ((long)player.Vault.ItemStorage.Money + value > maximum)
        {
            return false;
        }

        if (player.TryRemoveMoney((int)value))
        {
            return player.Vault.TryAddMoney((int)value);
        }

        return false;
    }

    /// <summary>
    /// Tries to take the money from the vault.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="value">The value that should be retrieved from the vault.</param>
    /// <returns><c>True</c>, if the vault is not locked, had enough money to move and player inventory isn't maximum; Otherwise, <c>false</c>.</returns>
    public static bool TryTakeVaultMoney(this Player player, uint value)
    {
        if (player.Vault is null || player.IsVaultLocked)
        {
            return false;
        }

        // The sum is calculated as long and limited to int.MaxValue, so it can't wrap and the value fits into an int.
        var maximum = player.GameContext?.Configuration?.MaximumInventoryMoney ?? int.MaxValue;
        if ((long)player.Money + value > maximum)
        {
            return false;
        }

        if (player.Vault.TryRemoveMoney((int)value))
        {
            return player.TryAddMoney((int)value);
        }

        return false;
    }

    private static void Notify(Player player, int amount, string callerFile, string callerMember)
    {
        if (amount == 0 || MoneyChanged is not { } listener)
        {
            return;
        }

        try
        {
            // The file name without folders and extension; the path is the build machine's, either slash.
            var file = callerFile[(callerFile.LastIndexOfAny(['/', '\\']) + 1)..];
            if (file.EndsWith(".cs", StringComparison.Ordinal))
            {
                file = file[..^3];
            }

            listener(player, amount, $"{file}.{callerMember}");
        }
        catch
        {
            // The ledger must never disturb the game.
        }
    }
}

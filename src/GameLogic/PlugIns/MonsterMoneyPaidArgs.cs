// <copyright file="MonsterMoneyPaidArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The arguments of the <see cref="IMonsterMoneyPaidPlugIn"/>. Plugin points can't return
/// values, so the amount is passed in and out through this object.
/// </summary>
public sealed class MonsterMoneyPaidArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonsterMoneyPaidArgs"/> class.
    /// </summary>
    /// <param name="amount">The amount of money which is about to be paid.</param>
    public MonsterMoneyPaidArgs(uint amount)
    {
        this.Amount = amount;
    }

    /// <summary>
    /// Gets or sets the amount of money which is paid. It can be modified by the plugins.
    /// </summary>
    public uint Amount { get; set; }
}

// <copyright file="IMonsterMoneyPaidPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when the money of a killed monster is paid directly to the
/// players instead of being dropped to the ground (in mini games like Devil Square, or when the
/// configuration doesn't drop money). It may change the amount before it's split and paid.
/// </summary>
/// <remarks>
/// Money which drops to the ground is handled by the <see cref="IMonsterMoneyDroppedPlugIn"/>.
/// </remarks>
[Guid("A3C5E7F1-2B4D-4E6F-8A1C-3D5E7F9B1C2A")]
[PlugInPoint("Monster money paid", "Plugins which may change the money of a killed monster which is paid directly instead of dropped.")]
public interface IMonsterMoneyPaidPlugIn
{
    /// <summary>
    /// Is called before the money of a killed monster is paid directly to the players.
    /// </summary>
    /// <param name="monster">The killed monster.</param>
    /// <param name="killer">The player who killed the monster.</param>
    /// <param name="args">The amount, which can be modified.</param>
    ValueTask MonsterMoneyPayingAsync(AttackableNpcBase monster, Player killer, MonsterMoneyPaidArgs args);
}

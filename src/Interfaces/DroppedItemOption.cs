// <copyright file="DroppedItemOption.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Interfaces;

/// <summary>
/// An option of a dropped item, as a <see cref="MonsterItemDroppedEvent"/> describes it.
/// </summary>
/// <param name="TypeId">The identifier of the option's type, e.g. of the excellent options.</param>
/// <param name="AttributeId">The identifier of the attribute which the option increases, if there is one.</param>
/// <param name="AttributeName">The name of the attribute which the option increases, in the neutral language.</param>
/// <param name="Value">The value which the option adds at its level; a factor, if <paramref name="IsMultiplier"/> is set.</param>
/// <param name="IsMultiplier">A value indicating whether the value multiplies the attribute, e.g. 1.04 for +4%.</param>
public sealed record DroppedItemOption(Guid TypeId, Guid? AttributeId, string AttributeName, float Value, bool IsMultiplier);

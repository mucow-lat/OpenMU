// <copyright file="MonsterLeash.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Keeps the monsters of a spot at their spot. OpenMU's monsters walk from wherever they are, so after
/// chasing someone they stay there and a spot spreads over the map with the time; Season 2's spots
/// (Devias' 10 Elite Yetis on 36,25) stay together. A monster of a spot (a point or a small area with
/// several monsters) walks inside its spot and <see cref="Slack"/> tiles around it, and back to it
/// after a chase. Single monsters and the big areas walk as before.
/// </summary>
/// <remarks>Used by Monster.RandomMoveAsync (patched in the Dockerfile).</remarks>
internal static class MonsterLeash
{
    /// <summary>How far around its spot a monster of the spot may walk, in tiles.</summary>
    public const int Slack = 3;

    /// <summary>How far one walk back may go: the walker takes 16 steps at most.</summary>
    private const int MaximumReturn = 12;

    /// <summary>A spot's area is this wide at most (the spots are 5×5 or points).</summary>
    private const int MaximumSpotSize = 5;

    /// <summary>
    /// Where a monster's random walk may end: the target itself, or for a monster of a spot one inside
    /// the spot's reach (towards it, when the monster is far after a chase).
    /// </summary>
    /// <param name="area">The monster's spawn area.</param>
    /// <param name="position">Where the monster is.</param>
    /// <param name="x">The random target's x.</param>
    /// <param name="y">The random target's y.</param>
    /// <returns>The target to walk to.</returns>
    public static (int X, int Y) Apply(MonsterSpawnArea area, Point position, int x, int y)
    {
        if (area.SpawnTrigger != SpawnTrigger.Automatic
            || area.Quantity < 2
            || area.X2 - area.X1 >= MaximumSpotSize
            || area.Y2 - area.Y1 >= MaximumSpotSize)
        {
            return (x, y);
        }

        var targetX = Math.Clamp(x, area.X1 - Slack, area.X2 + Slack);
        var targetY = Math.Clamp(y, area.Y1 - Slack, area.Y2 + Slack);
        if (targetX == x && targetY == y)
        {
            return (x, y);
        }

        // Out of its reach: back towards the spot, a walk at a time.
        targetX = position.X + Math.Clamp(targetX - position.X, -MaximumReturn, MaximumReturn);
        targetY = position.Y + Math.Clamp(targetY - position.Y, -MaximumReturn, MaximumReturn);
        return (targetX, targetY);
    }
}

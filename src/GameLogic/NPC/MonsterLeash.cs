// <copyright file="MonsterLeash.cs" company="MuWeb">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Keeps the monsters of a spot at their spot. OpenMU's monsters walk from wherever they are, so after
/// chasing someone they stay there and a spot spreads over the map with the time; Season 2's spots
/// (Devias' 10 Elite Yetis on 36,25) stay together. A monster of a spot (a point or a small area with
/// several monsters) walks inside its spot and <see cref="Slack"/> tiles around it, and back to it
/// after a chase. Single monsters and the big areas walk as before.
/// </summary>
/// <remarks>
/// Used by Monster.RandomMoveAsync and Monster.WalkToAsync. A map can also pen the monsters of its point
/// spawns (<see cref="PennedMaps"/>, a point or a small area): each one walks, and chases, only on the tiles a few steps from its spawn
/// (<see cref="Pen"/>), so the monsters of a hunting ground made of cages stay in their cage. They chase a few
/// steps further (<see cref="GetReach"/>), and can't be hurt from out of that reach.
/// </remarks>
public static class MonsterLeash
{
    /// <summary>How far around its spot a monster of the spot may walk, in tiles.</summary>
    public const int Slack = 3;

    /// <summary>How far one walk back may go: the walker takes 16 steps at most.</summary>
    private const int MaximumReturn = 12;

    /// <summary>A spot's area is narrower than this (the spots are points, 3×3, 5×5 or, the wide ones since 2026-11-19, 9×9).</summary>
    private const int MaximumSpotSize = 9;

    /// <summary>
    /// How many steps beyond its pen a penned monster chases: its reach. Who stands out of it can't hurt the
    /// monster (<see cref="IsOutOfReach"/>), so nobody kills a penned monster from across a fence it can't pass.
    /// </summary>
    public const int ReachSteps = 4;

    private static readonly ConditionalWeakTable<MonsterSpawnArea, Pen> Pens = new();

    private static readonly ConditionalWeakTable<MonsterSpawnArea, Pen> Reaches = new();

    /// <summary>
    /// Gets the maps whose monsters of a point spawn are penned, with how many steps
    /// from their spawn point they may walk. Set by the season's configuration at the start, empty by default.
    /// </summary>
    public static ConcurrentDictionary<short, int> PennedMaps { get; } = new();

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

    /// <summary>
    /// The pen of a monster's spawn, if it is a point and its map pens them: the walkable tiles out of the
    /// safezone at most <see cref="PennedMaps"/> steps (plus half its width) from the spawn's middle, walking around the walls.
    /// </summary>
    /// <param name="area">The monster's spawn area.</param>
    /// <param name="terrain">The map's terrain.</param>
    /// <returns>The pen, or null.</returns>
    internal static Pen? GetPen(MonsterSpawnArea area, GameMapTerrain terrain)
    {
        if (!TryGetPenSteps(area, out var steps))
        {
            return null;
        }

        return Pens.GetValue(area, a => Pen.Create(Centre(a), steps, terrain));
    }

    /// <summary>
    /// The reach of a penned monster: the tiles <see cref="ReachSteps"/> steps beyond its pen, walking around the
    /// walls. It chases on them, and only from them it can be hurt.
    /// </summary>
    /// <param name="area">The monster's spawn area.</param>
    /// <param name="terrain">The map's terrain.</param>
    /// <returns>The reach, or null when the monster isn't penned.</returns>
    internal static Pen? GetReach(MonsterSpawnArea area, GameMapTerrain terrain)
    {
        if (!TryGetPenSteps(area, out var steps))
        {
            return null;
        }

        return Reaches.GetValue(area, a => Pen.Create(Centre(a), steps + ReachSteps, terrain));
    }

    /// <summary>
    /// The steps of a penned spawn's pen from its middle: the map's (<see cref="PennedMaps"/>) plus half the
    /// spawn's width, for a point or a small area (a group of a spot, at most <see cref="MaximumSpotSize"/> wide).
    /// </summary>
    private static bool TryGetPenSteps(MonsterSpawnArea area, out int steps)
    {
        steps = 0;
        if (area.GameMap is not { } map
            || !PennedMaps.TryGetValue(map.Number, out var mapSteps)
            || mapSteps <= 0
            || area.SpawnTrigger != SpawnTrigger.Automatic
            || area.X2 - area.X1 >= MaximumSpotSize
            || area.Y2 - area.Y1 >= MaximumSpotSize)
        {
            return false;
        }

        steps = mapSteps + (Math.Max(area.X2 - area.X1, area.Y2 - area.Y1) / 2);
        return true;
    }

    private static Point Centre(MonsterSpawnArea area) => new((byte)((area.X1 + area.X2) / 2), (byte)((area.Y1 + area.Y2) / 2));

    /// <summary>
    /// Whether an attacker stands out of a penned monster's reach (from where the monster couldn't hit back).
    /// </summary>
    /// <param name="area">The monster's spawn area.</param>
    /// <param name="terrain">The map's terrain.</param>
    /// <param name="position">Where the attacker stands.</param>
    /// <returns>True if the monster is penned and the attacker out of its reach.</returns>
    public static bool IsOutOfReach(MonsterSpawnArea area, GameMapTerrain terrain, Point position)
        => GetReach(area, terrain) is { } reach && !reach.Contains(position);

    /// <summary>
    /// The tiles a penned monster may stand on.
    /// </summary>
    internal sealed class Pen
    {
        private readonly HashSet<Point> _tiles;
        private readonly Point[] _list;

        private Pen(Point center, HashSet<Point> tiles)
        {
            this.Center = center;
            this._tiles = tiles;
            this._list = [.. tiles];
        }

        /// <summary>Gets the spawn point.</summary>
        public Point Center { get; }

        /// <summary>Whether a tile is in the pen.</summary>
        /// <param name="point">The tile.</param>
        /// <returns>True if it is.</returns>
        public bool Contains(Point point) => this._tiles.Contains(point);

        /// <summary>A random tile of the pen.</summary>
        /// <returns>The tile.</returns>
        public Point RandomTile() => this._list[Rand.NextInt(0, this._list.Length)];

        /// <summary>Fills the pen from the spawn point, step by step (8 directions) over walkable tiles out of the safezone.</summary>
        internal static Pen Create(Point center, int steps, GameMapTerrain terrain)
        {
            var tiles = new HashSet<Point> { center };
            var front = new List<Point> { center };
            for (var step = 0; step < steps && front.Count > 0; step++)
            {
                var next = new List<Point>();
                foreach (var tile in front)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var (x, y) = (tile.X + dx, tile.Y + dy);
                            if (x is < 0 or > byte.MaxValue || y is < 0 or > byte.MaxValue || terrain.AIgrid[x, y] != 1)
                            {
                                continue;
                            }

                            var point = new Point((byte)x, (byte)y);
                            if (tiles.Add(point))
                            {
                                next.Add(point);
                            }
                        }
                    }
                }

                front = next;
            }

            return new Pen(center, tiles);
        }
    }
}

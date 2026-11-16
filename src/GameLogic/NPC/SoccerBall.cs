// <copyright file="SoccerBall.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;
using Nito.AsyncEx;

/// <summary>
/// The implementation of a soccer ball.
/// They can be attacked, but take no damage. Instead, they move into the direction in which they got hit.
/// </summary>
public sealed class SoccerBall : NonPlayerCharacter, IAttackable, IMovable
{
    private readonly AsyncLock _moveLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SoccerBall"/> class.
    /// </summary>
    /// <param name="spawnInfo">The spawn information.</param>
    /// <param name="stats">The stats.</param>
    /// <param name="map">The map on which this instance will spawn.</param>
    public SoccerBall(MonsterSpawnArea spawnInfo, MonsterDefinition stats, GameMap map)
        : base(spawnInfo, stats, map)
    {
        this.MagicEffectList = new MagicEffectsList(this);
        this.Attributes = new AttributeSystem(Enumerable.Empty<IAttribute>(), Enumerable.Empty<IAttribute>(), Enumerable.Empty<AttributeRelationship>());
    }

    /// <summary>
    /// Occurs when it has been moved.
    /// </summary>
    public event AsyncEventHandler<(Point From, Point To)>? Moved;

    /// <inheritdoc />
    public event EventHandler<DeathInformation>? Died;

    /// <inheritdoc />
    public IAttributeSystem Attributes { get; }

    /// <inheritdoc />
    public MagicEffectsList MagicEffectList { get; }

    /// <inheritdoc />
    public bool IsAlive => true;

    /// <inheritdoc />
    public bool IsTeleporting => false;

    /// <inheritdoc />
    public DeathInformation? LastDeath => null;

    /// <inheritdoc />
    public async ValueTask<HitInfo?> AttackByAsync(IAttacker attacker, SkillEntry? skill, bool isCombo, double damageFactor = 1.0, bool? isFinalStreakHit = null)
    {
        var direction = attacker.GetDirectionTo(this);
        await this.MoveToDirectionAsync(direction, skill is { }).ConfigureAwait(false);
        return null;
    }

    /// <inheritdoc />
    public ValueTask ReflectDamageAsync(IAttacker reflector, uint damage)
    {
        // A ball doesn't attack, so it doesn't reflect.
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ApplyPoisonDamageAsync(IAttacker initialAttacker, uint damage)
    {
        // A ball doesn't take any damage
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ApplyBleedingDamageAsync(IAttacker initialAttacker, uint damage)
    {
        // A ball doesn't take any damage
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask KillInstantlyAsync()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async ValueTask MoveAsync(Point target)
    {
        var old = this.Position;
        await this.CurrentMap.MoveAsync(this, target, this._moveLock, MoveType.Instant).ConfigureAwait(false);
        if (this.Moved is { } eventHandler)
        {
            await eventHandler((From: old, To: target)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Moves the ball up to 2 tiles (3 with a skill) away from the attacker. Changed for MuWeb (see
    /// server/openmu/Dockerfile): OpenMU stopped the ball at the first blocked tile, so a ball in a
    /// corner of the field could only be hit towards the walls and was stuck for the rest of the match.
    /// Now it bounces like a billiard ball: the part of its way that runs into a wall is turned around.
    /// </summary>
    /// <param name="direction">The direction from the attacker to the ball.</param>
    /// <param name="withSkill">Whether it was hit with a skill.</param>
    private async ValueTask MoveToDirectionAsync(Direction direction, bool withSkill)
    {
        var terrain = this.CurrentMap.Terrain;
        var range = withSkill ? 3 : 2;
        var step = this.Position.CalculateTargetPoint(direction);
        var dx = step.X - this.Position.X;
        var dy = step.Y - this.Position.Y;
        if (dx == 0 && dy == 0)
        {
            return;
        }

        bool IsFree(int x, int y) => x is >= 0 and <= byte.MaxValue && y is >= 0 and <= byte.MaxValue && terrain.AIgrid[x, y] == 1;

        int px = this.Position.X;
        int py = this.Position.Y;
        for (int i = 0; i < range; i++)
        {
            if (!IsFree(px + dx, py + dy))
            {
                // Bounce off the wall: turn around the blocked part of the way.
                if (dx != 0 && !IsFree(px + dx, py))
                {
                    dx = -dx;
                }

                if (dy != 0 && !IsFree(px, py + dy))
                {
                    dy = -dy;
                }

                if (!IsFree(px + dx, py + dy))
                {
                    // Only the diagonal tile was blocked (an outer corner): back the way it came.
                    dx = -(step.X - this.Position.X);
                    dy = -(step.Y - this.Position.Y);
                    if (!IsFree(px + dx, py + dy))
                    {
                        break;
                    }
                }
            }

            px += dx;
            py += dy;
        }

        var finalTarget = new Point((byte)px, (byte)py);
        if (finalTarget != this.Position)
        {
            await this.MoveAsync(finalTarget).ConfigureAwait(false);
        }
    }
}
// <copyright file="IConfigurationReconcilePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin which brings the configuration of the database to what the server is deployed with, at
/// every start: after a new database got its initial data or an existing one its mandatory updates,
/// and before the game servers start. Unlike a configuration update, which runs once, it runs every
/// time, so it has to be idempotent: it only changes what differs, and changes nothing the second time.
/// </summary>
/// <remarks>
/// Examples: the game servers which an environment variable defines, or values which a deployment
/// keeps in code instead of editing them on the admin panel.
/// </remarks>
[Guid("5B2E8F14-7C3A-4D96-A1E5-2F8B0C6D9E47")]
[PlugInPoint("Configuration reconcile", "Brings the configuration to what the server is deployed with, at every start, before the game servers start.")]
public interface IConfigurationReconcilePlugIn
{
    /// <summary>
    /// Reconciles the configuration.
    /// </summary>
    /// <param name="contextProvider">The context provider of the database.</param>
    /// <returns>The task.</returns>
    ValueTask ReconcileAsync(IPersistenceContextProvider contextProvider);
}

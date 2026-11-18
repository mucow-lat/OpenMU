// <copyright file="LetterDeleteAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Messenger;

using MUnique.OpenMU.GameLogic.Views.Messenger;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to delete a letter.
/// </summary>
public class LetterDeleteAction
{
    /// <summary>
    /// Deletes the letter.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="letter">The letter.</param>
    public async ValueTask DeleteLetterAsync(Player player, LetterHeader letter)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        if (player.SelectedCharacter is not { } character)
        {
            player.Logger.LogWarning("no character selected, player {0}", player);
            return;
        }

        var letterIndex = character.Letters.IndexOf(letter);
        if (letterIndex < 0)
        {
            player.Logger.LogWarning("Letter {0} not found in the letters of player {1}", letter.Id, player);
            return;
        }

        // The body is a row of its own which references the header, and it's only loaded into
        // the context when the letter is read. Without deleting it here, deleting a letter which
        // wasn't read in this session removes just the header: the database rejects that (foreign
        // key), and since the pending delete stays in the context, every following save of the
        // player fails too - the whole session is rolled back at the next login.
        if (await player.PersistenceContext.GetLetterBodyByHeaderIdAsync(letter.Id).ConfigureAwait(false) is { } letterBody)
        {
            await player.PersistenceContext.DeleteAsync(letterBody).ConfigureAwait(false);
        }

        character.Letters.RemoveAt(letterIndex);
        await player.InvokeViewPlugInAsync<ILetterDeletedPlugIn>(p => p.LetterDeletedAsync((ushort)letterIndex)).ConfigureAwait(false);
    }
}
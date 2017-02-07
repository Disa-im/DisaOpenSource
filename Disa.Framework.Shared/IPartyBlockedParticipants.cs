using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface IPartyBlockedParticipants
    {
        /// <summary>
        /// Set the result of this with an array of DisaParticipants which are blocked for this party.
        /// </summary>
        /// <returns>A task that sets the result.</returns>
        /// <param name="group">The BubbleGroup in context.</param>
        /// <param name="result">The result Action to set the array of DisaParticipants on.</param>
        Task GetPartyBlockedParticipants(BubbleGroup group, Action<DisaParticipant[]> result);
        /// <summary>
        /// Set the result as true if the current user can unblock participants, false otherwise
        /// </summary>
        /// <returns>The party unblock participants.</returns>
        /// <param name="group">The BubbleGroup in context.</param>
        /// <param name="result">The result Action to set the array of DisaParticipants on.</param>
        Task CanPartyUnblockParticipants(BubbleGroup group, Action<bool> result);
        /// <summary>
        /// Set the result as true if the unblocking was successful, false otherwise, false otherwise.
        /// </summary>
        /// <returns>A task that sets the result.</returns>
        /// <param name="group">The BubbleGroup in context.</param>
        /// <param name="participant">The Participant to unblock</param>
        /// <param name="result">The result Action to set the array of DisaParticipants on.</param>
        Task UnblockPartyParticipant(BubbleGroup group, DisaParticipant participant, Action<bool> result);

        /// <summary>
        /// Set the result as true if the unblocking was successful, false otherwise.
        /// </summary>
        /// <returns>A task that sets the result.</returns>
        /// <param name="group">The BubbleGroup in context.</param>
        /// <param name="address">the address of whom the Disa thumbnail is needed</param>
        /// <param name="result">The result Action to set the DisaThumbnail on.</param>
        Task GetPartyBlockedParticipantPicture(BubbleGroup group, string address, Action<DisaThumbnail> result);

    }
}


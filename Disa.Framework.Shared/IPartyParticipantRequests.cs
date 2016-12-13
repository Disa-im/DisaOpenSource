using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IPartyParticipantRequests
    {
        /// <summary>
        /// Set the result of the <see cref="Action"/> as the collection of <see cref="DisaParticipant"/>s
        /// requesting to be added to this <see cref="BubbleGroup"/>.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/></returns>
        Task GetPartyParticipantRequests(BubbleGroup group, Action<DisaParticipant[]> result);

        /// <summary>
        /// Set the result of the <see cref="Action"/> as true if the <see cref="DisaParticipant"/> was
        /// accepted or denied based on the value for acceptDeny, false otherwise.
        /// </summary>
        /// <param name="group">The <see cref="BubbleGroup"/> in context.</param>
        /// <param name="participant">The <see cref="DisaParticipant"/> to be accepted or denied to this <see cref="BubbleGroup"/>.</param>
        /// <param name="acceptDeny">True to accept or false to deny.</param>
        /// <param name="result"><see cref="Action"/> on which the result should be set.</param>
        /// <returns>A new <see cref="Task"/> that sets the result <see cref="Action"/></returns>
        Task PartyParticipantRequestAction(BubbleGroup group, DisaParticipant participant, bool acceptDeny, Action<bool> result);
    }
}

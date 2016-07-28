using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IPartyOptions
    {
        // thumbnail

        Task GetPartyPhoto(BubbleGroup group, DisaParticipant participant, bool preview, Action<DisaThumbnail> result);

        Task CanSetPartyPhoto(BubbleGroup group, Action<bool> result);

        Task CanViewPartyPhoto(BubbleGroup group, Action<bool> result);

        Task CanDeletePartyPhoto(BubbleGroup group, Action<bool> result);

        Task SetPartyPhoto(BubbleGroup group, byte[] bytes, Action<DisaThumbnail> result);

        Task DeletePartyPhoto(BubbleGroup group);

        // name

        Task GetPartyName(BubbleGroup group, Action<string> result);

        Task CanSetPartyName(BubbleGroup group, Action<bool> result);

        Task SetPartyName(BubbleGroup group, string name);

        Task GetPartyNameMaxLength(BubbleGroup group, Action<int> result);

        // participants

        Task GetPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result);

        Task CanAddPartyParticipant(BubbleGroup group, Action<bool> result);

        Task AddPartyParticipant(BubbleGroup group, DisaParticipant participant);

        Task CanDeletePartyParticipant(BubbleGroup group, Action<bool> result);

        Task DeletePartyParticipant(BubbleGroup group, DisaParticipant participant);

        Task CanPromotePartyParticipantToLeader(BubbleGroup group, Action<bool> result);

        Task PromotePartyParticipantToLeader(BubbleGroup group, DisaParticipant participant);

        Task GetPartyLeaders(BubbleGroup group, Action<DisaParticipant[]> result);

        int GetMaxParticipantsAllowed();

        Task ConvertContactIdToParticipant(Contact contact,
            Contact.ID contactId, Action<DisaParticipant> result);

        // deleting

        Task CanLeaveParty(BubbleGroup group, Action<bool> result);

        Task LeaveParty(BubbleGroup group);

        // houskeeping

        Task PartyOptionsClosed();
    }
}


using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IPartyOptions
    {
        // make a method that does an rpc call. everything must wait until rpc call is done. when rpc call is done, it will assign a object to _fullChat field.

       // you will have a method like IsMyselfInParty, and IsMySelfAnAdminOfParty, those willenumerate the _fulLClient properties,

        // thumbnai

            // create is myself in participant list cache method. remember to cache and sync to force all calls to wait for the one that is processing the rpc call


        Task GetPartyPhoto(BubbleGroup group, DisaParticipant participant, bool preview, Action<DisaThumbnail> result);

        Task CanSetPartyPhoto(BubbleGroup group, Action<bool> result); //not in the group, can't set photo, so maje

        Task CanViewPartyPhoto(BubbleGroup group, Action<bool> result);

        Task CanDeletePartyPhoto(BubbleGroup group, Action<bool> result); // not in the group, can't set photo

        Task SetPartyPhoto(BubbleGroup group, byte[] bytes, Action<DisaThumbnail> result); // make sure to remove from telegram cache, both small and large previews, Platform.GenerateJpegBytes

        Task DeletePartyPhoto(BubbleGroup group);

        // name

        Task GetPartyName(BubbleGroup group, Action<string> result); // 

        Task CanSetPartyName(BubbleGroup group, Action<bool> result); // not in the grop...

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

        Task PartyOptionsClosed(); // set _fulLClient to null and other shit you have disposed.
    }
}


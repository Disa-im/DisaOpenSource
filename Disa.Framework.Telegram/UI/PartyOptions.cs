using System;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IPartyOptions
    {
        public Task GetPartyPhoto(BubbleGroup group, DisaParticipant participant, bool preview, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {
                DisaThumbnail thumbnail;
                if (participant == null)
                {
                    thumbnail = GetThumbnail(group.Address, group.IsParty, preview);
                }
                else 
                {
                    thumbnail = GetThumbnail(participant.Address, false, preview);
                }
                result(thumbnail);
            });
        }

        public Task CanSetPartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                var chat = GetFullChat(group.Address);
                DebugPrint(ObjectDumper.Dump(chat));
            });
        }

        public Task CanViewPartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task CanDeletePartyPhoto(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task SetPartyPhoto(BubbleGroup group, byte[] bytes, Action<DisaThumbnail> result)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task DeletePartyPhoto(BubbleGroup group)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task GetPartyName(BubbleGroup group, Action<string> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result("lol");
            });
        }

        public Task CanSetPartyName(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(false);
            });
        }

        public Task SetPartyName(BubbleGroup group, string name)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task GetPartyNameMaxLength(BubbleGroup group, Action<int> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(1000);
            });
        }

        public Task GetPartyParticipants(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(null);
            });
        }

        public Task CanAddPartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task AddPartyParticipant(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task CanDeletePartyParticipant(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(false);
            });
        }

        public Task DeletePartyParticipant(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task CanPromotePartyParticipantToLeader(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(false);
            });
        }

        public Task PromotePartyParticipantToLeader(BubbleGroup group, DisaParticipant participant)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task GetPartyLeaders(BubbleGroup group, Action<DisaParticipant[]> result)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public int GetMaxParticipantsAllowed()
        {
            return MaxParticipants;
        }

        public Task ConvertContactIdToParticipant(Contact contact,
                                           Contact.ID contactId, Action<DisaParticipant> result)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task CanLeaveParty(BubbleGroup group, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(true);
            });
        }

        public Task LeaveParty(BubbleGroup group)
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }

        public Task PartyOptionsClosed()
        {
            return Task.Factory.StartNew(() =>
            {

            });
        }
    }
}


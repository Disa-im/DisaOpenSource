using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface IPrivacyList
    {
        Task GetPrivacyList(Action<string[]> addresses);

        Task SetPrivacyList(string[] addresses);

        Task GetAddressPicture(string address, Action<DisaThumbnail> result);

        Task GetAddressTitle(string address, Action<string> result);

        Task ConvertContactIdToParticipant(Contact contact,
            Contact.ID contactId, Action<DisaParticipant> result);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewMessage
    {
        Task GetContacts(string query, bool searchForParties, Action<List<Contact>> result);

        Task GetContactsFavorites(Action<List<Contact>> result);

        Task GetContactPhoto(Contact contact, bool preview, Action<DisaThumbnail> result);

        Task FetchBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result);

        Task FetchBubbleGroupAddress(Tuple<Contact, Contact.ID>[] contacts, Action<bool, string> result);

        Task GetContactFromAddress(string address, Action<Contact, Contact.ID> result);

        int MaximumParticipants
        {
            get;
        }

        bool FastSearch
        {
            get;
        }

        bool CanAddContact
        {
            get;
        }

        //
        // INewMessageExtended
        //

        Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result);

        bool SupportsShareLinks
        {
            get;
        }

    }
}


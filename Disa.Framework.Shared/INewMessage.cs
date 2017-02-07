using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewMessage
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var newMessageUi = service as INewMessage
        // if (newMessageUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

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
        // Begin interface extensions below (previously INewMethodsExtended). For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameWorkMethods.INewMessageXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //

        Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result);

        bool SupportsShareLinks
        {
            get;
        }

    }
}


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

        /// <summary>
        /// Does the <see cref="Service"/> support incremental searching.
        /// 
        /// True if you can submit search requests to the <see cref="Service"/> as the user is typing. 
        /// False if you must submit a search request to the <see cref="Service"/> after the user has designated via some type of 
        /// submit indication to search on the text they have input.
        /// </summary>
        bool FastSearch
        {
            get;
        }

        /// <summary>
        /// Does the <see cref="Service"/> support adding in a new contact from the on device contacts
        /// when creating a group.
        /// 
        /// True if the <see cref="Service"/> allows adding a new contact from the on device contacts when creating a group.
        /// False if the <see cref="Service"/> only allows selecting from a predefined set of contacts
        /// specified by the <see cref="Service"/>.
        /// </summary>
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

        /// <summary>
        /// Specify the text string to be used as a hint in the New Message search edit box.
        /// 
        /// Plugin developers may want to use this string to guide users in what can be searched on - 
        /// such as app-specific contact name and/or device-specific contact phone number.
        /// 
        /// If null is returned, will default to a language specific version for "Search".
        /// </summary>
        string SearchHint
        {
            get;
        }
    }
}


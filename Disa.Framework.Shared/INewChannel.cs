using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewChannel
    {
        //
        // Original interface - for these you can use the following test methodology:
        //
        // // Does the service implement the required interface?
        // var newChannelUi = service as INewChannel
        // if (newChannelUi != null)
        // {
        //     .
        //     .
        //     .
        // }
        //

        Task FetchChannelBubbleGroupAddress(string name, string description, Action<bool, string> result);

        Task InviteToChannel(BubbleGroup group, Tuple<Contact, Contact.ID>[] contacts, Action<bool> result);

        Task GetChannelContacts(string query, Action<List<Contact>> result);

        //
        // Begin interface extensions below. For these you must use the following test methodology
        // or something similar:
        // 
        // // Do we have the required method?
        // if(DisaFrameworkMethods.Missing(service, DisaFrameWorkMethods.INewChannelXxx)
        // {
        //     return;
        // }
        //
        // // Ok to proceed
        //

    }
}


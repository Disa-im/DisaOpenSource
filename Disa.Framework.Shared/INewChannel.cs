using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewChannel
    {
        Task FetchChannelBubbleGroupAddress(string name, string description, Action<bool, string> result);

        Task InviteToChannel(BubbleGroup group, Tuple<Contact, Contact.ID>[] contacts, Action<bool> result);
    }
}


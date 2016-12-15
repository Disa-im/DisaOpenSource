using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    [DisaFramework]
    public interface INewChannel
    {
        Task FetchBubbleGroup(Contact.ID[] contactIds, Action<BubbleGroup> result);

        Task FetchBubbleGroupAddress(Tuple<Contact, Contact.ID>[] contacts, Action<bool, string> result);
    }
}


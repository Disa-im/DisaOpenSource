using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public interface INewMessageExtended
    {
        Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result);

        bool SupportsShareLinks
        {
            get;
        }
    }
}
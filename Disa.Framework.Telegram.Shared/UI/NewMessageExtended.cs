using System;
using System.Threading.Tasks;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : INewMessageExtended
    {
        public bool SupportsShareLinks
        {
            get
            {
                return true;
            }
        }

        public Task FetchBubbleGroupAddressFromLink(string link, Action<Tuple<Contact, Contact.ID>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                result(new Tuple<Contact, Contact.ID>(null, null));
            });
        }
    }
}


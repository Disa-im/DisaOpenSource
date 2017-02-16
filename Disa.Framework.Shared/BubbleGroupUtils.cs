using System;
using System.Linq;

namespace Disa.Framework
{
    public static class BubbleGroupUtils
    {
        public const string BubbleGroupPartyDelimeter = ",";

        public static string GeneratePartyTitle(string[] names)
        {
            var title = String.Empty;

            for (int i = 0; i < names.Length; i++)
            {
                title += names[i];
                if (i != names.Length - 1)
                {
                    title += BubbleGroupPartyDelimeter + " ";
                }
            }

            if (String.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return title;
        }

        public static string GeneratePartyTitle(Contact[] contacts)
        {
            var title = String.Empty;

            foreach (var contact in contacts)
            {
                if (contact != null)
                {
                    title += contact.FullName;
                    if (contact != contacts.Last())
                    {
                        title += BubbleGroupPartyDelimeter + " ";
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return title;
        }

        public static string GenerateComposeAddress(Contact.ID[] ids)
        {
            var address = String.Empty;
            foreach (var id in ids)
            {
                address += id.Id;
                if (id != ids.Last())
                {
                    address += ",";
                }
            }

            if (String.IsNullOrWhiteSpace(address))
            {
                return null;
            }
            return "compose:" + address;
        }
    }
}
using System.Collections.Generic;

namespace Disa.Framework
{
    public class PhoneBookContact
    {
        public List<PhoneNumber> PhoneNumbers { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string ThumbnailUri { get; set; }
        public string ContactId { get; private set; }

        public PhoneBookContact(List<PhoneNumber> phoneNumbers, string firstName, string lastName, string thumbnailUri = null, string contactId = null)
        {
            PhoneNumbers = phoneNumbers;
            FirstName = firstName;
            LastName = lastName;
            ThumbnailUri = thumbnailUri;
            ContactId = contactId;
        }

        public string FullName
        {
            get
            {
                string name = null;

                if (FirstName != null)
                {
                    name = FirstName;
                }
                if (LastName != null)
                {
                    if (name == null)
                        name = "";

                    name += " " + LastName;
                }

                return name;
            }
        }

        public class PhoneNumber
        {
            public string Number { get; set; }
            public string NumberType { get; set; }

            public PhoneNumber(string number, string numberType)
            {
                Number = number;
                NumberType = numberType;
            }
        }
    }
}
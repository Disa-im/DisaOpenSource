using System.Collections.Generic;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class PhoneBookContact
    {
        [ProtoMember(1)]
        public List<PhoneNumber> PhoneNumbers { get; private set; }
        [ProtoMember(2)]
        public string FirstName { get; private set; }
        [ProtoMember(3)]
        public string LastName { get; private set; }
        [ProtoMember(4)]
        public string ThumbnailUri { get; set; }
        [ProtoMember(5)]
        public string ContactId { get; private set; }

        private PhoneBookContact()
        {
        }

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

        [ProtoContract]
        public class PhoneNumber
        {
            [ProtoMember(1)]
            public string Number { get; set; }
            [ProtoMember(2)]
            public string NumberType { get; set; }

            public PhoneNumber(string number, string numberType)
            {
                Number = number;
                NumberType = numberType;
            }

            private PhoneNumber()
            {
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace Disa.Framework
{
    public class ContactCard
    {
        public class ContactCardPhone
        {
            public string Number { get; set;}
            public bool IsHome { get; set; }
            public bool IsWork { get; set; }
        }

        private List<ContactCardPhone> _phones;

        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public List<ContactCardPhone> Phones
        {
            get 
            {
                if (_phones == null) 
                {
                    _phones = new List<ContactCardPhone>();
                }
                return _phones;
            }
        }
    }
}


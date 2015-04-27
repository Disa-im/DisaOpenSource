using System.Collections.Generic;

namespace Disa.Framework
{
    public abstract class Contact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; }
        public List<ID> Ids { get; set; }
        public long LastSeen { get; set; }
        public bool? Available { get; set; }

        public string FullName
        {
            get
            {
                string name = null;
                if (!string.IsNullOrEmpty(FirstName))
                {
                    name = FirstName;
                }
                if (!string.IsNullOrEmpty(LastName))
                {
                    if (name == null)
                        name = string.Empty;

                    name += " " + LastName;
                }

                return name;
            }
        }

        public class ID
        {
            public Service Service { get; set; }
            public string Id { get; set; }
            public string LegibleId { get; set; }
            public string Name { get; set; }
            public object Tag { get; set; }
        }

        public class PartyID : ID
        {
        }
    }

    public class PartyContact : Contact
    {
    }
}
using System;
using System.Xml.Serialization;
using Newtonsoft.Json;
using ProtoBuf;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    public class DisaParticipant
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Address { get; set; }

		[JsonIgnore]
        [XmlIgnore]
        public bool IsPhotoSetFromService { get; set; }
		[JsonIgnore]
        [XmlIgnore]
        public bool IsPhotoSetInitiallyFromCache { get; set; }
        [ProtoMember(3)]
        public DisaThumbnail Photo { get; set; }

		[JsonIgnore]
		//FIXME: Do we need to prevent other serialization types from serializing this?
        public bool Unknown { get; set; }

        public DisaParticipant(string name, string address)
        {
            Name = name;
            Address = address;
        }

        public DisaParticipant()
        {
            
        }
    }
}
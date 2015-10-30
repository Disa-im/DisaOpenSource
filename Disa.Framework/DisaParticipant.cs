using System;
using System.Xml.Serialization;
using ProtoBuf;
using Newtonsoft.Json;

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

        [XmlIgnore]
        [JsonIgnore]
        public bool IsPhotoSetFromService { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public bool IsPhotoSetInitiallyFromCache { get; set; }

        [ProtoMember(3)]
        public DisaThumbnail Photo { get; set; }

        [XmlIgnore]
        [JsonIgnore]
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
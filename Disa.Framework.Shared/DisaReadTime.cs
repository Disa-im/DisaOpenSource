using System;
using ProtoBuf;

namespace Disa.Framework
{
    [ProtoContract]
    public class DisaReadTime
    {
        public static string SingletonPartyParticipantAddress
        {
            get
            {
                return "&^%$#@?!singletonpartyparticipantaddress!?@#$%^&";
            }
        }

        [NonSerialized]
        public object Tag;
        [ProtoMember(1)]
        public string ParticipantAddress { get; set; }
        [ProtoMember(2)]
        public long Time { get; set; }
    }
}


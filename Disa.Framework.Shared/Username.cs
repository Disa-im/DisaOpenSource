using System;
using ProtoBuf;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    public class Username : Mentions
    {
        /// <summary>
        /// The <see cref="DisaParticipant.Name"/> for this username. 
        /// </summary>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// The <see cref="DisaParticipant.Address"/> for this username. 
        /// </summary>
        [ProtoMember(2)]
        public string Address { get; set; }
    }
}

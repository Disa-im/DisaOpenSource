using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    public class KeyboardButtonRow
    {
        [ProtoMember(1)]
        public List<KeyboardButton> Buttons {get; set; }
    }
}

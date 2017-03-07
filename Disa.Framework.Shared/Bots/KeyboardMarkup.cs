using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(KeyboardMarkupHide))]
    [ProtoInclude(102, typeof(KeyboardMarkupForceReply))]
    [ProtoInclude(103, typeof(KeyboardCustomMarkup))]
    [ProtoInclude(104, typeof(KeyboardInlineMarkup))]
    public abstract class KeyboardMarkup
    {
    }

    public class KeyboardMarkupHide : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool Selective { get; set; }

    }

    public class KeyboardMarkupForceReply : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool SingleUse { get; set; }

        [ProtoMember(2)]
        public bool Selective { get; set; }
    }

    public class KeyboardCustomMarkup : KeyboardMarkup
    {
        [ProtoMember(1)]
        public bool Resize { get; set; }

        [ProtoMember(2)]
        public bool SingleUse { get; set; }

        [ProtoMember(3)]
        public bool Selective { get; set; }

        [ProtoMember(4)]
        public List<KeyboardButtonRow> Rows { get; set; }
    }

    public class KeyboardInlineMarkup : KeyboardMarkup
    {
        [ProtoMember(1)]
        public List<KeyboardButtonRow> Rows { get; set; }
    }
}

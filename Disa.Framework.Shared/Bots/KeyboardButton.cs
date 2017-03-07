using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(KeyboardButtonStandard))]
    [ProtoInclude(102, typeof(KeyboardButtonUrl))]
    [ProtoInclude(103, typeof(KeyboardButtonCallback))]
    [ProtoInclude(104, typeof(KeyboardButtonRequestPhone))]
    [ProtoInclude(105, typeof(KeyboardButtonRequestGeoLocation))]
    [ProtoInclude(106, typeof(KeyboardButtonSwitchInline))]
    public abstract class KeyboardButton
    {
    }

    public class KeyboardButtonStandard : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    public class KeyboardButtonUrl : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public string Url { get; set; }
    }

    public class KeyboardButtonCallback : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public byte[] Data { get; set; }
    }

    public class KeyboardButtonRequestPhone : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    public class KeyboardButtonRequestGeoLocation : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    public class KeyboardButtonSwitchInline : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public string Query { get; set; }
    }
}

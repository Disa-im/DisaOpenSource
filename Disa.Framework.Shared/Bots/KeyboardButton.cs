using ProtoBuf;
using System;

namespace Disa.Framework.Bots
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(101, typeof(KeyboardButtonCustom))]
    [ProtoInclude(102, typeof(KeyboardButtonUrl))]
    [ProtoInclude(103, typeof(KeyboardButtonCallback))]
    [ProtoInclude(104, typeof(KeyboardButtonRequestPhone))]
    [ProtoInclude(105, typeof(KeyboardButtonRequestGeoLocation))]
    [ProtoInclude(106, typeof(KeyboardButtonSwitchInline))]
    public abstract class KeyboardButton
    {
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonCustom : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonUrl : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public string Url { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonCallback : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public byte[] Data { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonRequestPhone : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonRequestGeoLocation : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }
    }

    [Serializable]
    [ProtoContract]
    public class KeyboardButtonSwitchInline : KeyboardButton
    {
        [ProtoMember(1)]
        public string Text { get; set; }

        [ProtoMember(2)]
        public string Query { get; set; }
    }
}

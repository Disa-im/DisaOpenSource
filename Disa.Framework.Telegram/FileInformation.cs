using ProtoBuf;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    [ProtoContract]
    public class FileInformation
    {
        [ProtoMember(1)]
        public FileLocation FileLocation { get; set; }
        [ProtoMember(2)]
        public uint Size { get; set; }
        [ProtoMember(3)]
        public string FileType { get; set; }
        [ProtoMember(4)]
        public Document Document { get; set; }
    }
}
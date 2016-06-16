using ProtoBuf;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    [ProtoContract]
    public class FileInformation
    {
        [ProtoMember(1)]
        public IFileLocation FileLocation { get; set; }
        [ProtoMember(2)]
        public uint Size { get; set; }
        [ProtoMember(3)]
        public string FileType { get; set; }
    }
}
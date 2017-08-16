using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class DocumentBase
    {
        public UInt64 Id { get; set; }
    }

    public class DocumentEmpty : DocumentBase
    {
    }

    public class Document : DocumentBase
    {
        public UInt64 AccessHash { get; set; }

        public UInt32 Date { get; set; }

        public string MimeType { get; set; }

        public UInt32 Size { get; set; }

        public PhotoSize Thumb { get; set; }

        public byte[] AdditionalData { get; set; }

        public List<DocumentAttributeBase> Attributes { get; set; }
    }
}

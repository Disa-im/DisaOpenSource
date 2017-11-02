using System;
using System.Collections.Generic;
using System.Text;

namespace Disa.Framework.Bots
{
    public abstract class InputStickerSetBase
    {
    }

    public class InputStickerSetEmpty : InputStickerSetBase
    {
    }

    public class InputStickerSetID : InputStickerSetBase
    {
        public UInt64 Id { get; set; }

        public UInt64 AccessHash { get; set; }
    }

    public class InputStickerSetShortName : InputStickerSetBase
    {
        public string ShortName { get; set; }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TelegramAppInfo.cs">
//   Copyright (c) 2013-2014 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace SharpTelegram
{
    public class TelegramAppInfo
    {
        public UInt32 ApiId { get; set; }

        public String DeviceModel { get; set; }

        public String SystemVersion { get; set; }

        public String AppVersion { get; set; }

        public String LangCode { get; set; }
    }
}

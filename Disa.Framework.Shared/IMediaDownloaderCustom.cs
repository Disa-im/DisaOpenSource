﻿using System;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    [DisaFramework]
    public interface IMediaDownloaderCustom
    {
        Task<string> Download(VisualBubble bubble, Action<int> progress);
    }
}


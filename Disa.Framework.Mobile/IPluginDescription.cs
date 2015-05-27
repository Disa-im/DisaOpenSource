using System;

namespace Disa.Framework.Mobile
{
    public interface IPluginDescription<T> where T : Service
    {
        string FetchDescription(T service);
    }
}


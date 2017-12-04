using System;
using System.Resources;
using System.Globalization;
using System.Reflection;

namespace Disa.Framework.Telegram
{
    public class Localize
    {
        private static ResourceManager _cachedResourceManager;
        private static CultureInfo _cachedCultureInfo;

        private static string Locale()
        {
            return Platform.GetCurrentLocale();
        }

        public static string GetString(string key) 
        {
            if (_cachedCultureInfo == null || _cachedResourceManager == null)
            {
                _cachedCultureInfo = new CultureInfo(Locale());
                _cachedResourceManager = new ResourceManager("Disa.Framework.Telegram.Resx.AppResources", typeof(Localize).GetTypeInfo().Assembly);
            }
            return _cachedResourceManager.GetString(key, _cachedCultureInfo);
        }
    }
}


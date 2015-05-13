using System;
using System.Linq;

namespace Disa.Framework
{
    public static class PlatformManager
    {
        public static void InitializePlatform(PlatformImplementation platform)
        {
            Platform.PlatformImplementation = platform;
        }

        public static void InitializeMain(Service[] allServices)
        {
            if (!Platform.Ready)
            {
                throw new Exception("Please initialize the platform first");
            }
            ServiceManager.Initialize(allServices.ToList());
            ServiceUserSettingsManager.LoadAll();
            BubbleGroupFactory.Initialize();
            Emoji.Initalize(Platform.GetEmojisPath());
            BubbleGroupFactory.LoadAllPartiallyIfPossible();
        }

        public static string FrameworkVersion
        {
            get
            {
                return "4";
            }
        }

        public static int FrameworkVersionInt
        {
            get
            {
                return int.Parse(FrameworkVersion);
            }
        }

        public static void Deinitialize()
        {
            // do nothing, atm
        }
    }
}
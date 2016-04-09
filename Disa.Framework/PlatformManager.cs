using System;
using System.Linq;

namespace Disa.Framework
{
    public static class PlatformManager
    {
        public static PlatformImplementation PlatformImplementation { get; private set; }

        public static readonly string[] AndroidLinkedAssemblies = new []
        {
            "AuditApp.Android.dll", "Cropper.dll", "Crouton.dll", "Disa.Android.Common.dll", "Disa.Android.dll", "Disa.Framework.dll", "Disa.Framework.Mobile.dll", 
            "Disa.Framework.Text.Android.dll", "ExifLib.dll", "FloatingActionButton.dll", "FormsViewGroup.dll", "GoogleMMSWrapper.dll", "I18N.dll", "I18N.Rare.dll", 
            "I18N.West.dll", "InflateView.dll", "LibPhoneNumber.dll", "Mono.Android.dll", "Mono.Android.Export.dll", "mscorlib.dll", "Newtonsoft.Json.dll", 
            "OkHttpCustom.dll", "PagerSlidingTabStrip.dll", "PhotoView.dll", "PicassoRoundedTransformation.dll", "protobuf-net.dll", "PushBullet.dll", "RenderScript.dll",
            "RSAx.dll", "SlidingMenu.dll", "SQLite-net.dll", "SQLitePCL.raw.dll", "SquareUp.dll", "System.Core.dll", "System.Diagnostics.Tracing.dll", "System.dll", 
            "System.Net.Http.dll", "System.Numerics.dll", "System.Reflection.Emit.dll", "System.Reflection.Emit.ILGeneration.dll", "System.Reflection.Emit.Lightweight.dll", 
            "System.Runtime.Serialization.dll", "System.Threading.Timer.dll", "System.Web.Services.dll", 
            "System.Xml.dll", "SystemBarTint.dll", "TimSort.dll", "Xamarin.Android.Support.v13.dll", "Xamarin.Android.Support.v4.dll", "Xamarin.Android.Support.v7.AppCompat.dll",
            "Xamarin.Android.Support.v7.CardView.dll", "Xamarin.Android.Support.v7.MediaRouter.dll", "Xamarin.Android.Support.v7.RecyclerView.dll", "Xamarin.Forms.Core.dll", 
            "Xamarin.Forms.Platform.Android.dll", "Xamarin.Forms.Platform.dll", "Xamarin.Forms.Xaml.dll", "Xamarin.GooglePlayServices.Analytics.dll", "Xamarin.GooglePlayServices.Base.dll", 
            "Xamarin.GooglePlayServices.Gcm.dll", "Xamarin.GooglePlayServices.Location.dll", "Xamarin.GooglePlayServices.Maps.dll", "Xamarin.Insights.dll", "zxing.monoandroid.dll", "ZXing.Net.Mobile.dll",
        };

        public static void InitializePlatform(PlatformImplementation platform)
        {
            PlatformImplementation = platform;
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
            BubbleGroupFactory.LoadAllPartiallyIfPossible();
        }

        public static string FrameworkVersion
        {
            get
            {
                return "26";
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
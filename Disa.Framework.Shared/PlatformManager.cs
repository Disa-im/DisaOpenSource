using System;
using System.Linq;

namespace Disa.Framework
{
    public static class PlatformManager
    {
        public static PlatformImplementation PlatformImplementation { get; private set; }

        public static readonly string[] AndroidLinkedAssemblies = new []
        {
            "AuditApp.Android.dll", "Axolotl.dll", "Cropper.dll", 
            "Crouton.dll", "Disa.Android.Common.dll", "Disa.Android.dll", 
            "Disa.Framework.Mobile.dll", "Disa.Framework.Text.Android.dll", 
            "Disa.Framework.dll", "Emoji.dll", "ExifLib.dll", 
            "FloatingActionButton.dll", "FormsViewGroup.dll", 
            "GoogleMMSWrapper.dll", "I18N.Rare.dll", "I18N.West.dll", 
            "I18N.dll", "InflateView.dll", "Java.Interop.dll", 
            "JpegTransparency.dll", "LibPhoneNumber.dll", "Microsoft.CSharp.dll", 
            "Mono.Android.Export.dll", "Mono.Android.dll", "Mono.CSharp.dll", 
            "Newtonsoft.Json.dll", "OkHttpCustom.dll", "Opus.dll", 
            "PagerSlidingTabStrip.dll", "PhotoView.dll", 
            "PicassoRoundedTransformation.dll", "RSAx.dll", "SQLite-net.dll", 
            "SQLitePCL.batteries.dll", "SQLitePCL.raw.dll", 
            "SQLitePCLPlugin_esqlite3.dll", "SlidingMenu.dll", "SquareUp.dll", 
            "System.Core.dll", "System.Net.Http.dll", "System.Numerics.dll", 
            "System.Reactive.Core.dll", "System.Reactive.Interfaces.dll",
            "System.Reactive.Linq.dll", "System.Runtime.Serialization.dll", 
            "System.ServiceModel.Internals.dll", "System.Web.Services.dll",
            "System.Xml.Linq.dll", "System.Xml.dll", "System.dll", 
            "Thought.vCards.dll", "TimSort.dll", "VideoEditor.dll", 
            "Xamarin.Android.Support.Animated.Vector.Drawable.dll", 
            "Xamarin.Android.Support.Design.dll", 
            "Xamarin.Android.Support.Vector.Drawable.dll",
            "Xamarin.Android.Support.v13.dll", "Xamarin.Android.Support.v4.dll",
            "Xamarin.Android.Support.v7.AppCompat.dll", 
            "Xamarin.Android.Support.v7.CardView.dll", 
            "Xamarin.Android.Support.v7.MediaRouter.dll",
            "Xamarin.Android.Support.v7.RecyclerView.dll", 
            "Xamarin.Forms.Core.dll", "Xamarin.Forms.Platform.Android.dll", 
            "Xamarin.Forms.Platform.dll", "Xamarin.Forms.Xaml.dll", 
            "Xamarin.GooglePlayServices.Analytics.dll", 
            "Xamarin.GooglePlayServices.Base.dll", 
            "Xamarin.GooglePlayServices.Basement.dll", 
            "Xamarin.GooglePlayServices.Gcm.dll", 
            "Xamarin.GooglePlayServices.Location.dll",
            "Xamarin.GooglePlayServices.Maps.dll", 
            "Xamarin.GooglePlayServices.Measurement.dll", 
            "Xamarin.Insights.dll", "ZXing.Net.Mobile.dll", "mscorlib.dll", 
            "protobuf-net.dll", "zxing.monoandroid.dll"
        };

        public static void PreInitialize(PlatformImplementation platform, 
                                              AxolotlImplementation axolotl)
        {
            PlatformImplementation = platform;
            Platform.PlatformImplementation = platform;
            Axolotl.AxolotlImplementation = axolotl;
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
                return "44";
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

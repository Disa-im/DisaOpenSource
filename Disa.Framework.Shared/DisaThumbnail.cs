using System;
using System.IO;
using System.Text;
using ProtoBuf;

namespace Disa.Framework
{
    [Serializable]
    [ProtoContract]
    public class DisaThumbnail
    {
        [ProtoMember(1)]
        private string _location { get; set; }

        [ProtoMember(2)]
        private string _name { get; set; }

        [ProtoMember(3)]
        public bool IsUrl { get; private set; }

        public bool Failed { get; set; }

        public DisaThumbnail(Service service, byte[] bytes, string name)
        {
            _name = name;
            _location = Path.Combine(GetThumbnailCachePath(),
                service.Information.ServiceName + "^" + Convert.ToBase64String(Encoding.UTF8.GetBytes(name)).Replace("/", "_") + ".cache");
            try
            {
                File.WriteAllBytes(_location, bytes);
            }
            catch
            {
                _name = null;
                _location = null;
            }
            IsUrl = false;
        }

        public DisaThumbnail(string url)
        {
            _name = url;
            _location = url;
            IsUrl = true;
        }

        public DisaThumbnail()
        {
            Failed = true;
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string Location
        {
            get
            {
                return _location;
            }
        }

        private static string _cachedThumbnailCachePath;
        private static string GetThumbnailCachePath()
        {
            if (_cachedThumbnailCachePath != null)
                return _cachedThumbnailCachePath;

            if (!Platform.Ready)
            {
                #if __ANDROID__
                Utils.DebugPrint("WARRRNINGGGG: Platform not initilized. Storing on public storage.");
                var state = Android.OS.Environment.ExternalStorageState;
                if (state != Android.OS.Environment.MediaMounted)
                    return null;
                var file = Android.OS.Environment.ExternalStorageDirectory.Path;
                var disaLocationPath = Path.Combine(file, Path.Combine("disa", "thumbnailcache"));
                if (!Directory.Exists(disaLocationPath))
                {
                    Directory.CreateDirectory(disaLocationPath);
                }
                _cachedThumbnailCachePath = disaLocationPath;
                #else
                var tempPath = System.IO.Path.GetTempPath();
                var path = Path.Combine(tempPath, Path.Combine("disa", "thumbnailcache"));
                _cachedThumbnailCachePath = path;
                #endif
                //TODO: other platforms?
            }
            else
            {
                var disaLocationPath = Path.Combine(Platform.GetDatabasePath(), "thumbnailcache");
                if (!Directory.Exists(disaLocationPath))
                {
                    Directory.CreateDirectory(disaLocationPath);
                }
                _cachedThumbnailCachePath = disaLocationPath;
            }

            return _cachedThumbnailCachePath;
        }
    }
}
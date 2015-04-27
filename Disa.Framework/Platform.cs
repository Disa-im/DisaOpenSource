using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public static class Platform
    {
        static internal PlatformImplementation PlatformImplementation { private get; set; }

        public static bool Ready
        {
            get
            {
                return PlatformImplementation != null;
            }
        }

        public static byte[] GetIcon(IconType iconType)
        {
            return PlatformImplementation.GetIcon(iconType);
        }

        public static string GetCurrentLocale()
        {
            return PlatformImplementation.GetCurrentLocale();
        }

        public static string GetFilesPath()
        {
            return PlatformImplementation.GetFilesPath();
        }

        public static string GetPicturesPath()
        {
            return PlatformImplementation.GetPicturesPath();
        }

        public static string GetVideosPath()
        {
            return PlatformImplementation.GetVideosPath();
        }

        public static string GetAudioPath()
        {
            return PlatformImplementation.GetAudioPath();
        }

        public static string GetLogsPath()
        {
            return PlatformImplementation.GetLogsPath();
        }

        public static string GetSettingsPath()
        {
            return PlatformImplementation.GetSettingsPath();
        }

        public static string GetDatabasePath()
        {
            return PlatformImplementation.GetDatabasePath();
        }

        public static string GetEmojisPath()
        {
            return PlatformImplementation.GetEmojisPath();
        }

        public static string GetDeviceId(int minimumLength = 5)
        {
            return PlatformImplementation.GetDeviceId(minimumLength);
        }

        public static List<PhoneBookContact> GetPhoneBookContacts()
        {
            return PlatformImplementation.GetPhoneBookContacts();
        }

        public static void ScheduleAction(WakeLockBalancer.WakeLock wakeLock)
        {
            PlatformImplementation.ScheduleAction(wakeLock);
        }

        public static void RemoveAction(WakeLockBalancer.WakeLock wakeLock)
        {
            PlatformImplementation.RemoveAction(wakeLock);
        }

        public static void ScheduleAction(int interval, WakeLockBalancer.ActionObject execute)
        {
            PlatformImplementation.ScheduleAction(interval, execute);
        }

        public static WakeLock AquireWakeLock(string name)
        {
            return PlatformImplementation.AquireWakeLock(name);
        }

        public static void OpenContact(string phoneNumber)
        {
            PlatformImplementation.OpenContact(phoneNumber);
        }

        public static void DialContact(string phoneNumber)
        {
            PlatformImplementation.DialContact(phoneNumber);
        }

        public static void LaunchViewIntent(string url)
        {
            PlatformImplementation.LaunchViewIntent(url);
        }

        public static bool DeviceHasApp(string appName)
        {
            return PlatformImplementation.DeviceHasApp(appName);
        }

        public static bool HasInternetConnection()
        {
            return PlatformImplementation.HasInternetConnection();
        }

        public static string GetMimeTypeFromPath(string path)
        {
            return PlatformImplementation.GetMimeTypeFromPath(path);
        }

        public static byte[] GenerateJpegBytes(byte[] bytes, int toWidth, int toHeight, int quality = 100)
        {
            return PlatformImplementation.GenerateJpegBytes(bytes, toWidth, toHeight, quality);
        }

        public static byte[] GenerateVideoThumbnail(string videoPath)
        {
            return PlatformImplementation.GenerateVideoThumbnail(videoPath);
        }

        public static Task<byte[]> GenerateLocationThumbnail(double longitude, double latitude)
        {
            return PlatformImplementation.GenerateLocationThumbnail(longitude, latitude);
        }

        public static void CreatePartyBitmap(Service service, string name, IPartyThumbnail thumbnail,
            List<Tuple<string, string>> participants, Action<DisaThumbnail> result)
        {
            PlatformImplementation.CreatePartyBitmap(service, name, thumbnail, participants, result);
        }

        public static DisaThumbnail CreatePartyBitmap(Service service, string name,
            List<Tuple<string, string>> participants)
        {
            return PlatformImplementation.CreatePartyBitmap(service, name, participants);
        }

        public static DisaThumbnail CreatePartyBitmap(Service service, string name,
            List<Tuple<DisaThumbnail, string>> participants)
        {
            return PlatformImplementation.CreatePartyBitmap(service, name, participants);
        }

        public static DisaThumbnail CreatePartyBitmap(Service service, string name,
            DisaThumbnail big, string bigName,
            DisaThumbnail small1, string small1Name,
            DisaThumbnail small2, string small2Name)
        {
            return PlatformImplementation.CreatePartyBitmap(service, name, big, bigName, small1, small1Name, small2,
                small2Name);
        }
    }
}
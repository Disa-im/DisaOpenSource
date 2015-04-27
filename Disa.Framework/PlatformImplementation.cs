using System.Collections.Generic;
using Disa.Framework.Bubbles;
using System;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public abstract class PlatformImplementation
    {
        public abstract byte[] GetIcon(IconType iconType);

        public abstract string GetCurrentLocale();

        public abstract string GetFilesPath();

        public abstract string GetPicturesPath();

        public abstract string GetVideosPath();

        public abstract string GetAudioPath();

        public abstract string GetLogsPath();

        public abstract string GetSettingsPath();

        public abstract string GetDatabasePath();

        public abstract string GetEmojisPath();

        public abstract string GetDeviceId(int minimumLength = 5);

        public abstract List<PhoneBookContact> GetPhoneBookContacts();

        public abstract void ScheduleAction(WakeLockBalancer.WakeLock wakeLock);

        public abstract void RemoveAction(WakeLockBalancer.WakeLock wakeLock);

        public abstract void ScheduleAction(int interval, WakeLockBalancer.ActionObject execute);

        public abstract WakeLock AquireWakeLock(string name);

        public abstract void OpenContact(string phoneNumber);

        public abstract void DialContact(string phoneNumber);

        public abstract void LaunchViewIntent(string url);

        public abstract bool DeviceHasApp(string appName);

        public abstract bool HasInternetConnection();

        public abstract string GetMimeTypeFromPath(string path);

        public abstract byte[] GenerateJpegBytes(byte[] bytes, int toWidth, int toHeight, int quality = 100);

        public abstract byte[] GenerateVideoThumbnail(string videoPath);

        public abstract Task<byte[]> GenerateLocationThumbnail(double longitude, double latitude);

        public abstract void CreatePartyBitmap(Service service, string name, IPartyThumbnail thumbnail, 
            List<Tuple<string, string>> participants, Action<DisaThumbnail> result);

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<string, string>> participants); //thumbnail_location, name

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<DisaThumbnail, string>> participants);

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, 
            DisaThumbnail big, string bigName,
            DisaThumbnail small1, string small1Name,
            DisaThumbnail small2, string small2Name);
    }
}
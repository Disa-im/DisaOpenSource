using System.Collections.Generic;
using Disa.Framework.Bubbles;
using System;
using System.Threading.Tasks;
using System.IO;

namespace Disa.Framework
{
    public abstract class PlatformImplementation
    {
        public abstract void MarkTemporaryFileForDeletion(string path);

        public abstract void UnmarkTemporaryFileForDeletion(string path, bool purge);

        public abstract byte[] GetIcon(IconType iconType);

        public abstract string GetCurrentLocale();

        public abstract string GetStickersPath();

        public abstract string GetFilesPath();

        public abstract string GetPicturesPath();

        public abstract string GetVideosPath();

        public abstract string GetAudioPath();

        public abstract string GetLogsPath();

        public abstract string GetSettingsPath();

        public abstract string GetDatabasePath();

        public abstract string GetDeviceId(int minimumLength = 5);

        public abstract List<PhoneBookContact> GetPhoneBookContacts();

        public abstract void ScheduleAction(WakeLockBalancer.WakeLock wakeLock);

        public abstract void RemoveAction(WakeLockBalancer.WakeLock wakeLock);

        public abstract void ScheduleAction(int interval, WakeLockBalancer.ActionObject execute);

        public abstract WakeLock AquireWakeLock(string name);

		public abstract Stream GetConversationExportAssetsArchiveStream();

		public abstract void OpenContact(string phoneNumber);

        public abstract void DialContact(string phoneNumber);

        public abstract void LaunchViewIntent(string url);

        public abstract bool DeviceHasApp(string appName);

        public abstract bool HasInternetConnection();

        public abstract bool ShouldAttemptInternetConnection();

        public abstract string GetMimeTypeFromPath(string path);

        public abstract string GetExtensionFromMimeType(string mimeType);

        public abstract byte[] GenerateJpegBytes(byte[] bytes, int toWidth, int toHeight, int quality = 100);

        public abstract byte[] GenerateVideoThumbnail(string videoPath);

        public abstract byte[] GenerateBytesFromContactCard(ContactCard contactCard);

        public abstract ContactCard GenerateContactCardFromBytes(byte[] contactCardBytes);

        public abstract Task<byte[]> GenerateLocationThumbnail(double longitude, double latitude);

        public abstract void CreatePartyBitmap(Service service, string name, IPartyThumbnail thumbnail, 
            List<Tuple<string, string>> participants, Action<DisaThumbnail> result);

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<string, string>> participants); //thumbnail_location, name

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<DisaThumbnail, string>> participants);

        public abstract DisaThumbnail CreatePartyBitmap(Service service, string name, 
            DisaThumbnail big, string bigName,
            DisaThumbnail small1, string small1Name,
            DisaThumbnail small2, string small2Name);

        public abstract BubbleGroup GetCurrentBubbleGroupOnUI();

        public abstract bool SwitchCurrentBubbleGroupOnUI(BubbleGroup group);

        public abstract void DeleteBubbleGroup(BubbleGroup[] bubbleGroups);

        public abstract void ExecuteAllOldWakeLocksAndAllGracefulWakeLocksImmediately();

		public abstract void ShareContent(string mimeType, string uri);

        public abstract void OpenApp(string appName);

        public abstract void OpenAppSettings(string appName);

        public abstract string GetDeviceRegistrationId();
    }
}

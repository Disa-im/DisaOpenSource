using System;
using Disa.Framework;
using System.IO;
using System.Threading.Tasks;
using MimeSharp;
using System.Collections.Generic;
using System.Timers;

namespace Disa.Terminal
{
    public class WindowsUnix : PlatformImplementation
    {
        public override byte[] GetIcon(IconType iconType)
        {
            //TODO:
            throw new NotImplementedException();
        }

        public override string GetCurrentLocale()
        {
            return "en-US"; //TODO
        }

        private string GetDisaPath(string directory)
        {
            var @base = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Disa");
            if (!Directory.Exists(@base))
            {
                Directory.CreateDirectory(@base);
            }
            var @complete = Path.Combine(@base, directory);
            if (!Directory.Exists(@complete))
            {
                Directory.CreateDirectory(@complete);
            }
            return @complete;
        }

        public override string GetFilesPath()
        {
            return GetDisaPath("Files");
        }

        public override string GetPicturesPath()
        {
            return GetDisaPath("Pictures");
        }

        public override string GetVideosPath()
        {
            return GetDisaPath("Videos");
        }

        public override string GetAudioPath()
        {
            return GetDisaPath("Audio");
        }

        public override string GetLogsPath()
        {
            return GetDisaPath("Logs");
        }

        public override string GetSettingsPath()
        {
            return GetDisaPath("Settings");
        }

        public override string GetDatabasePath()
        {
            return GetDisaPath("Database");
        }

        public override string GetEmojisPath()
        {
            return GetDisaPath("Emojis");
        }

        public override string GetDeviceId(int minimumLength = 5)
        {
            //TODO
            return Guid.NewGuid().ToString();
        }

        public override List<PhoneBookContact> GetPhoneBookContacts()
        {
            //TODO:
            return new List<PhoneBookContact>();
        }

        private Dictionary<WakeLockBalancer.WakeLock, Timer> _scheduledWakeLocks = new Dictionary<WakeLockBalancer.WakeLock, Timer>();

        public override void ScheduleAction(WakeLockBalancer.WakeLock wakeLock)
        {
            RemoveAction(wakeLock);
            var timer = new Timer(wakeLock.Interval * 1000.0);
            timer.Elapsed += (sender, e) =>
            {
                    if (wakeLock.Action.Execute != WakeLockBalancer.ActionObject.ExecuteType.TaskWithWakeLock)
                    {
                        wakeLock.Action.Action();
                    }
                    else
                    {
                        Task.Factory.StartNew(() =>
                            {
                                wakeLock.Action.Action();
                            });
                    }
                    if (!wakeLock.Reoccurring)
                    {
                        timer.Enabled = false;
                        timer.Dispose();
                    }
            };
            _scheduledWakeLocks.Add(wakeLock, timer);
            timer.Enabled = true;
        }

        public override void RemoveAction(WakeLockBalancer.WakeLock wakeLock)
        {
            if (_scheduledWakeLocks.ContainsKey(wakeLock))
            {
                var timer = _scheduledWakeLocks[wakeLock];
                timer.Enabled = false;
                timer.Dispose();
                _scheduledWakeLocks.Remove(wakeLock);
            }
        }

        public override void ScheduleAction(int interval, WakeLockBalancer.ActionObject execute)
        {
            ScheduleAction(new WakeLockBalancer.CruelWakeLock(execute, interval, 0, false));
        }

        private class DumbWakeLock : WakeLock
        {
            private readonly string _name;

            public DumbWakeLock(string name)
            {
                TemporaryAcquire();
                _name = name;
            }

            public override void Dispose()
            {
                TemporaryRelease();
            }

            public override void TemporaryAcquire()
            {
                Utils.DebugPrint(">>>>>>> Aquiring wake lock. " + _name);
            }

            public override void TemporaryRelease()
            {
                Utils.DebugPrint(">>>>>>> Releasing wake lock. " + _name);
            }
        }

        public override WakeLock AquireWakeLock(string name)
        {
            return new DumbWakeLock(name);
        }

        public override void OpenContact(string phoneNumber)
        {
            throw new NotImplementedException();
        }

        public override void DialContact(string phoneNumber)
        {
            throw new NotImplementedException();
        }

        public override void LaunchViewIntent(string url)
        {
            throw new NotImplementedException();
        }

        public override bool DeviceHasApp(string appName)
        {
            return false;
        }

        public override bool HasInternetConnection()
        {
            return true;
        }

        private Mime _mime;
        public override string GetMimeTypeFromPath(string path)
        {
            if (_mime == null)
            {
                _mime = new Mime();
            }
            return _mime.Lookup(path);
        }

        public override byte[] GenerateJpegBytes(byte[] bytes, int toWidth, int toHeight, int quality = 100)
        {
            throw new NotImplementedException();
        }

        public override byte[] GenerateVideoThumbnail(string videoPath)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> GenerateLocationThumbnail(double longitude, double latitude)
        {
            throw new NotImplementedException();
        }

        public override void CreatePartyBitmap(Service service, string name, IPartyThumbnail thumbnail, List<Tuple<string, string>> participants, Action<DisaThumbnail> result)
        {
            throw new NotImplementedException();
        }

        public override DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<string, string>> participants)
        {
            throw new NotImplementedException();
        }

        public override DisaThumbnail CreatePartyBitmap(Service service, string name, List<Tuple<DisaThumbnail, string>> participants)
        {
            throw new NotImplementedException();
        }

        public override DisaThumbnail CreatePartyBitmap(Service service, string name, DisaThumbnail big, string bigName, DisaThumbnail small1, string small1Name, DisaThumbnail small2, string small2Name)
        {
            throw new NotImplementedException();
        }
    }
}


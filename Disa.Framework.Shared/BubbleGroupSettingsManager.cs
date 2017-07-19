using System;
using System.IO;
using System.Xml.Serialization;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace Disa.Framework
{
    public static class BubbleGroupSettingsManager
    {
        private static readonly object _lock = new object();

        private static string _pathCached;
        private static string GetPath()
        {
            if (_pathCached != null)
            {
                return _pathCached;
            }

            var databasePath = Platform.GetDatabasePath();
            if (!Directory.Exists(databasePath))
            {
                Utils.DebugPrint("Creating database directory.");
                Directory.CreateDirectory(databasePath);
            }

            var bubbleGroupsSettingsPath = Path.Combine(databasePath, "bubblegroupssettings.db");

            _pathCached = bubbleGroupsSettingsPath;
            return bubbleGroupsSettingsPath;
        }

        public static void Load()
        {
            lock (_lock)
            {
                using (var db = new SqlDatabase<BubbleGroupSettings>(GetPath()))
                {
                    var toRemoves = new List<BubbleGroupSettings>();
                    foreach (var settings in db.Store.ToList())
                    {
                        var bubbleGroup = BubbleGroupManager.Find(settings.Guid);
                        if (bubbleGroup == null)
                        {
                            toRemoves.Add(settings);
                        }
                        else
                        {
                            bubbleGroup.Settings = settings;
                        }
                    }
                    if (toRemoves.Any())
                    {
                        foreach (var toRemove in toRemoves)
                        {
                            db.Remove(toRemove);
                        }
                    }
                }
            }
        }

        private static void Update(BubbleGroupSettings settings)
        {
            lock (_lock)
            {
                using (var db = new SqlDatabase<BubbleGroupSettings>(GetPath()))
                {
                    db.Update(settings);
                }
            }
        }

        private static void InsertDefaultIfNull(BubbleGroup group)
        {
            lock (_lock)
            {
                if (group.Settings == null)
                {
                    using (var db = new SqlDatabase<BubbleGroupSettings>(GetPath()))
                    {
                        var settings = new BubbleGroupSettings
                        {
							Guid = group.ID,
                            Mute = false,
							NotificationLed = DefaultNotificationLedColor,
							VibrateOption = null,
							VibrateOptionCustomPattern = null,
							Ringtone = null,
                            Unread = true,
                            UnreadOffline = true,
                            UnreadIndicatorGuid = null,
							LastUnreadSetTime = 0,
							ParticipantNicknames = null,
							RingtoneDisabled = false,
							VibrateOptionDisabled = false,
                            SentBubbleColor = 0,
                            ReceivedBubbleColor = 0,
                            SentFontColor = 0,
                            ReceivedFontColor = 0,
                            BubbleColorsChosen = false,
                            BackgroundChosen = false,
                            BackgroundColor = 0,
                            BackgroundImagePath = null,
                            ReadTimes = null,
                            QuotedTitles = null,
                        };
                        db.Add(settings);
                        group.Settings = settings;
                    }
                }
            }
        }

        public static int DefaultNotificationLedColor
        {
            get
            {
                return 0xffffff;
            }
        }

        public static void SetLastUnreadSetTime(BubbleGroup group, long lastUnreadSetTime)
        {
            InsertDefaultIfNull(group);
            group.Settings.LastUnreadSetTime = lastUnreadSetTime;
            Update(group.Settings);
        }

        public static void SetUnread(BubbleGroup group, bool unread)
        {
            InsertDefaultIfNull(group);
            if (group.Settings.Unread != unread)
            {
                group.Settings.Unread = unread;
                Update(group.Settings);
            }
            SetUnreadOffline(group, unread);
        }

        internal static void SetUnreadOffline(BubbleGroup group, bool unread)
        {
            InsertDefaultIfNull(group);
            if (group.Settings.UnreadOffline != unread)
            {
                group.Settings.UnreadOffline = unread;
                Update(group.Settings);
            }
        }

        public static void SetUnreadIndicatorGuid(BubbleGroup group, string guid, bool isNew)
        {
            InsertDefaultIfNull(group);
            var toSet = guid + "|" + (isNew ? "true" : "false");
            if (group.Settings.UnreadIndicatorGuid != toSet)
            {
                group.Settings.UnreadIndicatorGuid = toSet;
                Update(group.Settings);
            }
        }

        public static void SetMute(BubbleGroup group, bool mute)
        {
            InsertDefaultIfNull(group);
            group.Settings.Mute = mute;
            Update(group.Settings);
        }

        public static void SetNotificationLed(BubbleGroup group, int notificationLed)
        {
            InsertDefaultIfNull(group);
            group.Settings.NotificationLed = notificationLed;
            Update(group.Settings);
        }

        public static void SetVibrateOption(BubbleGroup group, string vibrateOption)
        {
            InsertDefaultIfNull(group);
            group.Settings.VibrateOption = vibrateOption;
            Update(group.Settings);
        }

        public static void SetVibrateOptionDisabled(BubbleGroup group, bool disabled)
        {
            InsertDefaultIfNull(group);
            group.Settings.VibrateOptionDisabled = disabled;
            Update(group.Settings);
        }

        public static void SetRingtoneDisabled(BubbleGroup group, bool disabled)
        {
            InsertDefaultIfNull(group);
            group.Settings.RingtoneDisabled = disabled;
            Update(group.Settings);
        }

        public static void SetVibrateOptionCustomPattern(BubbleGroup group, string vibrateOptionCustomPattern)
        {
            InsertDefaultIfNull(group);
            group.Settings.VibrateOptionCustomPattern = vibrateOptionCustomPattern;
            Update(group.Settings);
        }

        public static void SetRingtone(BubbleGroup group, string ringtone)
        {
            InsertDefaultIfNull(group);
            group.Settings.Ringtone = ringtone;
            Update(group.Settings);
        }

        public static void SetSentBubbleColor(BubbleGroup group, int sentBubbleColor)
        {
            InsertDefaultIfNull(group);
            group.Settings.SentBubbleColor = sentBubbleColor;
            Update(group.Settings);
        }

        public static void SetReceivedBubbleColor(BubbleGroup group, int receivedBubbleColor)
        {
            InsertDefaultIfNull(group);
            group.Settings.ReceivedBubbleColor = receivedBubbleColor;
            Update(group.Settings);
        }

        public static void SetSentFontColor(BubbleGroup group, int sentFontColor)
        {
            InsertDefaultIfNull(group);
            group.Settings.SentFontColor = sentFontColor;
            Update(group.Settings);
        }

        public static void SetReceivedFontColor(BubbleGroup group, int receivedFontColor)
        {
            InsertDefaultIfNull(group);
            group.Settings.ReceivedFontColor = receivedFontColor;
            Update(group.Settings);
        }

        public static void SetBubbleColorsChosen(BubbleGroup group, bool bubbleColorsChosen)
        {
            InsertDefaultIfNull(group);
            group.Settings.BubbleColorsChosen = bubbleColorsChosen;
            Update(group.Settings);
        }

        public static int GetSentBubbleColor(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.SentBubbleColor;
        }

        public static int GetReceivedBubbleColor(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.ReceivedBubbleColor;
        }

        public static int GetSentFontColor(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.SentFontColor;
        }

        public static int GetReceivedFontColor(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.ReceivedFontColor;
        }

        public static bool GetBubbleColorsChosen(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.BubbleColorsChosen;
        }

        public static void SetBackgroundChosen(BubbleGroup group, bool backgroundChosen)
        {
            InsertDefaultIfNull(group);
            group.Settings.BackgroundChosen = backgroundChosen;
            Update(group.Settings);
        }

        public static void SetBackgroundColor(BubbleGroup group, int backgroundColor)
        {
            InsertDefaultIfNull(group);
            group.Settings.BackgroundColor = backgroundColor;
            Update(group.Settings);
        }

        public static void SetBackgroundImagePath(BubbleGroup group, string backgroundImagePath)
        {
            InsertDefaultIfNull(group);
            group.Settings.BackgroundImagePath = backgroundImagePath;
            Update(group.Settings);
        }

        public static bool GetBackgroundChosen(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.BackgroundChosen;
        }

        public static int GetBackgroundColor(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.BackgroundColor;
        }

        public static string GetBackgroundImagePath(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.BackgroundImagePath;
        }

        public static bool GetMute(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.Mute;
        }

        public static bool GetRingtoneDisabled(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.RingtoneDisabled;
        }

        public static bool GetVibrateOptionDisabled(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.VibrateOptionDisabled;
        }

        public static int GetNotificationLed(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.NotificationLed;
        }

        public static string GetVibrateOption(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.VibrateOption;
        }

        public static string GetVibrateOptionCustomPattern(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.VibrateOptionCustomPattern;
        }

        public static string GetRingtone(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.Ringtone;
        }

        public static bool GetUnread(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.Unread;
        }

        public static long GetLastUnreadSetTime(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.LastUnreadSetTime;
        }

        public static bool GetUnreadOffline(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.UnreadOffline;
        }

        public static string GetUnreadIndicatorGuid(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            var guid = group.Settings.UnreadIndicatorGuid;
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var indexOf = guid.IndexOf('|');
                if (indexOf > -1)
                {
                    var editedGuid = guid.Substring(0, indexOf);
                    if (string.IsNullOrWhiteSpace(editedGuid))
                    {
                        return null;
                    }
                    return editedGuid;
                }
            }
            else
            {
                return null;
            }
            return guid;
        }

        public static bool GetUnreadIndicatorGuidIsNew(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            var guid = group.Settings.UnreadIndicatorGuid;
            if (!string.IsNullOrWhiteSpace(guid))
            {
                var indexOf = guid.IndexOf('|');
                if (indexOf > -1)
                {
                    indexOf += 1;
                    var value = guid.Substring(indexOf, guid.Length - indexOf);
                    return value == "true";
                }
            }
            return false;
        }

        internal static DisaParticipantNickname[] GetParticipantNicknames(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            if (group.Settings.ParticipantNicknamesCachedSet)
            {
                return group.Settings.ParticipantNicknamesCached;
            }
            else
            {
                if (group.Settings.ParticipantNicknames == null)
                {
                    group.Settings.ParticipantNicknamesCachedSet = true;
                }
                else
                {
                    using (var ms = new MemoryStream(group.Settings.ParticipantNicknames))
                    {
                        var participantNicknames = Serializer.Deserialize<DisaParticipantNickname[]>(ms);
                        group.Settings.ParticipantNicknamesCached = participantNicknames;
                        group.Settings.ParticipantNicknamesCachedSet = true;
                    }
                }
                return group.Settings.ParticipantNicknamesCached;
            }
        }

        internal static DisaReadTime[] GetReadTimes(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            if (group.Settings.ReadTimesCachedSet)
            {
                return group.Settings.ReadTimesCached;
            }
            else
            {
                if (group.Settings.ReadTimes == null)
                {
                    group.Settings.ReadTimesCachedSet = true;
                }
                else
                {
                    using (var ms = new MemoryStream(group.Settings.ReadTimes))
                    {
                        var readTimes = Serializer.Deserialize<DisaReadTime[]>(ms);
                        group.Settings.ReadTimesCached = readTimes;
                        group.Settings.ReadTimesCachedSet = true;
                    }
                }
                return group.Settings.ReadTimesCached;
            }
        }

        internal static DisaQuotedTitle[] GetQuotedTitles(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            if (group.Settings.QuotedTitlesCachedSet)
            {
                return group.Settings.QuotedTitlesCached;
            }
            else
            {
                if (group.Settings.QuotedTitles == null)
                {
                    group.Settings.QuotedTitlesCachedSet = true;
                }
                else
                {
                    using (var ms = new MemoryStream(group.Settings.QuotedTitles))
                    {
                        var quotedTitles = Serializer.Deserialize<DisaQuotedTitle[]>(ms);
                        group.Settings.QuotedTitlesCached = quotedTitles;
                        group.Settings.QuotedTitlesCachedSet = true;
                    }
                }
                return group.Settings.QuotedTitlesCached;
            }
        }

        internal static void SetQuotedTitles(BubbleGroup group, DisaQuotedTitle[] quotedTitles)
        {
            InsertDefaultIfNull(group);
            group.Settings.QuotedTitlesCached = quotedTitles;
            if (quotedTitles != null)
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, quotedTitles);
                    group.Settings.QuotedTitles = ms.ToArray();
                }
            }
            else
            {
                group.Settings.QuotedTitles = null;
            }
            Update(group.Settings);
        }

        internal static void SetParticipantNicknames(BubbleGroup group, DisaParticipantNickname[] nicknames)
        {
            InsertDefaultIfNull(group);
            group.Settings.ParticipantNicknamesCached = nicknames;
            if (nicknames != null)
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, nicknames);
                    group.Settings.ParticipantNicknames = ms.ToArray();
                }
            }
            else
            {
                group.Settings.ParticipantNicknames = null;
            }
            Update(group.Settings);
        }

        internal static void SetReadTimes(BubbleGroup group, DisaReadTime[] readTimes)
        {
            InsertDefaultIfNull(group);
            group.Settings.ReadTimesCached = readTimes;
            if (readTimes != null)
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, readTimes);
                    group.Settings.ReadTimes = ms.ToArray();
                }
            }
            else
            {
                group.Settings.ReadTimes = null;
            }
            Update(group.Settings);
        }
    }
}

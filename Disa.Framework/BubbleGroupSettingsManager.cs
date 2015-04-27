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
                            Mute = false,
                            Unread = true,
                            Guid = group.ID,
                            LastUnreadSetTime = 0,
                            ReadTimes = null,
                        };
                        db.Add(settings);
                        group.Settings = settings;
                    }
                }
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
            group.Settings.Unread = unread;
            Update(group.Settings);
        }

        public static void SetMute(BubbleGroup group, bool mute)
        {
            InsertDefaultIfNull(group);
            group.Settings.Mute = mute;
            Update(group.Settings);
        }

        public static bool GetMute(BubbleGroup group)
        {
            InsertDefaultIfNull(group);
            return group.Settings.Mute;
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
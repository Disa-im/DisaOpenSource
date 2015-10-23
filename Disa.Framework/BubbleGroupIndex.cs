using System;
using SQLite;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ProtoBuf;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public class BubbleGroupIndex
    {
        private class Entry
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Service { get; set; }
            public string Guid { get; set; }
            public byte[] LastBubble { get; set; }
            public string LastBubbleGuid { get; set; }
            public bool Unified { get; set; }
            public string UnifiedBubbleGroupsGuids { get; set; }
            public string UnifiedPrimaryBubbleGroupGuid { get; set; }
            public string UnifiedSendingBubbleGroupGuid { get; set; }
            public long LastModifiedIndex { get; set; }
        }

        private const string FileName = "BubbleGroupIndex.db";

        private static readonly object _migrationLock = new object();

        private static string Location
        {
            get
            {
                var databasePath = Platform.GetDatabasePath();
                var queuedBubblesLocation = Path.Combine(databasePath, FileName);

                return queuedBubblesLocation;
            }
        }

        private static void Save(Entry[] entries)
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var entry in entries)
                {
                    db.Add(entry);
                }
            }
        }

        internal static long? GetLastModifiedIndex(string bubbleGroupId)
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var item in db.Store.Where(x => !x.Unified && x.Guid == bubbleGroupId))
                {
                    if (item.LastModifiedIndex <= 0)
                    {
                        return null;
                    }
                    return item.LastModifiedIndex;
                }
            }
            return null;
        }

        internal static void UpdateLastBubbleOrAndLastModifiedIndex(string bubbleGroupId, VisualBubble lastBubble = null, long lastModifiedIndex = -1)
        {
            if (lastBubble == null && lastModifiedIndex == -1)
                return;
            
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var item in db.Store.Where(x => !x.Unified && x.Guid == bubbleGroupId))
                {
                    if (lastBubble != null)
                    {
                        item.LastBubble = SerializeBubble(lastBubble);
                        item.LastBubbleGuid = lastBubble.ID;
                    }
                    if (lastModifiedIndex != -1)
                    {
                        item.LastModifiedIndex = lastModifiedIndex;
                    }
                    db.Update(item);
                }
            }
        }

        internal static void SetUnifiedSendingBubbleGroup(string unifiedGroupId, string sendingGroupId)
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var item in db.Store.Where(x => x.Unified && x.Guid == unifiedGroupId))
                {
                    item.UnifiedSendingBubbleGroupGuid = sendingGroupId;
                    db.Update(item);
                }
            }
        }

        internal static void RemoveUnified(string unifiedGroupId)
        {
            RemoveUnified(new [] { unifiedGroupId });
        }

        internal static void RemoveUnified(string[] unifiedGroupIds)
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var unifiedGroupId in unifiedGroupIds)
                {
                    foreach (var item in db.Store.Where(x => x.Unified && x.Guid == unifiedGroupId))
                    {
                        db.Remove(item);
                    }
                }
            }
        }

        internal static void AddUnified(UnifiedBubbleGroup unifiedGroup)
        {
            RemoveUnified(unifiedGroup.ID);
            using (var db = new SqlDatabase<Entry>(Location))
            {
                db.Add(new Entry
                {
                    Guid = unifiedGroup.ID,
                    Unified = true,
                    UnifiedBubbleGroupsGuids = unifiedGroup.Groups.Select(x => x.ID).
                        Aggregate((current, next) => current + "," + next),
                    UnifiedPrimaryBubbleGroupGuid = unifiedGroup.PrimaryGroup.ID,
                    UnifiedSendingBubbleGroupGuid = unifiedGroup.SendingGroup.ID,
                });
            }
        }

        internal static void Add(BubbleGroup group)
        {
            if (group is UnifiedBubbleGroup)
                return;
            Remove(group.ID);
            using (var db = new SqlDatabase<Entry>(Location))
            {
                var lastBubble = group.LastBubbleSafe();
                var serviceName = group.Service.Information.ServiceName;
                db.Add(new Entry
                {
                    Guid = group.ID,
                    Service = serviceName,
                    LastBubble = SerializeBubble(lastBubble),
                    LastBubbleGuid = lastBubble.ID
                });
            }
        }

        internal static void Remove(string[] groupIds)
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                foreach (var groupId in groupIds)
                {
                    foreach (var item in db.Store.Where(x => !x.Unified && x.Guid == groupId))
                    {
                        db.Remove(item);
                    }
                }
            }
        }

        internal static void Remove(string groupId)
        {
            Remove(new [] { groupId });
        }

        private static List<Entry> GetAllEntries()
        {
            using (var db = new SqlDatabase<Entry>(Location))
            {
                return db.Store.ToList();
            }
        }

        private static void LoadInternal()
        {
            var entries = GetAllEntries();

            foreach (var entry in entries)
            {
                if (entry.Unified)
                {
                    continue;
                }
                    
                var service = ServiceManager.GetByName(entry.Service);
                if (service != null)
                {
                    var mostRecentVisualBubble = DeserializeBubble(entry.LastBubble);
                    mostRecentVisualBubble.Service = service;
                    mostRecentVisualBubble.ID = entry.LastBubbleGuid;

                    var bubbleGroup = new BubbleGroup(mostRecentVisualBubble, entry.Guid, true);

                    BubbleGroupManager.BubbleGroupsAdd(bubbleGroup, true);
                }
                else
                {
                    Utils.DebugPrint("Could not obtain a valid service object from " + entry.Id + " (" + entry.Service + ")");
                }
            }

            foreach (var entry in entries)
            {
                if (!entry.Unified)
                {
                    continue;
                }

                var innerGroups = new List<BubbleGroup>();

                var innerGroupsIds = entry.UnifiedBubbleGroupsGuids.Split(',');
                if (innerGroupsIds != null)
                {
                    foreach (var innerGroupId in innerGroupsIds)
                    {
                        var innerGroup = BubbleGroupManager.Find(innerGroupId);
                        if (innerGroup == null)
                        {
                            Utils.DebugPrint("Unified group, inner group " + innerGroupId + " could not be related.");
                        }
                        else
                        {
                            innerGroups.Add(innerGroup);
                        }
                    }
                }

                if (!innerGroups.Any())
                {
                    Utils.DebugPrint("This unified group has no inner groups. Skipping this unified group.");
                    continue;
                }

                var primaryGroup = innerGroups.FirstOrDefault(x => x.ID == entry.UnifiedPrimaryBubbleGroupGuid);
                if (primaryGroup == null)
                {
                    Utils.DebugPrint("Unified group, primary group " + entry.UnifiedPrimaryBubbleGroupGuid +
                        " could not be related. Skipping this unified group.");
                    continue;
                }

                var id = entry.Guid;

                var unifiedGroup = BubbleGroupFactory.CreateUnifiedInternal(innerGroups, primaryGroup, id);

                var sendingGroup = innerGroups.FirstOrDefault(x => x.ID == entry.UnifiedSendingBubbleGroupGuid);
                if (sendingGroup != null)
                {
                    unifiedGroup._sendingGroup = sendingGroup;
                }

                BubbleGroupManager.BubbleGroupsAdd(unifiedGroup, true);
            }
        }

        internal static void Load()
        {
            if (!File.Exists(Location))
            {
                Migrate();
            }
            LoadInternal();
        }

        private static void Migrate()
        {
            lock (_migrationLock)
            {
                var groups = new List<BubbleGroup>();
                var serviceNames = new Dictionary<string, string>();

                // Load all normal groups

                var bubbleGroupsLocations = Directory.GetFiles(BubbleGroupDatabase.GetBaseLocation(), "*.*", SearchOption.AllDirectories);
                var bubbleGroupsLocationsSorted =
                    bubbleGroupsLocations.OrderByDescending(x => Time.GetUnixTimestamp(File.GetLastWriteTime(x))).ToList();

                foreach (var bubbleGroupLocation in bubbleGroupsLocationsSorted)
                {
                    String groupId = null;
                    try
                    {
                        var groupHeader = Path.GetFileNameWithoutExtension(bubbleGroupLocation);

                        var groupDelimeter = groupHeader.IndexOf("^", StringComparison.Ordinal);
                        var serviceName = groupHeader.Substring(0, groupDelimeter);
                        groupId = groupHeader.Substring(groupDelimeter + 1);

                        serviceNames.Add(groupId, serviceName);
                        Service service = null;

                        var deserializedBubbleGroup = LoadPartiallyIfPossible(bubbleGroupLocation, 
                                                      service, groupId, 100);
                        if (deserializedBubbleGroup == null)
                        {
                            throw new Exception("DeserializedBubbleGroup is nothing.");
                        }

                        groups.Add(deserializedBubbleGroup);
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("[Migration] Group " + bubbleGroupLocation + " is corrupt." + ex);
                    }
                }

                // Load all unified groups

                var unifiedBubbleGroupsDatabase =
                    new SimpleDatabase<UnifiedBubbleGroup, DisaUnifiedBubbleGroupEntry>("UnifiedBubbleGroups");
                foreach (var group in unifiedBubbleGroupsDatabase)
                {
                    var innerGroups = new List<BubbleGroup>();
                    foreach (var innerGroupId in @group.Serializable.GroupIds)
                    {
                        var innerGroup = groups.FirstOrDefault(x => x.ID == innerGroupId);
                        if (innerGroup == null)
                        {
                            Utils.DebugPrint("[Migration] Unified group, inner group " + innerGroupId + " could not be related.");
                        }
                        else
                        {
                            innerGroups.Add(innerGroup);
                        }
                    }
                    if (!innerGroups.Any())
                    {
                        Utils.DebugPrint("[Migration] This unified group has no inner groups. Skipping this unified group.");
                        continue;
                    }

                    var primaryGroup = innerGroups.FirstOrDefault(x => x.ID == @group.Serializable.PrimaryGroupId);
                    if (primaryGroup == null)
                    {
                        Utils.DebugPrint("[Migration] Unified group, primary group " + @group.Serializable.PrimaryGroupId +
                        " could not be related. Skipping this unified group.");
                        continue;
                    }

                    var id = @group.Serializable.Id;

                    var unifiedGroup = BubbleGroupFactory.CreateUnifiedInternal(innerGroups, primaryGroup, id);

                    var sendingGroup = innerGroups.FirstOrDefault(x => x.ID == @group.Serializable.SendingGroupId);
                    if (sendingGroup != null)
                    {
                        unifiedGroup.SendingGroup = sendingGroup;
                    }
                        
                    groups.Add(unifiedGroup);
                }

                // save it to the new index

                var entries = new List<Entry>();
                foreach (var group in groups)
                {
                    var unified = group as UnifiedBubbleGroup;
                    if (unified == null)
                    {
                        var lastBubble = group.LastBubbleSafe();
                        var serviceName = serviceNames[group.ID];
                        if (serviceName != null)
                        {
                            entries.Add(new Entry
                            {
                                Guid = group.ID,
                                Service = serviceName,
                                LastBubble = SerializeBubble(lastBubble),
                                LastBubbleGuid = lastBubble.ID
                            });
                        }
                        else
                        {
                            Utils.DebugPrint("[Migration] Weird... there's no associated service name!");
                        }
                    }
                    else
                    {
                        entries.Add(new Entry
                        {
                            Guid = group.ID,
                            Unified = true,
                            UnifiedBubbleGroupsGuids = unified.Groups.Select(x => x.ID).
                            Aggregate((current, next) => current + "," + next),
                            UnifiedPrimaryBubbleGroupGuid = unified.PrimaryGroup.ID,
                            UnifiedSendingBubbleGroupGuid = unified.SendingGroup.ID,
                        });
                    }
                }
                Save(entries.ToArray());
            }
        }

        private static byte[] SerializeBubble(VisualBubble b)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, b);

                return ms.ToArray();
            }
        }

        private static VisualBubble DeserializeBubble(byte[] visualBubbleData)
        {
            using (var bubbleDataRawStream = new MemoryStream(visualBubbleData))
            {
                var visualBubble = Serializer.Deserialize<VisualBubble>(bubbleDataRawStream);

                return visualBubble;
            }
        }

        private static BubbleGroup LoadPartiallyIfPossible(string location, Service service, string groupId, int bubblesPerGroup = 100)
        {
            using (var stream = File.Open(location, FileMode.Open, FileAccess.Read))
            {
                stream.Seek(stream.Length, SeekOrigin.Begin);

                var mostRecentVisualBubble = BubbleGroupDatabase.FetchNewestBubbleIfNotWaiting(stream, service);
                if (mostRecentVisualBubble != null)
                {
                    return new BubbleGroup(mostRecentVisualBubble, groupId, true);
                }
            }

            var bubbles = BubbleGroupDatabase.FetchBubbles(location, service, bubblesPerGroup).Reverse().ToList();
            return new BubbleGroup(bubbles, groupId);
        }
    }
}


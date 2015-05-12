using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public class BubbleGroupFactory
    {
        internal static SimpleDatabase<UnifiedBubbleGroup, DisaUnifiedBubbleGroupEntry> UnifiedBubbleGroupsDatabase
        {
            get;
            private set;
        }

        internal static void Initialize()
        {
            UnifiedBubbleGroupsDatabase =
                new SimpleDatabase<UnifiedBubbleGroup, DisaUnifiedBubbleGroupEntry>("UnifiedBubbleGroups");
        }

        private static Tuple<List<VisualBubble>, List<Tuple<BubbleGroup, long>>> LoadDatabaseBubblesOnUnitInto
        (List<BubbleGroup> groups, int day, List<Tuple<BubbleGroup, Stream>> handles, 
            List<Tuple<BubbleGroup, long>> cursors, string[] bubbleTypes = null, Func<VisualBubble, bool> comparer = null)
        {
            var allBubbles = new List<VisualBubble>();
            var cursorTuples = new List<Tuple<BubbleGroup, long>>();

            foreach (var innerGroup in groups)
            {
                var bubbles = new List<VisualBubble>();

                var cursor = cursors.FirstOrDefault(x => x.Item1 == innerGroup);
                if (cursor == null)
                    continue;

                if (cursor.Item2 == -2)
                {
                    cursorTuples.Add(cursor);
                    continue;
                }

                var innerGroupCursor = BubbleGroupDatabase.FetchBubblesOnDay(innerGroup,
                    handles.FirstOrDefault(x => x.Item1 == innerGroup).Item2,
                    bubbles.Add, day, cursor.Item2, bubbleTypes, comparer);
                cursorTuples.Add(new Tuple<BubbleGroup, long>(innerGroup, innerGroupCursor));
                bubbles.Reverse();
                allBubbles.AddRange(bubbles);
            }

            return allBubbles.Count == 0
                ? new Tuple<List<VisualBubble>, List<Tuple<BubbleGroup, long>>>(
                    null, cursorTuples)
                    : new Tuple<List<VisualBubble>, List<Tuple<BubbleGroup, long>>>(
                        allBubbles, cursorTuples);
        }

        private static Tuple<List<VisualBubble>, List<Tuple<BubbleGroup, long>>> LoadDatabaseBubblesOnUnitInto
            (UnifiedBubbleGroup group, int day, List<Tuple<BubbleGroup, Stream>> handles, 
            List<Tuple<BubbleGroup, long>> cursors, string[] bubbleTypes = null, Func<VisualBubble, bool> comparer = null)
        {
            return LoadDatabaseBubblesOnUnitInto(group.Groups, day, handles, cursors, bubbleTypes, comparer);
        }

        private static IEnumerable<Tuple<BubbleGroup, Stream>> OpenDatabaseStreams(UnifiedBubbleGroup group)
        {
            return OpenDatabaseStreams(@group.Groups);
        }

        private static IEnumerable<Tuple<BubbleGroup, Stream>> OpenDatabaseStreams(List<BubbleGroup> groups)
        {
            foreach (var group in @groups)
            {
                var groupLocation = BubbleGroupDatabase.GetLocation(group);
                var stream = File.Open(groupLocation, FileMode.Open, FileAccess.Read);

                yield return new Tuple<BubbleGroup, Stream>(group, stream);
            }
        }

        private static void CloseDatabaseStreams(IEnumerable<Tuple<BubbleGroup, Stream>> handles)
        {
            foreach (var handle in handles)
            {
                try
                {
                    handle.Item2.Dispose();
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Failed to close unified unified inner group: " + ex.Message);
                }
            }
        }

        public class Cursor
        {
            private readonly BubbleGroup _group;
            private readonly Selection _selection;
            private int _day = 1;
            private bool _endReached;

            public Cursor(BubbleGroup group, Selection selection)
            {
                _group = group;
                _selection = selection;
            }

            public class Selection
            {
                public string[] BubbleTypes { get; set; }
                public Func<VisualBubble, bool> Comparer { get; set; }
            }

            public Task<List<VisualBubble>> FetchNext()
            {
                return Task.Factory.StartNew<List<VisualBubble>>(() =>
                {
                    if (_endReached)
                        return null;
                    
                    lock (BubbleGroupDatabase.OperationLock)
                    {
                        var groups = BubbleGroupManager.GetInner(_group);
                        var groupCursors = groups.Select(@group => new Tuple<BubbleGroup, long>(@group, -1)).ToList();

                        var allBubbles = new List<VisualBubble>();

                        var handles = OpenDatabaseStreams(groups).ToList();

                        GetMoreBubbles:

                        var result = LoadDatabaseBubblesOnUnitInto(groups, _day, handles, groupCursors, _selection.BubbleTypes, _selection.Comparer);

                        var bubbles = result.Item1;
                        if (bubbles != null)
                        {
                            allBubbles.AddRange(bubbles);
                        }

                        groupCursors = result.Item2;
                        _day++;

                        var endReached = result.Item2.Count(cursor => cursor.Item2 == -2);
                        if (endReached == result.Item2.Count)
                        {
                            _endReached = true;
                            goto ReturnResult;
                        }
                        if (bubbles == null)
                        {
                            goto GetMoreBubbles;
                        }
                        if (allBubbles.Count < 100)
                        {
                            goto GetMoreBubbles;
                        }

                        ReturnResult:

                        CloseDatabaseStreams(handles);

                        allBubbles.TimSort((x, y) => -x.Time.CompareTo(y.Time));

                        return allBubbles;
                    }
                });
            }
        }

        private static void Populate(UnifiedBubbleGroup unified)
        {
            var innerGroupsCursors = unified.Groups.Select(@group => new Tuple<BubbleGroup, long>(@group, -1)).ToList();

            var allBubbles = new List<VisualBubble>();
            var day = 1;

            var handles = OpenDatabaseStreams(unified).ToList();

            GetMoreBubbles:
            var result = LoadDatabaseBubblesOnUnitInto(unified, day, handles, innerGroupsCursors);
            innerGroupsCursors = result.Item2;
            day++;
            var bubbles = result.Item1;
            if (bubbles != null)
            {
                allBubbles.AddRange(bubbles);
            }
            var endReached = result.Item2.Count(cursor => cursor.Item2 == -2);
            if (endReached == result.Item2.Count)
            {
                goto End;
            }
            if (bubbles == null)
            {
                goto GetMoreBubbles;
            }
            if (allBubbles.Count < 100)
            {
                goto GetMoreBubbles;
            }

            End:

            CloseDatabaseStreams(handles);

            allBubbles.TimSort((x, y) => x.Time.CompareTo(y.Time));
            Func<VisualBubble, VisualBubble> tryFindRealBubble = incoming =>
            {
                foreach (var bubble in
                    unified.Groups.SelectMany(@group => @group.Where(bubble => bubble.ID == incoming.ID)))
                {
                    return bubble;
                }

                return incoming;
            };
            for (var i = 0; i < allBubbles.Count; i++)
            {
                allBubbles[i] = tryFindRealBubble(allBubbles[i]);
            }
            unified.Bubbles.Clear();
            foreach (var bubble in allBubbles)
            {
                unified.Bubbles.Add(bubble);
            }
            unified.UnifiedGroupLoaded = true;
        }

        public static UnifiedBubbleGroup CreateUnified(List<BubbleGroup> groups, BubbleGroup primaryGroup)
        {
            lock (BubbleGroupDatabase.OperationLock)
            {
                var unifiedGroupsToKill = new HashSet<UnifiedBubbleGroup>();
                foreach (var group in groups)
                {
                    if (group.IsUnified)
                    {
                        unifiedGroupsToKill.Add(group.Unified);
                        @group.DeregisterUnified();
                    }
                }
                foreach (var unifiedGroup in unifiedGroupsToKill)
                {
                    BubbleGroupManager.BubbleGroupsRemove(unifiedGroup);
                }
                UnifiedBubbleGroupsDatabase.Remove(unifiedGroupsToKill);

                var unified = CreateUnifiedInternal(groups, primaryGroup);
                UnifiedBubbleGroupsDatabase.Add(unified,
                    new DisaUnifiedBubbleGroupEntry(unified.ID,
                        unified.Groups.Select(innerGroup => innerGroup.ID)
                        .ToArray(), primaryGroup.ID, primaryGroup.ID));
                BubbleGroupManager.BubbleGroupsAdd(unified);
                return unified;
            }
        }

        private static UnifiedBubbleGroup CreateUnifiedInternal(List<BubbleGroup> groups, BubbleGroup primaryGroup, string id = null)
        {
            var service = ServiceManager.GetUnified();
            if (service == null)
                return null;

            var newBubble = new NewBubble(Time.GetNowUnixTimestamp(), Bubble.BubbleDirection.Incoming,
                Guid.NewGuid() + "@unified", null, false, service);
            var unified = new UnifiedBubbleGroup(groups, primaryGroup, newBubble, id);
            if (id == null)
            {
                var unread = groups.FirstOrDefault(BubbleGroupSettingsManager.GetUnread) != null;
                BubbleGroupSettingsManager.SetUnread(unified, unread);
            }

            foreach (var group in groups)
            {
                @group.RegisterUnified(unified);
            }

            var associatedPartiallyLoadedGroups = groups.Where(x => x.PartiallyLoaded).ToList();
            if (associatedPartiallyLoadedGroups.Any())
            {
                VisualBubble latest = null;
                foreach (var innerGroup in unified.Groups)
                {
                    var current = innerGroup.Last();
                    if (latest == null || current.Time > latest.Time)
                    {
                        latest = current;
                    }
                }
                if (latest != null)
                {
                    unified.Bubbles.Clear();
                    unified.Bubbles.Add(latest);
                }
                return unified;
            }

            Populate(unified);

            return unified;
        }

        public static bool LoadFullyIfNeeded(BubbleGroup group, bool sync = false)
        {
            if (@group == null)
                return false;

            var loadedSomething = false;

            lock (BubbleGroupDatabase.OperationLock)
            {
                var unifiedGroup = @group as UnifiedBubbleGroup;

                var associatedGroups = unifiedGroup != null
                    ? unifiedGroup.Groups.ToList()
                    : new[] { @group }.ToList();
                var associatedPartiallyLoadedGroups = associatedGroups.Where(x => x.PartiallyLoaded).ToList();
                foreach (var partiallyLoadedGroup in associatedPartiallyLoadedGroups)
                {
                    loadedSomething = true;
                    var partiallyLoadedBubblesToRemove = partiallyLoadedGroup.Bubbles.ToList();
                    foreach (var bubble in BubbleGroupDatabase.FetchBubbles(partiallyLoadedGroup).Reverse())
                    {
                        partiallyLoadedGroup.Bubbles.Add(bubble);
                    }
                    foreach (var partiallyLoadedBubbleToRemove in partiallyLoadedBubblesToRemove)
                    {
                        partiallyLoadedGroup.Bubbles.Remove(partiallyLoadedBubbleToRemove);
                    }
                    partiallyLoadedGroup.PartiallyLoaded = false;
                }

                if (unifiedGroup != null && !unifiedGroup.UnifiedGroupLoaded)
                {
                    Populate(unifiedGroup);
                }
            }

            if (!sync && loadedSomething)
            {
                Task.Factory.StartNew(() =>
                {
                    var unifiedGroup = @group as UnifiedBubbleGroup;
                    if (unifiedGroup != null)
                    {
                        BubbleQueueManager.Send(unifiedGroup.Groups.Where(x => ServiceManager.IsRunning(x.Service))
                            .Select(x => x.Service.Information.ServiceName).ToArray());
                    }
                    else if (ServiceManager.IsRunning(@group.Service))
                    {
                        BubbleQueueManager.Send(new[] { @group.Service.Information.ServiceName });
                    }
                    BubbleManager.SetNotQueuedToFailures(@group);
                });
            }

            return loadedSomething;
        }

        internal static void LoadAllPartiallyIfPossible(int bubblesPerGroup = 100)
        {
            var corruptedGroups = new List<string>();

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

                    var service = ServiceManager.GetByName(serviceName);

                    if (service == null)
                    {
                        Utils.DebugPrint("Service " + serviceName +
                                                 " not found in AllServices!");
                        continue;
                    }

                    var deserializedBubbleGroup = LoadPartiallyIfPossible(bubbleGroupLocation, service, groupId, bubblesPerGroup);
                    if (deserializedBubbleGroup == null)
                    {
                        throw new Exception("DeserializedBubbleGroup is nothing.");
                    }
                    BubbleGroupManager.BubbleGroupsAdd(deserializedBubbleGroup);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint("Group " + bubbleGroupLocation + " is corrupt. Deleting. " + ex);
                    File.Delete(bubbleGroupLocation);
                    if (groupId != null)
                        corruptedGroups.Add(groupId);
                }
            }

            var migrationResaveNeeded = false;
            var corruptedUnifiedGroups =
                new List<SimpleDatabase<UnifiedBubbleGroup, DisaUnifiedBubbleGroupEntry>.Container>();
            var removeFromRuntime =
                new List<SimpleDatabase<UnifiedBubbleGroup, DisaUnifiedBubbleGroupEntry>.Container>();
            foreach (var group in UnifiedBubbleGroupsDatabase)
            {
                var innerGroupCorrupt = false;
                var innerGroups = new List<BubbleGroup>();
                foreach (var innerGroupId in @group.Serializable.GroupIds)
                {
                    var innerGroup = BubbleGroupManager.Find(innerGroupId);
                    if (innerGroup == null)
                    {
                        Utils.DebugPrint("Unified group, inner group " + innerGroupId +
                                                 " could not be related.");
                        if (corruptedGroups.Contains(innerGroupId))
                        {
                            Utils.DebugPrint(
                                "It was detected that this inner group was corrupted and deleted. Will delete unified group.");
                            innerGroupCorrupt = true;
                            corruptedUnifiedGroups.Add(@group);
                        }
                    }
                    else
                    {
                        innerGroups.Add(innerGroup);
                    }
                }
                if (!innerGroups.Any())
                {
                    Utils.DebugPrint("Yuck. This unified group has no inner groups. Removing from runtime.");
                    removeFromRuntime.Add(@group);
                    continue;
                }
                if (innerGroupCorrupt)
                    continue;

                var primaryGroup = innerGroups.FirstOrDefault(x => x.ID == @group.Serializable.PrimaryGroupId);
                if (primaryGroup == null)
                {
                    Utils.DebugPrint("Unified group, primary group " + @group.Serializable.PrimaryGroupId +
                                             " could not be related. Removing from runtime.");
                    removeFromRuntime.Add(@group);
                    continue;
                }

                var id = @group.Serializable.Id;

                var unifiedGroup = CreateUnifiedInternal(innerGroups, primaryGroup, id);
                if (id == null)
                {
                    migrationResaveNeeded = true;
                    @group.Serializable.Id = unifiedGroup.ID;
                }

                var sendingGroup = innerGroups.FirstOrDefault(x => x.ID == @group.Serializable.SendingGroupId);
                if (sendingGroup != null)
                {
                    unifiedGroup.SendingGroup = sendingGroup;
                }

                @group.Object = unifiedGroup;
                BubbleGroupManager.BubbleGroupsAdd(unifiedGroup);
            }
            if (removeFromRuntime.Any())
            {
                foreach (var group in removeFromRuntime)
                {
                    UnifiedBubbleGroupsDatabase.Remove(@group);
                }
            }
            if (corruptedUnifiedGroups.Any())
            {
                foreach (var group in corruptedUnifiedGroups)
                {
                    UnifiedBubbleGroupsDatabase.Remove(@group);
                }
            }
            if (migrationResaveNeeded)
            {
                Utils.DebugPrint("It was detected that we need to save migration changes for UnifiedBubbleGroups.");
                UnifiedBubbleGroupsDatabase.SaveChanges();
            }

            try
            {
                foreach (var groupCache in BubbleGroupCacheManager.Load())
                {
                    var associatedGroup = BubbleGroupManager.Find(groupCache.Guid);
                    if (associatedGroup == null)
                        continue;

                    var unifiedGroup = associatedGroup as UnifiedBubbleGroup;
                    if (unifiedGroup != null)
                    {
                        associatedGroup = unifiedGroup.PrimaryGroup;
                    }

                    associatedGroup.Title = groupCache.Name;
                    associatedGroup.Photo = groupCache.Photo;
                    associatedGroup.IsPhotoSetInitiallyFromCache = true;
                    if (groupCache.Participants != null)
                    {
                        associatedGroup.Participants = groupCache.Participants.ToSynchronizedCollection();
                        foreach (var participant in associatedGroup.Participants)
                        {
                            participant.IsPhotoSetInitiallyFromCache = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to load bubble groups!: " + ex);
            }

            BubbleGroupSettingsManager.Load();
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

        internal static void UnloadFullLoad(BubbleGroup group)
        {
            var innerGroups = BubbleGroupManager.GetInner(@group);
            foreach (var innerGroup in innerGroups)
            {
                innerGroup.UnloadFullLoad();
            }
            var unifiedGroup = @group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                unifiedGroup.UnloadFullUnifiedLoad();
            }
        }

        public static List<BubbleGroup> DeleteUnified(UnifiedBubbleGroup unifiedGroup, bool deleteInnerGroups = true)
        {
            lock (BubbleGroupDatabase.OperationLock)
            {
                foreach (var group in unifiedGroup.Groups)
                {
                    @group.DeregisterUnified();
                    if (deleteInnerGroups)
                        Delete(@group);
                }

                BubbleGroupManager.BubbleGroupsRemove(unifiedGroup);
                UnifiedBubbleGroupsDatabase.Remove(unifiedGroup);

                return deleteInnerGroups ? null : unifiedGroup.Groups;
            }
        }

        public static void Delete(BubbleGroup group)
        {
            lock (BubbleGroupDatabase.OperationLock)
            {
                var unifiedGroup = @group as UnifiedBubbleGroup;
                if (unifiedGroup != null)
                {
                    DeleteUnified(unifiedGroup);
                    return;
                }

                var file = BubbleGroupDatabase.GetLocation(@group);

                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                BubbleGroupSync.RemoveFromSync(@group);

                BubbleGroupSync.DeleteBubbleGroupIfHasAgent(@group);

                BubbleGroupManager.BubbleGroupsRemove(@group);
            }
        }

        public static BubbleGroup AddNewIfNotExist(VisualBubble bubble, bool updateUi = false)
        {
            var group =
                BubbleGroupManager.FindWithAddress(bubble.Service, bubble.Address);
            if (@group != null)
                return null;

            return AddNewInternal(bubble, updateUi);
        }

        public static BubbleGroup AddNew(NewBubble newBubble, bool updateUi = false)
        {
            return AddNewInternal((VisualBubble)newBubble, updateUi);
        }

        private static BubbleGroup AddNewInternal(VisualBubble newBubble, bool raiseBubbleInserted)
        {
            var group = new BubbleGroup(newBubble, null, false);

            if (ServiceManager.IsRunning(@group.Service))
            {
                newBubble.Service.NewBubbleGroupCreated(@group).ContinueWith(x =>
                {
                    // force the UI to refetch the photo
                    @group.IsPhotoSetFromService = false;
                    BubbleManager.SendSubscribe(@group, true);
                    BubbleGroupUpdater.Update(@group);
                });
            }

            BubbleGroupManager.BubbleGroupsAdd(@group);

            BubbleGroupDatabase.AddBubble(@group, newBubble);

            if (raiseBubbleInserted)
            {
                try
                {
                    BubbleGroupEvents.RaiseBubbleInserted(newBubble, @group);
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(
                        "Error in notifying the interface that the bubble group has been updated (" +
                        newBubble.Service.Information.ServiceName + "): " + ex.Message);
                }
            }

            BubbleGroupUpdater.Update(@group);

            return @group;
        }
    }
}
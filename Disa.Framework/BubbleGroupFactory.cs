using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Disa.Framework
{
    public class BubbleGroupFactory
    {
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
                BubbleGroupIndex.RemoveUnified(unifiedGroupsToKill.Select(x => x.ID).ToArray());

                var unified = CreateUnifiedInternal(groups, primaryGroup);
                BubbleGroupIndex.AddUnified(unified);
                BubbleGroupManager.BubbleGroupsAdd(unified);
                return unified;
            }
        }

        internal static UnifiedBubbleGroup CreateUnifiedInternal(List<BubbleGroup> groups, BubbleGroup primaryGroup, string id = null)
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
                    var partiallyLoadedBubblesToRemove = partiallyLoadedGroup.Bubbles.ToList();
                    var rollingBack = false;
                    TryAgain:
                    try
                    {
                        loadedSomething = true;
                        partiallyLoadedGroup.PartiallyLoaded = false;
                        foreach (var bubble in BubbleGroupDatabase.FetchBubbles(partiallyLoadedGroup).Reverse())
                        {
                            partiallyLoadedGroup.Bubbles.Add(bubble);
                        }
                        foreach (var partiallyLoadedBubbleToRemove in partiallyLoadedBubblesToRemove)
                        {
                            partiallyLoadedGroup.Bubbles.Remove(partiallyLoadedBubbleToRemove);
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("Failed to fully load partially loaded group " + partiallyLoadedGroup.ID + ": " + ex);
                        if (!rollingBack)
                        {
                            Utils.DebugPrint("Attempting to roll back the last transaction....");
                            rollingBack = true;
                            var lastModifiedIndex = BubbleGroupIndex.GetLastModifiedIndex(partiallyLoadedGroup.ID);
                            if (lastModifiedIndex.HasValue)
                            {
                                try
                                {
                                    BubbleGroupDatabase.RollBackTo(partiallyLoadedGroup, lastModifiedIndex.Value);
                                    goto TryAgain;
                                }
                                catch (Exception ex2)
                                {
                                    Utils.DebugPrint("Failed to rollback: " + ex2);
                                    // fall-through. It's unrecoverable!
                                }
                            }
                            else
                            {
                                // fall-through. It's unrecoverable!
                            }
                        }
                        Utils.DebugPrint("Partially loaded group is dead. Killing and restarting (lost data occurred).");
                        BubbleGroupDatabase.Kill(partiallyLoadedGroup);
                        BubbleGroupSync.RemoveFromSync(partiallyLoadedGroup);
                        BubbleGroupDatabase.AddBubbles(partiallyLoadedGroup, 
                            partiallyLoadedBubblesToRemove.ToArray());
                    }
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
                    BubbleQueueManager.SetNotQueuedToFailures(@group);
                });
            }

            return loadedSomething;
        }

        internal static void LoadAllPartiallyIfPossible()
        {
            BubbleGroupIndex.Load();
            BubbleGroupSettingsManager.Load();
            BubbleGroupCacheManager.LoadAll();
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
                BubbleGroupIndex.RemoveUnified(unifiedGroup.ID);

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

        public static void OutputBubblesInJsonFormat(string bubbleGroupLocation, string jsonOutputLocation, 
            int count = int.MaxValue, bool skipDeleted = true)
        {
            lock (BubbleGroupDatabase.OperationLock)
            {
                using (var fs = File.OpenWrite(jsonOutputLocation))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        using (var writer = new JsonTextWriter(sw))
                        {
                            sw.Write("var json_bubbles = '");
                            writer.WriteStartArray();
                            foreach (var bubble in BubbleGroupDatabase.FetchBubbles(bubbleGroupLocation, null, count, skipDeleted))
                            {
                                var jobject = JObject.FromObject(bubble);
                                jobject.Add("ID", bubble.ID);
                                jobject.Add("Type", bubble.GetType().Name);
                                writer.WriteStartObject();
                                writer.WritePropertyName("bubble");
                                writer.WriteValue(Convert.ToBase64String(Encoding.UTF8.GetBytes(jobject.ToString(Formatting.None))));
                                writer.WriteEndObject();
                            }
                            writer.WriteEndArray();
                            sw.Write("'");
                        }
                    }
                }
            }
        }
    }
}
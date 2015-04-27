using System;
using Disa.Framework.Bubbles;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;
using System.Collections;
using System.Reflection;
using System.Threading;

namespace Disa.Framework
{
    public static class BubbleGroupSync
    {
        public interface Agent
        {
            Task<Result> Sync(BubbleGroup group, string actionId);
            Task<bool> DeleteBubble(BubbleGroup group, VisualBubble bubble);
            Task<bool> DeleteConversation(BubbleGroup group);
            Task<List<VisualBubble>> LoadBubbles(BubbleGroup group, long fromTime, int max = 100);
        }

        public interface Comparer
        {
            bool LoadBubblesComparer(VisualBubble localBubble, VisualBubble agentBubble);
        }
            
        //TODO: rename
        public class Result
        {
            public enum Type { Purge, New, JustRefresh }

            public string NewActionId { get; private set; }
            public VisualBubble[] Updates { get; private set; }
            public VisualBubble[] Inserts { get; private set; }
            public Type ResultType { get; private set; }

            public Result(Type resultType, string newActionId, VisualBubble[] updates, VisualBubble[] inserts)
            {
                NewActionId = newActionId;
                Updates = updates;
                Inserts = inserts;
                ResultType = resultType;
            }

            public Result(bool justRefresh = false)
            {
                if (justRefresh)
                {
                    ResultType = Type.JustRefresh;
                }
            }

            public bool EmptyResult
            {
                get
                {
                    return EmptyUpdates && EmptyInserts;
                }
            }

            public bool EmptyUpdates
            {
                get
                {
                    return (Updates == null || !Updates.Any());
                }
            }

            public bool EmptyInserts
            {
                get
                {
                    return (Inserts == null || !Inserts.Any());
                }
            }

            public bool NullActionId
            {
                get
                {
                    return string.IsNullOrEmpty(NewActionId);
                }
            }

            public bool JustRefresh
            {
                get
                {
                    return ResultType == Type.JustRefresh;
                }
            }
        }

        private static class Database
        {
            private static object _lock = new object();

            private static string Location
            {
                get
                {
                    var databasePath = Platform.GetDatabasePath();
                    var bubbleGroupsSyncLocation = Path.Combine(databasePath, "BubbleGroupsSync.db");

                    return bubbleGroupsSyncLocation;
                }
            }

            private class Entry
            {
                [PrimaryKey, AutoIncrement]
                public int Id { get; set; }

                public string ActionId { get; set; }
                public string Guid { get; set; }
            }

            public static string GetActionId(BubbleGroup group)
            {
                lock (_lock)
                {
                    using (var db = new SqlDatabase<Entry>(Location))
                    {
                        var groupId = group.ID;
                        foreach (var entry in db.Store.Where(x => x.Guid == groupId))
                        {
                            return entry.ActionId;
                        }
                    }

                    return null;
                }
            }

            public static void SetActionId(BubbleGroup group, string actionId)
            {
                lock (_lock)
                {
                    using (var db = new SqlDatabase<Entry>(Location))
                    {
                        var groupId = group.ID;
                        foreach (var entry in db.Store.Where(x => x.Guid == groupId))
                        {
                            entry.ActionId = actionId;
                            db.Update(entry);
                            return;
                        }

                        // insert a new one
                        var newEntry = new Entry()
                        {
                            ActionId = actionId,
                            Guid = group.ID
                        };
                        db.Add(newEntry);
                    }
                }
            }

            public static void RemoveActionId(BubbleGroup group)
            {
                lock (_lock)
                {
                    using (var db = new SqlDatabase<Entry>(Location))
                    {
                        var groupId = group.ID;
                        foreach (var entry in db.Store.Where(x => x.Guid == groupId))
                        {
                            db.Remove(entry);
                            return;
                        }
                    }
                }
            }
        }

        public const int SearchDepth = 1000;
        public static readonly object SyncLock = new object();

        public static void ResetSyncsIfHasAgent(BubbleGroup group)
        {
            var groupsToReset = new List<BubbleGroup>();

            var unifiedGroup = group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                groupsToReset.AddRange(unifiedGroup.Groups);
            }
            else
            {
                groupsToReset.Add(group);
            }

            foreach (var groupToReset in groupsToReset)
            {
                if (SupportsSync(groupToReset.Service))
                {
                    groupToReset.NeedsSync = true;
                }
            }
        }

        public static void ResetSyncsIfHasAgent(Service service)
        {
            if (!SupportsSync(service))
                return;

            foreach (var group in BubbleGroupManager.FindAll(service))
            {
                group.NeedsSync = true;
            }
        }

        public static bool ResetSyncsIfHasAgent(Service service, string bubbleGroupAddress)
        {
            if (!SupportsSync(service))
                return false;

            foreach (var group in BubbleGroupManager.FindAll(service))
            {
                if (service.BubbleGroupComparer(group.Address, bubbleGroupAddress))
                {
                    group.NeedsSync = true;
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> DeleteBubbleIfHasAgent(BubbleGroup group, VisualBubble bubble)
        {
            if (group is UnifiedBubbleGroup)
                return false;

            if (!SupportsSyncAndIsRunning(group.Service))
                return false;

            var groupAgent = group.Service as Agent;
            try
            {
                return await groupAgent.DeleteBubble(group, bubble);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to delete bubble in sync agent: " + ex);
                return false;
            }
        }

        public static async Task<bool> DeleteBubbleGroupIfHasAgent(BubbleGroup group)
        {
            if (group is UnifiedBubbleGroup)
                return false;

            if (!SupportsSyncAndIsRunning(group.Service))
                return false;

            var groupAgent = group.Service as Agent;
            try
            {
                return await groupAgent.DeleteConversation(group);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Failed to delete conversation in sync agent: " + ex);
                return false;
            }
        }

        public static bool NeedsSync(BubbleGroup group)
        {
            if (group == null)
                return false;

            var groupsToSync = new List<BubbleGroup>();

            var unifiedGroup = group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                groupsToSync.AddRange(unifiedGroup.Groups);
            }
            else
            {
                groupsToSync.Add(group);
            }

            foreach (var groupToSync in groupsToSync)
            {
                if (!SupportsSyncAndIsRunning(groupToSync.Service))
                {
                    continue;
                }

                if (groupToSync.NeedsSync)
                    return true;
            }

            return false;
        }

        public static bool SupportsSyncAndIsRunning(BubbleGroup group)
        {
            if (group == null)
                return false;

            var groupsToSync = new List<BubbleGroup>();

            var unifiedGroup = group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                groupsToSync.AddRange(unifiedGroup.Groups);
            }
            else
            {
                groupsToSync.Add(group);
            }

            foreach (var groupToSync in groupsToSync)
            {
                if (SupportsSyncAndIsRunning(groupToSync.Service))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SupportsSync(Service service)
        {
            return service is Agent;
        }

        public static void RemoveFromSync(BubbleGroup group)
        {
            if (group == null)
                return;

            var groupsToRemove = new List<BubbleGroup>();

            var unifiedGroup = group as UnifiedBubbleGroup;
            if (unifiedGroup != null)
            {
                groupsToRemove.AddRange(unifiedGroup.Groups);
            }
            else
            {
                groupsToRemove.Add(group);
            }

            foreach (var groupToRemove in groupsToRemove)
            {
                if (!SupportsSync(groupToRemove.Service))
                    continue;

                Database.RemoveActionId(groupToRemove);
            }
        }

        private static bool SupportsSyncAndIsRunning(Service service)
        {
            if (!ServiceManager.IsRunning(service))
                return false;

            if (!SupportsSync(service))
                return false;

            return true;
        }

        public static IEnumerable<VisualBubble> ReadBubblesFromDatabase(BubbleGroup group)
        {
            return BubbleGroupDatabase.FetchBubbles(group, SearchDepth, false);
        }

        private class LoadBubblesIntoStateHolder
        {
            public bool Dead { get; set; }
            public bool LocalFinished { get; set; }
            public long LocalCursor { get; set; }
            public List<VisualBubble> Bubbles { get; set; }

            public LoadBubblesIntoStateHolder()
            {
                LocalCursor = -1; // i want Roselyn
            }
        }

        private static List<VisualBubble> LoadBubblesIntoQueryAgent(Agent innerGroupAgent, BubbleGroup innerGroup, 
            long currentTime, int maxLoadPerGroup, LoadBubblesIntoStateHolder innerGroupState)
        {
            try
            {
                var task = innerGroupAgent.LoadBubbles(innerGroup, currentTime, maxLoadPerGroup);
                task.Wait();
                var loadedBubbles = task.Result;

                if (loadedBubbles == null || loadedBubbles.Count == 0)
                {
                    innerGroupState.Dead = true;
                    return null;
                }
                else
                {
                    return loadedBubbles;
                }

            }
            catch (Exception ex)
            {
                Utils.DebugPrint("Loading bubbles for service " +
                    innerGroup.Service.Information.ServiceName + " on group " + innerGroup.ID + " failed: " + ex);
                innerGroupState.Dead = true;
            }

            return null;
        }

        private static List<VisualBubble> LoadBubblesIntoDoAgent(BubbleGroup innerGroup, 
            long currentTime, int maxLoadPerGroup, LoadBubblesIntoStateHolder innerGroupState)
        {
            var innerGroupAgent = innerGroup.Service as Agent;

            if (!SupportsSyncAndIsRunning(innerGroup.Service))
            {
                innerGroupState.Dead = true;
            }
            else
            {
                var bubbles = LoadBubblesIntoQueryAgent(innerGroupAgent, innerGroup, currentTime, 
                    maxLoadPerGroup, innerGroupState);
                return bubbles;
            }

            return null;
        }

        public class LoadBubbles : IDisposable
        {
            private readonly IEnumerator<bool> _enumerator;
            private readonly object _lock = new object();

            public EventHandler<Service> ServiceStarted { get; set; }

            public LoadBubbles(BubbleGroup group, List<VisualBubble> listToLoadInto)
            {
                _enumerator = LoadBubblesInto(group, listToLoadInto, this).GetEnumerator();
            }

            public Task<bool> Next()
            {
                return Task<bool>.Factory.StartNew(() =>
                {
                    lock (_lock)
                    {
                        return _enumerator.MoveNext();
                    }
                });
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (ServiceStarted != null)
                    {
                        ServiceEvents.Started -= ServiceStarted;
                    }
                }
            }
        }

        private class LoaderBubblesIntoRemoveDuplicatesComparer : IEqualityComparer<VisualBubble>
        {
            public bool Equals(VisualBubble x, VisualBubble y)
            {
                if (x.Service != y.Service)
                    return false;
                    
                var comparer = x.Service as Comparer;
                if (comparer == null)
                    return false;

                return comparer.LoadBubblesComparer(x, y);
            }

            public int GetHashCode(VisualBubble obj)
            {
                return 0;
            }
        }

        private static List<VisualBubble> LoadBubblesIntoRemoveDuplicates(List<VisualBubble> bubbles, 
            List<VisualBubble> lastSecondItems)
        {
            if (!lastSecondItems.Any())
            {
                return bubbles.ToList();
            }

            var time = lastSecondItems.First().Time;

            var newBubbles = new List<VisualBubble>();

            foreach (var bubble in bubbles)
            {
                var comparer = bubble.Service as Comparer;

                if (comparer == null || bubble.Time != time)
                {
                    newBubbles.Add(bubble);
                    continue;
                }
                    
                var hasDuplicate = lastSecondItems.FirstOrDefault(x => x.Service == bubble.Service 
                    && comparer.LoadBubblesComparer(x, bubble)) != null;
                if (!hasDuplicate)
                {
                    newBubbles.Add(bubble);
                }
            }

            return newBubbles;
        }

        private static void LoadBubblesIntoRemoveDuplicates(List<VisualBubble> list, long unixTime)
        {
            var itemsOnTime = list.Where(x => x.Time == unixTime).ToList();

            if (itemsOnTime.Count < 1)
            {
                return;
            }

            // if we remove anything, we prefer to remove what was added
            itemsOnTime.Reverse();

            var deletionNegatives = new HashSet<VisualBubble>(new LoaderBubblesIntoRemoveDuplicatesComparer());

            foreach (var item in itemsOnTime)
            {
                deletionNegatives.Add(item);
            }

            foreach (var deletions in itemsOnTime.Except(deletionNegatives))
            {
                list.Remove(deletions);
            }
        }

        private static IEnumerable<bool> LoadBubblesInto(BubbleGroup group, List<VisualBubble> listToLoadInto, 
            LoadBubbles instance)
        {
            if (!listToLoadInto.Any())
                yield break;

            Utils.DebugPrint("Loading into convo " + group.ID);

            lock (SyncLock)
            {
                Utils.DebugPrint("Loading into convo " + group.ID + " (after lock)");

                var unifiedGroup = group as UnifiedBubbleGroup;
                var groups = unifiedGroup != null ? unifiedGroup.Groups : new List<BubbleGroup>() { group };

                const int MaxLoadPerGroup = 100;
                var currentTime = listToLoadInto.Min(x => x.Time);

                var bubbleGroupStates = new Dictionary<BubbleGroup, LoadBubblesIntoStateHolder>();
                foreach (var innerGroup in groups)
                {
                    bubbleGroupStates[innerGroup] = new LoadBubblesIntoStateHolder();
                }

                EventHandler<Service> serviceStarted = (sender, e) =>
                {
                    foreach (var state in bubbleGroupStates)
                    {
                        if (state.Key.Service == e)
                        {
                            state.Value.Dead = false;
                        }
                    }
                };

                ServiceEvents.Started += serviceStarted;
                instance.ServiceStarted = serviceStarted;

                while (true)
                {
                    var listToLoadIntoCount = listToLoadInto.Count;
                    var bubbleLoads = new Dictionary<BubbleGroup, List<Tuple<VisualBubble, bool>>>();

                    foreach (var innerGroup in groups)
                    {
                        var innerGroupState = bubbleGroupStates[innerGroup];

                        if (innerGroupState.Dead)
                        {
                            bubbleLoads[innerGroup] = null;
                            continue;
                        }

                        if (innerGroupState.LocalFinished)
                        {
                            var agentBubbles = LoadBubblesIntoDoAgent(innerGroup, currentTime, MaxLoadPerGroup, innerGroupState);
                            if (agentBubbles != null)
                            {
                                bubbleLoads[innerGroup] = agentBubbles.Select(x => 
                                    new Tuple<VisualBubble, bool>(x, true)).ToList();
                            }
                            else
                            {
                                bubbleLoads[innerGroup] = null;
                            }
                        }
                        else
                        {
                            var localBubbles = new List<VisualBubble>();
                            innerGroupState.LocalCursor = BubbleGroupDatabase.FetchBubblesAt(innerGroup, 
                                currentTime, MaxLoadPerGroup, ref localBubbles, innerGroupState.LocalCursor);
                            if (innerGroupState.LocalCursor == 0 || localBubbles.Count == 0)
                            {
                                innerGroupState.LocalFinished = true;
                            }

                            if (localBubbles.Count == MaxLoadPerGroup)
                            {
                                bubbleLoads[innerGroup] = localBubbles.Select(x => 
                                    new Tuple<VisualBubble, bool>(x, false)).ToList();
                            }
                            else
                            {
                                var innerGroupComparer = innerGroup.Service as Comparer;

                                if (innerGroupComparer != null)
                                {
                                    var agentBubbles = LoadBubblesIntoDoAgent(innerGroup, currentTime, MaxLoadPerGroup, innerGroupState);

                                    if (agentBubbles == null)
                                    {
                                        bubbleLoads[innerGroup] = localBubbles.Select(x => 
                                            new Tuple<VisualBubble, bool>(x, false)).ToList();
                                    }
                                    else
                                    {
                                        var innerGroupAgent = innerGroup.Service as Agent;
                                        var combined = new List<Tuple<VisualBubble, bool>>();

                                        // combine them: take all agent bubbles, and then try to replace the clouds with the locals.
                                        // what can't be replaced becomes the inserts.

                                        for (int i = 0; i < agentBubbles.Count; i++)
                                        {
                                            var agentBubble = agentBubbles[i];
                                            var localBubble = 
                                                localBubbles.FirstOrDefault(x => innerGroupComparer.LoadBubblesComparer(x, agentBubble));
                                            if (localBubble != null)
                                            {
                                                combined.Add(new Tuple<VisualBubble, bool>(localBubble, false));
                                            }
                                            else
                                            {
                                                combined.Add(new Tuple<VisualBubble, bool>(agentBubble, true));
                                            }
                                        }

                                        bubbleLoads[innerGroup] = combined;
                                    }
                                }
                                else
                                {
                                    bubbleLoads[innerGroup] = localBubbles.Select(x => 
                                        new Tuple<VisualBubble, bool>(x, false)).ToList();
                                }
                            }
                        }
                    }

                    // if all the sync agents failed, then obviously loading more bubbles in trivially failed
                    if (bubbleGroupStates.Count(x => x.Value.Dead) == groups.Count)
                    {
                        yield break;
                    }

                    // insert the bubbles into the disa bubble group with two conditions
                    // a) must not be bubble retreived from disa bubble group
                    // b) must not be a duplicate already in the list to load into
                    var listToLoadIntoBubblesOnTime = listToLoadInto.Where(x => x.Time == currentTime).ToList();
                    foreach (var bubbleLoad in bubbleLoads)
                    {
                        if (bubbleLoad.Value == null)
                            continue;

                        var bubbleGroup = bubbleLoad.Key;
                        var bubblesToInsert = LoadBubblesIntoRemoveDuplicates(bubbleLoad.Value.Where(x => x.Item2)
                            .Select(x => x.Item1).ToList(), listToLoadIntoBubblesOnTime);
                        if (bubblesToInsert.Any())
                        {
                            BubbleGroupDatabase.InsertBubblesByTime(bubbleGroup, 
                                bubblesToInsert.ToArray(), int.MaxValue, true, true);
                        }
                    }
                        
                    // find the greatest minimum time of all the bubble loads
                    // and merge the bubble loads against that

                    var greatestMin = 0L;
                    foreach (var bubbleLoad in bubbleLoads)
                    {
                        if (bubbleLoad.Value == null || !bubbleLoad.Value.Any())
                            continue;

                        var min = bubbleLoad.Value.Min(x => x.Item1.Time);
                        if (min > greatestMin)
                        {
                            greatestMin = min;
                        }
                    }

                    var mergedBubbles = new List<VisualBubble>();
                    foreach (var bubbleLoad in bubbleLoads)
                    {
                        if (bubbleLoad.Value != null)
                        {
                            var bubblesToMerge = bubbleLoad.Value.Where(x => 
                                x.Item1.Time >= greatestMin).Select(x => x.Item1).ToList();
                            foreach (var bubbleToMerge in bubblesToMerge)
                            {
                                bubbleToMerge.BubbleGroupReference = bubbleLoad.Key;
                            }
                            mergedBubbles.AddRange(bubblesToMerge);
                        }
                    }
                    mergedBubbles.TimSort((x, y) => x.Time.CompareTo(y.Time));

                    // insert the merged bubbles into the list to load into, making sure to 
                    // remove and duplicates encountered.
                    listToLoadInto.InsertRange(0, mergedBubbles);
                    LoadBubblesIntoRemoveDuplicates(listToLoadInto, currentTime);

                    currentTime = mergedBubbles.First().Time;

                    // if the count wasn't altered, we've hit the end
                    if (listToLoadIntoCount == listToLoadInto.Count)
                    {
                        yield break;
                    }

                    yield return true;
                }
            }
        }

        public static Task Sync(BubbleGroup group, bool force = false)
        {
            return Task.Factory.StartNew(() =>
            {
                Utils.DebugPrint("Syncing convo " + group.ID);

                lock (SyncLock)
                {
                    Utils.DebugPrint("Syncing convo " + group.ID + " (after lock)");

                    var somethingSynced = true;

                    var groupsToSync = new List<BubbleGroup>();
                  
                    var unifiedGroup = group as UnifiedBubbleGroup;
                    if (unifiedGroup != null)
                    {
                        groupsToSync.AddRange(unifiedGroup.Groups);
                    }
                    else
                    {
                        groupsToSync.Add(group);
                    }
                        
                    foreach (var groupToSync in groupsToSync)
                    {
                        if (!groupToSync.NeedsSync && !force)
                            continue;

                        var groupToSyncAgent = groupToSync.Service as Agent;

                        if (groupToSyncAgent == null)
                            continue;

                        if (!ServiceManager.IsRunning(groupToSync.Service))
                        {
                            continue;
                        }

                        try
                        {
                            var syncTask = groupToSyncAgent.Sync(groupToSync, Database.GetActionId(groupToSync));
                            syncTask.Wait();
                            var syncResult = syncTask.Result;
                            if (!syncResult.EmptyResult && !syncResult.NullActionId && !syncResult.JustRefresh)
                            {
                                lock (BubbleGroupDatabase.OperationLock)
                                {
                                    if (syncResult.ResultType == Result.Type.Purge)
                                    {
                                        Utils.DebugPrint("Sync is purging the database for bubble group " + groupToSync.ID 
                                            + " on service " + groupToSync.Service.Information.ServiceName);
                                        BubbleGroupDatabase.Kill(groupToSync);
                                    }
                                    if (!syncResult.EmptyInserts)
                                    {
                                        Utils.DebugPrint("Sync is inserting " + syncResult.Inserts.Length
                                            + " bubbles into " + groupToSync.ID + " on service " + groupToSync.Service.Information.ServiceName);
                                        if (syncResult.ResultType == Result.Type.Purge)
                                        {
                                            syncResult.Inserts.TimSort((x, y) => x.Time.CompareTo(y.Time));
                                            BubbleGroupDatabase.AddBubbles(groupToSync, syncResult.Inserts);
                                        }
                                        else
                                        {
                                            BubbleGroupDatabase.InsertBubblesByTime(groupToSync, syncResult.Inserts, SearchDepth);
                                        }
                                    }
                                    if (!syncResult.EmptyUpdates)
                                    {
                                        Utils.DebugPrint("Sync is updating " + syncResult.Updates.Length 
                                            + " bubbles into " + groupToSync.ID + " on service " + groupToSync.Service.Information.ServiceName);
                                        BubbleManager.Update(groupToSync, syncResult.Updates, SearchDepth);
                                    }
                                    Database.SetActionId(groupToSync, syncResult.NewActionId);
                                }
                            }
                            else
                            {
                                Utils.DebugPrint("Sync for bubble group " + 
                                    groupToSync.ID + " on service " + groupToSync.Service.Information.ServiceName + 
                                    " returned an empty result (" + syncResult.ResultType.ToString() + ").");
                                somethingSynced = syncResult.JustRefresh;
                            }
                            groupToSync.NeedsSync = false;
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint("Failed to sync bubble group " + groupToSync.ID 
                                + " on service " + groupToSync.Service.Information.ServiceName + ": " + ex);
                            somethingSynced = false;
                        }
                    }
                        
                    if (somethingSynced)
                    {
                        lock (BubbleGroupDatabase.OperationLock)
                        {
                            var sendingBubbles = BubbleManager.FetchAllSendingAndDownloading(group).ToList();
                            BubbleGroupFactory.UnloadFullLoad(group);
                            BubbleGroupFactory.LoadFullyIfNeeded(group, true);
                            if (sendingBubbles.Any())
                            {
                                BubbleManager.Replace(group, sendingBubbles);
                            }
                            group.RaiseBubblesSynced();
                        }
                    }
                }
            });
        }
    }
}


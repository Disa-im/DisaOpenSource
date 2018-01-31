using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    //FIXME: If BubbleGroupIndex gets re-generated, a lazy group will become a permanent group.
    //FIXME: If a lazy bubble group gets merged into a unified bubble group, the lazy tag should be dropped.
    //FIXME: Incoming bubble (event, process, and sync), drop lazy tag
    //TODO: Ensure BubbleGroupUpdater does not update Lazy groups
    public class BubbleGroupsSync
    {
        public interface Agent
        {
            Task<List<VisualBubble>> LoadBubbleGroups2(BubbleGroup startGroup, int count = 10, IEnumerable<Tag> tags = null);
            Task<IEnumerable<VisualBubble>> LoadBubbleGroups(IEnumerable<Tag> tags = null);

            Task<bool> OnLazyBubbleGroupsDeleted(List<BubbleGroup> groups);
        }

        private static void SortListByTime(List<BubbleGroup> list)
        {
            list.TimSort((x, y) =>
            {
                var timeX = x.LastBubbleSafe().Time;
                var timeY = y.LastBubbleSafe().Time;
                return -timeX.CompareTo(timeY);
            });
        }

        public class Cursor : IEnumerable<BubbleGroup>, IDisposable
        {
            private readonly List<Tag> _tags;
            
            public Cursor(IEnumerable<Tag> tags)
            {
                _tags = tags.ToList();
            }
            
            private IEnumerable<BubbleGroup> LoadBubblesInternal2()
            {
                var tagServices = _tags.Select(t => t.Service).ToHashSet();

                var allBubbles = tagServices.SelectMany(service => 
                    {
                        var agent = service as Agent;
                        if (agent != null)
                        {
                            var serviceTags = _tags.Where(t => t.Service == service).ToList();
                            var task = agent.LoadBubbleGroups(serviceTags);
                            try
                            {
                                task.Wait();
                            }
                            catch (Exception ex)
                            {
                                Utils.DebugPrint($"{service} threw exception: {ex}");
                                return new List<VisualBubble>();
                            }
                            return task.Result;
                        }
                        return new List<VisualBubble>();
                    });

                foreach (var bubble in allBubbles)
                {
                    var group = new BubbleGroup(bubble, null, false);
                    group.Lazy = true;
                    yield return group;
                }
            }

            private IEnumerable<BubbleGroup> LoadBubblesInternalTagManager()
            {
                return TagManager.GetAllBubbleGroups(_tags);
            }

            public IEnumerator<BubbleGroup> GetEnumerator()
            {
                return LoadBubblesInternalTagManager().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            //private class RemoveDuplicatesComparer : IEqualityComparer<BubbleGroup>
            //{
            //    public bool Equals(BubbleGroup x, BubbleGroup y)
            //    {
            //        if (x.Service != y.Service)
            //            return false;

            //        return x.Service.BubbleGroupComparer(x.Address, y.Address);
            //    }

            //    public int GetHashCode(BubbleGroup obj)
            //    {
            //        return 0;
            //    }
            //}

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            
            // NOTE: Leave out the finalizer altogether if this class doesn't   
            // own unmanaged resources itself, but leave the other methods  
            // exactly as they are.   
            ~Cursor()
            {
                // Finalizer calls Dispose(false)  
                Dispose(false);
            }

            // The bulk of the clean-up code is implemented in Dispose(bool)  
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    //FIXME: ensure services are running of the deleted groups
                    var lazys = BubbleGroupManager.FindAll(x => x.Lazy);
                    foreach (var lazy in lazys)
                    {
                        BubbleGroupFactory.Delete(lazy, false);
                    }

                    //FIXME: call into service needs to be atomic.
                    foreach (var lazyGroup in lazys.GroupBy(x => x.Service))
                    {
                        var key = lazyGroup.Key;
                        var agent = key as Agent;
                        agent.OnLazyBubbleGroupsDeleted(lazyGroup.ToList());
                    }
                }
            }

#if false
            private IEnumerable<Result> LoadBubblesInternal()
            {
                var doneQueryingAgents = false;
                var count = _pageSize;
                var servicesFinished = new Dictionary<Service, bool>();
                var tagServices = _tags.Select(t => t.Service).ToHashSet();

                while (true)
                {
                    var originalCardinality = _list.Count;
                    var bubbleGroups = TagManager.GetAllBubbleGroups(_tags).ToList();
                    //Utils.DebugPrint("LOADING xx" + bubbleGroups.Count);
                    //FIXME: ConvoList in UI will call this method before BubbleGroupManager is ready.
                    //       Fix this race condition.
                    if (!bubbleGroups.Any())
                    {
                        if (_list.Any())
                        {
                            _list.Clear();
                            yield return Result.Change;
                        }
                        else
                        {
                            yield return Result.NoChange;
                        }
                        continue;
                    }
                    if (!_initiallyRemovedLazy)
                    {
                        if (_refreshState)
                        {
                            bubbleGroups = bubbleGroups.Where(x => !x.Lazy).ToList();
                        }
                        else
                        {
                            //FIXME: ensure services are running of the deleted groups
                            var lazys = bubbleGroups.Where(x => x.Lazy);
                            foreach (var lazy in lazys)
                            {
                                BubbleGroupFactory.Delete(lazy, false);
                            }
                            //FIXME: call into service needs to be atomic.
                            foreach (var lazyGroup in lazys.GroupBy(x => x.Service))
                            {
                                var key = lazyGroup.Key;
                                var agent = key as Agent;
                                agent.OnLazyBubbleGroupsDeleted(lazyGroup.ToList());
                            }
                            bubbleGroups = TagManager.GetAllBubbleGroups(_tags).ToList();
                            _initiallyRemovedLazy = true;
                        }
                    }
                    SortListByTime(bubbleGroups);
                    if (!_refreshState)
                    {
                        count += _pageSize;
                    }
                    var page = bubbleGroups.Take(count).ToList();
                    SortListByTime(page);
                    if (!_refreshState && !doneQueryingAgents)
                    {
                        //var startIndex = page.Count - 1;
                        var startIndex = count - 1 - _pageSize;
                        if (startIndex + 1 > bubbleGroups.Count)
                        {
                            startIndex = bubbleGroups.Count - 1;
                        }
                        Utils.DebugPrint("BubbleGroupsSync", "Increment size (constant): " + _pageSize);
                        Utils.DebugPrint("BubbleGroupsSync", "New theoretical local size: " + count);
                        Utils.DebugPrint("BubbleGroupsSync", "Current local size: " + page.Count);
                        Utils.DebugPrint("BubbleGroupsSync", "Will expand local size by: " + (count - page.Count));
                        Utils.DebugPrint("BubbleGroupsSync", "Local bubblegroup for cloud start index: " + startIndex);
                        var bubbleGroupsToQuery = new List<BubbleGroup>();
                        for (int i = startIndex; i >= 0; i--)
                        {
                            var group = page[i];
                            var unifiedGroup = group as UnifiedBubbleGroup;
                            if (unifiedGroup != null)
                            {
                                group = unifiedGroup.LastBubbleSafe().BubbleGroupReference;
                            }
                            if (group != null)
                            {
                                var existing = bubbleGroupsToQuery.FirstOrDefault(x => x.Service == group.Service) != null;
                                if (!existing)
                                {
                                    bubbleGroupsToQuery.Add(group);
                                }
                            }
                            else
                            {
                                Utils.DebugPrint("BubbleGroupsSync", "Group is null (possible BubbleGroupReference)");
                            }
                        }
                        var bubbleGroupsRunningAgentToQuery = bubbleGroupsToQuery
                            .Where(x => ServiceManager.IsRunning(x.Service) && x.Service is Agent).ToList();
                        if (bubbleGroupsRunningAgentToQuery.Any())
                        {
                            var allBubbles = new List<VisualBubble>();
                            foreach (var bubbleGroup in bubbleGroupsRunningAgentToQuery)
                            {
                                if (!servicesFinished.ContainsKey(bubbleGroup.Service))
                                {
                                    var agent = bubbleGroup.Service as Agent;
                                    try
                                    {
                                        Utils.DebugPrint("BubbleGroupsSync", "Local bubblegroup for cloud start title: " + bubbleGroup.Title);
                                        var serviceTags = _tags.Where(t => t.Service == bubbleGroup.Service).ToList();
                                        var task = agent.LoadBubbleGroups2(bubbleGroup, count, serviceTags);
                                        task.Wait();
                                        //var result
                                        if (task.Result != null && task.Result.Count >= _pageSize)
                                        {
                                            allBubbles.AddRange(task.Result);
                                        }
                                        else
                                        {
                                            servicesFinished[bubbleGroup.Service] = true;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.DebugPrint("BubbleGroupsSync",
                                            "Sync agent failed to load bubble groups: " + ex);
                                        servicesFinished[bubbleGroup.Service] = true;
                                    }
                                }
                            }
                            Utils.DebugPrint("BubbleGroupsSync", "Cloud result size: " + allBubbles.Count);
                            var cloudGroups = new List<BubbleGroup>();
                            foreach (var bubble in allBubbles)
                            {
                                var group = new BubbleGroup(bubble, null, false);
                                group.Lazy = true;
                                page.Add(group);
                                cloudGroups.Add(group);
                            }
                            SortListByTime(page);
                            Utils.DebugPrint("BubbleGroupsSync", "Local & cloud combined size: " + page.Count);
                            var duplicates = new List<BubbleGroup>();
                            foreach (var group in cloudGroups)
                            {
                                if (!BubbleGroupFactory.AddNewIfNotExist(group))
                                {
                                    Utils.DebugPrint("BubbleGroupsSync",
                                                     "Duplicate bubblegroup detected: " + group.Address);
                                    duplicates.Add(group);
                                }
                            }
                            foreach (var duplicate in duplicates)
                            {
                                page.Remove(duplicate);
                            }
                            page = page.Take(count).ToList();
                            Utils.DebugPrint("BubbleGroupsSync", "New local size: " + page.Count);
                            if (count != page.Count)
                            {
                                Utils.DebugPrint("BubbleGroupsSync", "Theoretical local size " + count + " != actual local size " + page.Count);
                                count = page.Count;
                            }
                            if (servicesFinished.Count == bubbleGroupsRunningAgentToQuery.Count)
                            {
                                //TODO: doneQueryingAgents needs to be set to false when a service is started
                                doneQueryingAgents = true;
                            }
                        }
                    }
                    Utils.DebugPrint("BubbleGroupsSync", "List size: " + page.Count);
                    _list.Clear();
                    _list.AddRange(page);
                    try
                    {
                        if (_postAction != null)
                        {
                            _postAction();
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("BubbleGroupsSync", "Failed to run post action: " + ex);
                    }
                    if (_refreshState)
                    {
                        //TODO: if the list hasn't changed return false to optimize UI updates
                        yield return Result.Change;
                    }
                    else
                    {
                        if (originalCardinality == _list.Count)
                        {
                            yield return Result.NoCardinalityChange;
                        }
                        else
                        {
                            yield return Result.Change;
                        }
                    }
                }
            }

            private IEnumerable<Result> LoadBubblesInternal3()
            {
                var doneQueryingAgents = false;
                var count = _pageSize;
                var servicesFinished = new Dictionary<Service, bool>();
                var tagServices = _tags.Select(t => t.Service).ToHashSet();

                while (true)
                {
                    var originalCardinality = _list.Count;
                    var bubbleGroups = TagManager.GetAllBubbleGroups(_tags).ToList();
                    //Utils.DebugPrint("LOADING xx" + bubbleGroups.Count);
                    //FIXME: ConvoList in UI will call this method before BubbleGroupManager is ready.
                    //       Fix this race condition.
                    if (!bubbleGroups.Any())
                    {
                        if (_list.Any())
                        {
                            _list.Clear();
                            yield return Result.Change;
                        }
                        else
                        {
                            yield return Result.NoChange;
                        }
                        continue;
                    }
                    if (!_initiallyRemovedLazy)
                    {
                        if (_refreshState)
                        {
                            bubbleGroups = bubbleGroups.Where(x => !x.Lazy).ToList();
                        }
                        else
                        {
                            //FIXME: ensure services are running of the deleted groups
                            var lazys = bubbleGroups.Where(x => x.Lazy);
                            foreach (var lazy in lazys)
                            {
                                BubbleGroupFactory.Delete(lazy, false);
                            }
                            //FIXME: call into service needs to be atomic.
                            foreach (var lazyGroup in lazys.GroupBy(x => x.Service))
                            {
                                var key = lazyGroup.Key;
                                var agent = key as Agent;
                                agent.OnLazyBubbleGroupsDeleted(lazyGroup.ToList());
                            }
                            bubbleGroups = TagManager.GetAllBubbleGroups(_tags).ToList();
                            _initiallyRemovedLazy = true;
                        }
                    }
                    SortListByTime(bubbleGroups);
                    if (!_refreshState)
                    {
                        count += _pageSize;
                    }
                    var page = bubbleGroups.Take(count).ToList();
                    SortListByTime(page);
                    if (!_refreshState && !doneQueryingAgents)
                    {
                        //var startIndex = page.Count - 1;
                        var startIndex = count - 1 - _pageSize;
                        if (startIndex + 1 > bubbleGroups.Count)
                        {
                            startIndex = bubbleGroups.Count - 1;
                        }
                        Utils.DebugPrint("BubbleGroupsSync", "Increment size (constant): " + _pageSize);
                        Utils.DebugPrint("BubbleGroupsSync", "New theoretical local size: " + count);
                        Utils.DebugPrint("BubbleGroupsSync", "Current local size: " + page.Count);
                        Utils.DebugPrint("BubbleGroupsSync", "Will expand local size by: " + (count - page.Count));
                        Utils.DebugPrint("BubbleGroupsSync", "Local bubblegroup for cloud start index: " + startIndex);
                        var bubbleGroupsToQuery = new List<BubbleGroup>();
                        for (int i = startIndex; i >= 0; i--)
                        {
                            var group = page[i];
                            var unifiedGroup = group as UnifiedBubbleGroup;
                            if (unifiedGroup != null)
                            {
                                group = unifiedGroup.LastBubbleSafe().BubbleGroupReference;
                            }
                            if (group != null)
                            {
                                var existing = bubbleGroupsToQuery.FirstOrDefault(x => x.Service == group.Service) != null;
                                if (!existing)
                                {
                                    bubbleGroupsToQuery.Add(group);
                                }
                            }
                            else
                            {
                                Utils.DebugPrint("BubbleGroupsSync", "Group is null (possible BubbleGroupReference)");
                            }
                        }
                        var bubbleGroupsRunningAgentToQuery = bubbleGroupsToQuery
                            .Where(x => ServiceManager.IsRunning(x.Service) && x.Service is Agent).ToList();
                        if (bubbleGroupsRunningAgentToQuery.Any())
                        {
                            var allBubbles = new List<VisualBubble>();
                            foreach (var bubbleGroup in bubbleGroupsRunningAgentToQuery)
                            {
                                if (!servicesFinished.ContainsKey(bubbleGroup.Service))
                                {
                                    var agent = bubbleGroup.Service as Agent;
                                    try
                                    {
                                        Utils.DebugPrint("BubbleGroupsSync", "Local bubblegroup for cloud start title: " + bubbleGroup.Title);
                                        var serviceTags = _tags.Where(t => t.Service == bubbleGroup.Service).ToList();
                                        var task = agent.LoadBubbleGroups(serviceTags);
                                        task.Wait();
                                        //var result
                                        if (task.Result != null && task.Result.Count >= _pageSize)
                                        {
                                            allBubbles.AddRange(task.Result);
                                        }
                                        else
                                        {
                                            servicesFinished[bubbleGroup.Service] = true;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Utils.DebugPrint("BubbleGroupsSync",
                                            "Sync agent failed to load bubble groups: " + ex);
                                        servicesFinished[bubbleGroup.Service] = true;
                                    }
                                }
                            }
                            Utils.DebugPrint("BubbleGroupsSync", "Cloud result size: " + allBubbles.Count);
                            var cloudGroups = new List<BubbleGroup>();
                            foreach (var bubble in allBubbles)
                            {
                                var group = new BubbleGroup(bubble, null, false);
                                group.Lazy = true;
                                page.Add(group);
                                cloudGroups.Add(group);
                            }
                            SortListByTime(page);
                            Utils.DebugPrint("BubbleGroupsSync", "Local & cloud combined size: " + page.Count);
                            var duplicates = new List<BubbleGroup>();
                            foreach (var group in cloudGroups)
                            {
                                if (!BubbleGroupFactory.AddNewIfNotExist(group))
                                {
                                    Utils.DebugPrint("BubbleGroupsSync",
                                                     "Duplicate bubblegroup detected: " + group.Address);
                                    duplicates.Add(group);
                                }
                            }
                            foreach (var duplicate in duplicates)
                            {
                                page.Remove(duplicate);
                            }
                            page = page.Take(count).ToList();
                            Utils.DebugPrint("BubbleGroupsSync", "New local size: " + page.Count);
                            if (count != page.Count)
                            {
                                Utils.DebugPrint("BubbleGroupsSync", "Theoretical local size " + count + " != actual local size " + page.Count);
                                count = page.Count;
                            }
                            if (servicesFinished.Count == bubbleGroupsRunningAgentToQuery.Count)
                            {
                                //TODO: doneQueryingAgents needs to be set to false when a service is started
                                doneQueryingAgents = true;
                            }
                        }
                    }
                    Utils.DebugPrint("BubbleGroupsSync", "List size: " + page.Count);
                    _list.Clear();
                    _list.AddRange(page);
                    try
                    {
                        if (_postAction != null)
                        {
                            _postAction();
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("BubbleGroupsSync", "Failed to run post action: " + ex);
                    }
                    if (_refreshState)
                    {
                        //TODO: if the list hasn't changed return false to optimize UI updates
                        yield return Result.Change;
                    }
                    else
                    {
                        if (originalCardinality == _list.Count)
                        {
                            yield return Result.NoCardinalityChange;
                        }
                        else
                        {
                            yield return Result.Change;
                        }
                    }
                }
            }
#endif
        }
    }
}

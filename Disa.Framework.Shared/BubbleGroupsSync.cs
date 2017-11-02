using System;
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
        //TODO: move out to own class when Category system is well defined
        public class Category
        {
            
        }

        public interface Agent
        {
            Task<List<VisualBubble>> LoadBubbleGroups(BubbleGroup startGroup, int count = 10, Category category = null);

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

        public class Cursor
        {
            private enum Result { NoCardinalityChange, NoChange, Change }

            public bool IsBusy { get; private set; }

            private readonly List<BubbleGroup> _list;
            private readonly int _pageSize;
            private readonly IEnumerator<Result> _enumerator;

            private bool _refreshState;
            private Action _postAction;
            private Action _action;
            private bool _initiallyRemovedLazy;

            public Cursor(List<BubbleGroup> list, int pageSize)
            {
                _list = list;
                _pageSize = pageSize;
                _enumerator = LoadBubblesInternal().GetEnumerator();
            }

            public void Schedule(Action action)
            {
                _action = action;
            }

            public bool Refresh()
            {
                lock (_enumerator)
                {
                    var tempList = _list.ToList();
                    try
                    {
                        _refreshState = true;
                        _enumerator.MoveNext();
                        return _enumerator.Current == Result.Change;
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint("BubbleGroupsSync", "Failed to refresh: " + ex);
                        _list.Clear();
                        _list.AddRange(tempList);
                        return true;
                    }
                    finally
                    {
                        _refreshState = false;
                        try
                        {
                            if (_action != null)
                            {
                                _action();    
                            }
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint("BubbleGroupsSync", "Failed to execute action: " + ex);
                        }
                        finally
                        {
                            _action = null;
                        }
                    }
                }
            }

            public Task<bool> LoadBubbles(Action postAction = null)
            {
                return Task<bool>.Factory.StartNew(() =>
                {
                    lock (_enumerator)
                    {
                        var tempList = _list.ToList();
                        try
                        {
                            IsBusy = true;
                            _postAction = postAction;
                            _enumerator.MoveNext();
                            return _enumerator.Current != Result.NoCardinalityChange;
                        }
                        catch (Exception ex)
                        {
                            Utils.DebugPrint("BubbleGroupsSync", "Failed to load bubbles " + ex);
                            _list.Clear();
                            _list.AddRange(tempList);
                            return false;
                        }
                        finally
                        {
                            IsBusy = false;
                            _postAction = null;
                        }
                    }
                });
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

            private IEnumerable<Result> LoadBubblesInternal()
            {
                var doneQueryingAgents = false;
                var count = _pageSize;
                var servicesFinished = new Dictionary<Service, bool>();
                while (true)
                {
                    var originalCardinality = _list.Count;
                    var bubbleGroups = BubbleGroupManager.DisplayImmutable;
                    //Utils.DebugPrint("LOADING xx" + bubbleGroups.Count);
                    //FIXME: ConvoList in UI will call this method before BubbleGroupManager is ready.
                    //       Fix this race condition.
                    if (!bubbleGroups.Any())
                    {
                        yield return Result.NoChange;
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
                                BubbleGroupFactory.Delete(lazy);
                            }
                            //FIXME: call into service needs to be atomic.
                            foreach (var lazyGroup in lazys.GroupBy(x => x.Service))
                            {
                                var key = lazyGroup.Key;
                                var agent = key as Agent;
                                agent.OnLazyBubbleGroupsDeleted(lazyGroup.ToList());
                            }
                            bubbleGroups = BubbleGroupManager.DisplayImmutable;
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
                                        var task = agent.LoadBubbleGroups(bubbleGroup, _pageSize, null);
                                        task.Wait();
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
                            Utils.DebugPrint("BubbleGroupsSync", "New local size: " +  page.Count);
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
        }
    }
}

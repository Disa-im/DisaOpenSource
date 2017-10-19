using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public class BubbleGroupsSync
    {
        //TODO: move out to own class when Category system is well defined
        public class Category
        {
            
        }

        public interface Agent
        {
            Task<List<BubbleGroup>> LoadBubbleGroups(BubbleGroup startGroup, int count = 10, Category category = null);
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

            private IEnumerable<Result> LoadBubblesInternal()
            {
                var doneQueryingAgents = false;
                var count = _pageSize;
                var servicesFinished = new Dictionary<Service, bool>();
                while (true)
                {
                    var originalCardinality = _list.Count;
                    var bubbleGroups = BubbleGroupManager.DisplayImmutable;
                    //FIXME: ConvoList in UI will call this method before BubbleGroupManager is ready.
                    //       Fix this race condition.
                    if (!bubbleGroups.Any())
                    {
                        yield return Result.NoChange;
                        continue;
                    }
                    SortListByTime(bubbleGroups);
                    if (!_refreshState)
                    {
                        count += _pageSize;
                    }
                    var page = bubbleGroups.Take(count).ToList();
                    var lazy = _list.Where(x => x.Lazy);
                    page.AddRange(lazy);
                    SortListByTime(page);
                    if (!_refreshState && !doneQueryingAgents)
                    {
                        var bubbleGroupsToQuery = new List<BubbleGroup>();
                        for (int i = page.Count - 1; i >= 0; i--)
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
                            foreach (var bubbleGroup in bubbleGroupsRunningAgentToQuery)
                            {
                                if (!servicesFinished.ContainsKey(bubbleGroup.Service))
                                {
                                    var agent = bubbleGroup.Service as Agent;
                                    try
                                    {
                                        var task = agent.LoadBubbleGroups(bubbleGroup, count, null);
                                        task.Wait();
                                        if (task.Result != null && task.Result.Any())
                                        {
                                            page.AddRange(task.Result);
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
                            SortListByTime(page);
                            page = page.Take(count).ToList();
                            if (servicesFinished.Count == bubbleGroupsRunningAgentToQuery.Count)
                            {
                                //TODO: doneQueryingAgents needs to be set to false when a service is started
                                doneQueryingAgents = true;
                            }
                        }
                    }
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
                        Utils.DebugPrint("BubbleGroupsSync",
                                            "Failed to run post action: " + ex);
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

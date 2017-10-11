using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Disa.Framework
{
    public class BubbleGroupsSync
    {
        // TODO: move out to own class when Category system is well defined
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
            private readonly List<BubbleGroup> _list;
            private readonly int _pageSize;
            private readonly IEnumerator<bool> _enumerator;

            private bool _refreshState;

            public Cursor(List<BubbleGroup> list, int pageSize)
            {
                _list = list;
                _pageSize = pageSize;
                _enumerator = LoadBubblesInternal().GetEnumerator();
            }

            public Task Refresh()
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (_enumerator)
                    {
                        _refreshState = true;
                        _enumerator.MoveNext();
                        _refreshState = false;
                    }
                });
            }

            public Task LoadBubbles()
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (_enumerator)
                    {
                        _enumerator.MoveNext();
                    }
                });
            }

            private IEnumerable<bool> LoadBubblesInternal()
            {
                var count = _pageSize;
                var iteration = 0;
                var servicesFinished = new Dictionary<Service, bool>();
                while (true)
                {
                    var bubbleGroups = BubbleGroupManager.DisplayImmutable;
                    //if (bubbleGroups.Count != 0 && count > bubbleGroups.Count)
                    //{
                    //    yield break;
                    //}
                    SortListByTime(bubbleGroups);
                    _list.Clear();
                    if (!_refreshState)
                    {
                        count += _pageSize;
                        iteration++;
                    }
                    var page = bubbleGroups.Take(count).ToList();
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
                    var bubbleGroupsRunningAgentToQuery = bubbleGroupsToQuery.Where(x => ServiceManager.IsRunning(x.Service) && x.Service is Agent).ToList();
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
                                Utils.DebugPrint("BubbleGroupsSync", "Sync agent failed to load bubble groups: " + ex);
                                servicesFinished[bubbleGroup.Service] = true;
                            }
                        }
                    }
                    SortListByTime(page);
                    page = page.Take(count).ToList();
                    _list.AddRange(page);
                    if (servicesFinished.Count == bubbleGroupsRunningAgentToQuery.Count)
                    {
                        yield break;
                    }
                    yield return true;
                }
            }   
        }
    }
}

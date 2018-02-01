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
            Task<IEnumerable<BubbleGroup>> LoadBubbleGroups(IEnumerable<Tag> tags = null);

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
            
            private IEnumerable<BubbleGroup> LoadBubblesInternalLazyService()
            {
                var tagServices = _tags.Select(t => t.Service).ToHashSet();

                var serviceBubbleGroupsEnumerators = tagServices.Select(service =>
                {
                    var agent = service as Agent;
                    var serviceTags = _tags.Where(t => t.Service == service).ToList();
                    if (agent != null)
                    {
                        // Service supports lazy loading
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
                    else
                    {
                        // Service does not support lazy loading
                        // Get the bubble groups from Tag Manager

                    }
                    return new List<VisualBubble>();
                })
                .Select(l => l.GetEnumerator())
                .ToList();
                
                var priorityQueue = new List<(VisualBubble, IEnumerator<VisualBubble>)>();

                // Initialize the queue
                for (var i = 0; i < serviceBubbleEnumerators.Count;)
                {
                    var bubbleEnumerator = serviceBubbleEnumerators[i];
                    if (bubbleEnumerator.MoveNext())
                    {
                        priorityQueue.Add((bubbleEnumerator.Current, bubbleEnumerator));
                        i++;
                    }
                    else
                    {
                        serviceBubbleEnumerators.RemoveAt(i);
                    }
                }

                priorityQueue = priorityQueue.OrderByDescending(t => t.Item1.Time).ToList();

                while (true)
                {
                    // We're done enumerating all the bubbles we got from services
                    if (!serviceBubbleEnumerators.Any())
                    {
                        break;
                    }

                    (var bubble, var enumerator) = priorityQueue[0];
                    if (enumerator.MoveNext())
                    {
                        priorityQueue.Add((enumerator.Current, enumerator));
                    }
                    else
                    {
                        serviceBubbleEnumerators.Remove(enumerator);
                    }
                    priorityQueue = priorityQueue.OrderByDescending(t => t.Item1.Time).ToList();
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
                //return LoadBubblesInternalTagManager().GetEnumerator();
                return LoadBubblesInternalService().GetEnumerator();
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
            
            ~Cursor()
            {
                Dispose(false);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }
                
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
    }
}

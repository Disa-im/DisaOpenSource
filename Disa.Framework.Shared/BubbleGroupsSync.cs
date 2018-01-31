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
        }
    }
}

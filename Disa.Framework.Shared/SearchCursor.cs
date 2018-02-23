using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public interface ISearchAgent
    {
        Task<IEnumerable<BubbleGroup>> SearchBubbleGroups(string query);
    }

    public class SearchCursor : IEnumerable<BubbleGroup>, IDisposable
    {
        public readonly string Query;
        
        public SearchCursor(string query)
        {
            Query = query;
        }

        private IEnumerable<BubbleGroup> SearchBubbleGroups(Service service)
        {
            var bubbleGroups = BubbleGroupManager.FindAll(service);
            return Utils.SearchBubbleGroups(bubbleGroups, Query);
        }

        private IEnumerable<BubbleGroup> LoadBubblesInternalLazyService()
        {
            var services = ServiceManager.RegisteredNoUnified.ToList();
            var serviceBubbleGroupsEnumerators = services.Select(service =>
            {
                if (service is ISearchAgent agent)
                {
                    // Service supports lazy loading
                    var task = agent.SearchBubbleGroups(Query);
                    try
                    {
                        task.Wait();
                    }
                    catch (Exception ex)
                    {
                        Utils.DebugPrint($"{service} threw exception: {ex}");
                        return SearchBubbleGroups(service);
                    }
                    return task.Result;
                }
                else
                {
                    // Service does not support searching
                    // Search bubble groups                    
                    return SearchBubbleGroups(service);
                }
            });

            return Utils.LazySorting(serviceBubbleGroupsEnumerators, group => group.LastBubbleSafe().Time);
        }

        public IEnumerator<BubbleGroup> GetEnumerator()
        {
            //return LoadBubblesInternalService().GetEnumerator();
            return LoadBubblesInternalLazyService().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        ~SearchCursor()
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
        }
    }
}

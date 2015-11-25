using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Disa.Framework
{
    public class ThreadSafeList<T> : IList<T>
    {
        private readonly List<T> _items = new List<T>();

        public ThreadSafeList(IEnumerable<T> items = null) 
        {
            AddRange(items);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                return;
            }
            lock (_items)
            {
                _items.AddRange(collection);
            }
        }

        public int IndexOf(T item)
        {
            lock (_items)
            {
                return _items.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_items)
            {
                _items.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_items)
            {
                _items.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_items)
                {
                    return _items[index];
                }
            }
            set
            {
                lock (_items)
                {
                    _items[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            lock (_items)
            {
                _items.Add(item);
            }
        }

        public void Clear()
        {
            lock (_items)
            {
                _items.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_items)
            {
                return _items.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_items)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (_items)
            {
                return _items.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_items)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get 
            {
                return false;
            }
        }

        public bool Any(Func<T, bool> predicate)
        {
            lock (_items)
            {
                return _items.Any(predicate);
            }
        }

        public bool Any()
        {
            lock (_items)
            {
                return _items.Any();
            }
        }

        public T Last()
        {
            lock (_items)
            {
                return _items.Last();
            }
        }

        public T First()
        {
            lock (_items)
            {
                return _items.First();
            }
        }

        public List<T> ToList()
        {
            lock (_items)
            {
                return _items.ToList();
            }
        }

        public T[] ToArray()
        {
            lock (_items)
            {
                return _items.ToArray();
            }
        }

        public T FirstOrDefault(Func<T, bool> predicate)
        {
            lock (_items)
            {
                return _items.FirstOrDefault(predicate);
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            lock (_items)
            {
                return _items.Where(predicate);
            }
        }

        public int CountEx(Func<T, bool> predicate)
        {
            lock (_items)
            {
                return _items.Count(predicate);
            }
        }

        public IEnumerator<T> GetEnumerator() 
        {
            return ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() 
        {
            return GetEnumerator();
        }
    }
}


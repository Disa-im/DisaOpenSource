using System;
using System.Collections.Generic;
using System.Linq;

namespace Disa.Framework
{
    public sealed class Queue<T>
    {
        private readonly List<T> _mList = new List<T>();
        private readonly object _mLock = new object();

        public void Clear()
        {
            lock (_mLock)
            {
                _mList.Clear();
            }
        }

        public void Add(T value)
        {
            lock (_mLock)
            {
                _mList.Add(value);
            }
        }

        public bool Remove(T value)
        {
            lock (_mLock)
            {
                return _mList.Remove(value);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_mLock)
                {
                    return index < _mList.Count ? _mList[index] : default(T);
                }
            }
        }

        public T LastOrDefault(Func<T, bool> predicate)
        {
            lock (_mLock)
            {
                return _mList.LastOrDefault(predicate);
            }
        }

        public T LastOrDefault()
        {
            lock (_mLock)
            {
                return _mList.LastOrDefault();
            }
        }
    }
}
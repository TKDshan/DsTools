using System;
using System.Collections.Generic;

namespace DsTools.Tile
{
    internal class SortList<T> where T : IComparable<T>
    {
        private List<T> _list = new List<T>();

        public void Add(T item)
        {
            _list.Add(item);
            _list.Sort();
        }
        
        public int Count => _list.Count;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = value;
                _list.Sort();
            }
        }

        public void Remove(T item)
        {
            _list.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public bool Contains(T t)
        {
            return _list.Contains(t);
        }
    }
}



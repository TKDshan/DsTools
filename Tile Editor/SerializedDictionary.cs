using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DsTools
{
    /// <summary>
    /// 序列化字典
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    [Serializable]
    public class SerializedDictionary<Key, Value> : IEnumerable where Key : IComparable<Key>
    {
        [SerializeField] private List<Key> _keyList = new List<Key>();
        [SerializeReference] private List<Value> _valueList = new List<Value>();
        
        /// <summary>
        /// 字典长度
        /// </summary>
        public int Count => _keyList.Count;
        
        public Value this[Key key]
        {
            get
            {
                int index = _keyList.IndexOf(key);
                if (index != -1)
                {
                    return _valueList[index];
                }

                throw new KeyNotFoundException("Key not found: " + key);
            }
            set
            {
                int index = _keyList.IndexOf(key);
                if (index != -1)
                {
                    _valueList[index] = value;
                }
                else
                {
                    Add(key,value);
                }
            }
        }

        /// <summary>
        /// 是否包含key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(Key key) => _keyList.Contains(key);

        /// <summary>
        /// 清除元素
        /// </summary>
        public void Clear()
        {
            _keyList.Clear();
            _valueList.Clear();
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(Key key, Value value)
        {
            if (!_keyList.Contains(key))
            {
                _keyList.Add(key);
                _valueList.Add(value);
                return true;
            }

            Debug.Log($"Key already exists: {key}");
            return false;
        }

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(Key key)
        {
            int index = _keyList.IndexOf(key);
            if (index != -1)
            {
                _keyList.RemoveAt(index);
                _valueList.RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试获取数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(Key key, out Value value)
        {
            int index = _keyList.IndexOf(key);
            if (index != -1)
            {
                value = _valueList[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 排序
        /// </summary>
        public void Sort()
        {
            List<KeyValuePair<Key, Value>> tempList = new List<KeyValuePair<Key, Value>>();

            for (int i = 0; i < _keyList.Count; i++)
            {
                Key key = _keyList[i];
                Value value = _valueList[i];
                tempList.Add(new KeyValuePair<Key, Value>(key, value));
            }

            tempList.Sort((x, y) => x.Key.CompareTo(y.Key));

            _keyList.Clear();
            _valueList.Clear();
            foreach (var pair in tempList)
            {
                _keyList.Add(pair.Key);
                _valueList.Add(pair.Value);
            }
        }

        /// <summary>
        /// 迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator()
        {
            for (int i = 0; i < _keyList.Count; i++)
            {
                yield return new KeyValuePair<Key, Value>(_keyList[i], _valueList[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
namespace SecondMonitor.Contracts
{
    using System.Collections;
    using System.Collections.Generic;

    public class CachingDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionaryInternal;
        private TKey _lastUsedKey;
        private TValue _lastUsedValue;

        public CachingDictionary()
        {
            _dictionaryInternal = new Dictionary<TKey, TValue>();
            _lastUsedKey = default;
            _lastUsedValue = default;
        }

        public ICollection<TKey> Keys => _dictionaryInternal.Keys;
        public ICollection<TValue> Values => _dictionaryInternal.Values;
        public int Count => _dictionaryInternal.Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryInternal).IsReadOnly;

        public TValue this[TKey key]
        {
            get
            {
                if (Compare(key, _lastUsedKey))
                {
                    return _lastUsedValue;
                }

                _lastUsedKey = key;
                _lastUsedValue = _dictionaryInternal[key];
                return _lastUsedValue;
            }

            set => _dictionaryInternal[key] = value;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionaryInternal.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionaryInternal.GetEnumerator();

        public void Clear() => _dictionaryInternal.Clear();

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => _dictionaryInternal.Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryInternal).Contains(item);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryInternal).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionaryInternal).Remove(item);
        
        public bool ContainsKey(TKey key) => _dictionaryInternal.ContainsKey(key);

        public void Add(TKey key, TValue value) => _dictionaryInternal.Add(key, value);

        public bool Remove(TKey key)
        {
            if (Compare(key, _lastUsedKey))
            {
                _lastUsedKey = default;
            }

            return _dictionaryInternal.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (Compare(key, _lastUsedKey))
            {
                value = _lastUsedValue;
                return true;
            }

            if (!_dictionaryInternal.TryGetValue(key, out value))
            {
                return false;
            }

            _lastUsedKey = key;
            _lastUsedValue = value;
            return true;
        }

        public bool Compare<T>(T x, T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }
    }
}
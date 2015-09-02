using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    /// <summary>
    /// A sort of a <see cref="Dictionary"/> that a single key can have multiple values.
    /// </summary>
    /// <typeparam name="TKey">Type of keys.</typeparam>
    /// <typeparam name="TValue">Type of values.</typeparam>
    public class MultiDict<TKey, TValue> : IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>
    {
        private readonly Dictionary<TKey, TValue> Single = new Dictionary<TKey, TValue>();

        private readonly Dictionary<TKey, IList<TValue>> Multi = new Dictionary<TKey, IList<TValue>>();

        public void Add(TKey key, TValue value)
        {
            IList<TValue> values;
            TValue prev;
            if (Multi.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else if (Single.TryGetValue(key, out prev))
            {
                Single.Remove(key);
                Multi.Add(key, new List<TValue>() { prev, value });
            }
            else
            {
                Single.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return Single.ContainsKey(key) || Multi.ContainsKey(key);
        }

        public IEnumerable<TKey> Keys { get { return Enumerable.Concat(Single.Keys, Multi.Keys); } }

        public IEnumerable<TValue> Get(TKey key)
        {
            IList<TValue> values;
            TValue prev;
            if (Multi.TryGetValue(key, out values))
            {
                return values.AsEnumerable();
            }
            else if (Single.TryGetValue(key, out prev))
            {
                return new[] { prev }.AsEnumerable();
            }
            else
            {
                return null;
            }
        }

        private static KeyValuePair<TKey, IEnumerable<TValue>> Pair(TKey key, IEnumerable<TValue> values)
        {
            return new KeyValuePair<TKey, IEnumerable<TValue>>(key, values);
        }

        public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
        {
            var s = Single.Select(p => Pair(p.Key, new[] { p.Value }));
            var m = Multi.Select(p => Pair(p.Key, p.Value.ToArray()));
            return s.Concat(m).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

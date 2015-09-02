using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    public class ObjectReuser<T> where T: class
    {
        private readonly Dictionary<T, T> Pool = new Dictionary<T, T>();

        public T Get(T item)
        {
            T value;
            if (Pool.TryGetValue(item, out value)) return value;
            Pool.Add(item, item);
            return item;
        }
    }
}
